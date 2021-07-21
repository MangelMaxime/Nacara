module Watch

open Nacara.Core.Types
open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Elmish
open Node
open Chokidar

exception InitMarkdownFileErrorException of filePath : string * original : exn

[<NoComparison>]
type Msg =
    | InitMarkdownFile of filePath : string
    | InitMarkdownFileError of InitMarkdownFileErrorException
    | ProcessMarkdown of pageContext : PageContext
    | ProcessOther of filePath: string
    | LoadMenuFile of filePath : string
    | MenuFiledLoaded of filePath : string * menuConfig : MenuConfig
    | ProcessSass of filePath: string
    | CopyFile of source : string
    | CopyFileWithDestination of source : string * destination : string
    | DependencyFileChanged of filePath : string

[<NoComparison; NoEquality>]
type Model =
    {
        FileWatcher : Chokidar.FSWatcher
        LayoutDependencyWatcher : Chokidar.FSWatcher
        Server : Http.Server
        Config : Config
        LayoutRenderer : JS.Map<string, LayoutRenderFunc>
        LayoutDependencies : LayoutDependency list
        Pages : PageContext list
        Menus : MenuConfig list
        LightnerCache : JS.Map<string, CodeLightner.Config>
    }

[<NoComparison; NoEquality>]
type InitArgs =
    {
        ProcessQueue : QueueFile list
        Layouts : LayoutInfo array
        Config : Config
        Pages : PageContext list
        Menus : MenuConfig list
        LightnerCache : JS.Map<string, CodeLightner.Config>
    }

let fileWatcherSubscription (model : Model) =
    let handler dispatch =
        // Register behavior when:
        // - a new file is added to the `Source` directory
        // - a tracked file change
        model.FileWatcher.on(Events.All, (fun event filePath ->
            let isFromSourceFolder =
                filePath.Replace("\\", "/").StartsWith(model.Config.SourceFolder)

            let filePath =
                if isFromSourceFolder && filePath <> model.Config.SourceFolder then
                    filePath.Substring(model.Config.SourceFolder.Length + 1)
                else
                    filePath

            match event with
            | Events.Add
            | Events.Change ->
                match filePath with
                | MarkdownFile ->
                    InitMarkdownFile filePath
                    |> dispatch

                | SassFile ->
                    // If the file is under one of the special directory we re-process the SASS/SCSS root file
                    // because we consider that one of it's include has changed
                    if filePath.Replace("\\", "/").StartsWith("scss/")
                        || filePath.Replace("\\", "/").StartsWith("sass/") then

                        let styleSCSS = path.join(model.Config.SourceFolder, "style.scss")
                        let styleSASS = path.join(model.Config.SourceFolder, "style.sass")

                        Log.log $"File %s{filePath} changed, re-process style file"

                        if File.existSync styleSCSS then
                            ProcessSass "style.scss"
                            |> dispatch
                        else if File.existSync styleSASS then
                            ProcessSass "style.sass"
                            |> dispatch
                        else
                            Log.error $"Can't find %s{styleSASS} or %s{styleSCSS} files"

                    else
                        ProcessSass filePath
                        |> dispatch

                | MenuFile ->
                    LoadMenuFile filePath
                    |> dispatch

                | JavaScriptFile
                | OtherFile _ ->
                    ProcessOther filePath
                    |> dispatch
            | Events.Unlink
            | Events.UnlinkDir
            | Events.Ready
            | Events.Raw
            | Events.Error
            | Events.AddDir
            | _ -> ()
        ))

    [ handler ]

let layoutDependencyWatcherSubscription (model : Model) =
    let handler dispatch =
        // Register behavior when:
        // - a new file is added to the `Source` directory
        // - a tracked file change
        model.LayoutDependencyWatcher.on(Events.All, (fun event path ->
            match event with
            | Events.Change ->
                DependencyFileChanged path
                |> dispatch

            | Events.Add
            | Events.Unlink
            | Events.UnlinkDir
            | Events.Ready
            | Events.Raw
            | Events.Error
            | Events.AddDir
            | _ -> ()
        ))

    [ handler ]

let private baseUrlMiddleware (baseUrl : string) : LiveServer.Middleware = import "default" "./../js/base-url-middleware.js"

let private startServer (config : Config) =
    // Start the LiveServer instance
    let liveServerOption =
        jsOptions<LiveServer.Options>(fun o ->
            o.root <- path.join(config.WorkingDirectory, config.Output)
            o.``open`` <- false
            o.logLevel <- 0
            o.port <- config.ServerPort
            o.host <- "localhost"
        )

    if config.BaseUrl <> "/" then
        liveServerOption.middleware <-
            [|
                baseUrlMiddleware config.BaseUrl
            |]

    let server = LiveServer.liveServer.start(liveServerOption)

    // We need to register in the event in order have access to the server info
    // Otherwise, the server isn't ready yet
    server.on("listening", (fun _ _ ->
        Log.success $"Server started at: http://localhost:%i{config.ServerPort}"
    ))
    |> ignore

    server

let init (args : InitArgs) : Model * Cmd<Msg> =
    let layoutCache =
        let keyValues =
            args.Layouts
            |> Array.collect (fun info ->
                info.Renderers
                |> Array.map (fun renderer ->
                    renderer.Name, renderer.Func
                )
            )

        JS.Constructors.Map.Create(keyValues)

    let layoutDependencies =
        args.Layouts
        |> Array.collect (fun info ->
            info.Dependencies
        )
        |> Array.toList

    let chokidarOptions =
        jsOptions<Chokidar.IOptions>(fun o ->
            o.ignoreInitial <- true
            // o.ignored <- [|
            //     $"%s{args.Config.SourceFolder}/scss/*"
            //     $"%s{args.Config.SourceFolder}/sass/*"
            // |]
        )

    // Start the watcher empty because we don't know yet where the dependency files are
    let layoutDependencyWatcher =
        chokidar.watch([||])

    let cmd =
        args.ProcessQueue
        |> List.map (
            function
            | QueueFile.Markdown pageContext ->
                Cmd.ofMsg (ProcessMarkdown pageContext)

            | QueueFile.Sass filePath ->
                Cmd.ofMsg (ProcessSass filePath)

            | QueueFile.LayoutDependency layoutDependency ->
                // Watch the dependency file, this will trigger copy when the source file changes
                layoutDependencyWatcher.add(layoutDependency.Source)

                Cmd.ofMsg (CopyFileWithDestination (layoutDependency.Source, layoutDependency.Destination))

            | QueueFile.JavaScript filePath
            | QueueFile.Other filePath ->
                Cmd.ofMsg (ProcessOther filePath)
        )
        |> Cmd.batch

    {
        FileWatcher = chokidar.watch(args.Config.SourceFolder, chokidarOptions)
        LayoutDependencyWatcher = layoutDependencyWatcher
        Server = startServer args.Config
        LayoutRenderer = layoutCache
        LayoutDependencies = layoutDependencies
        Config = args.Config
        Pages = args.Pages
        Menus = args.Menus
        LightnerCache = args.LightnerCache
    }
    , cmd

let private updatePagesCache (cache : PageContext list) (newPageContext : PageContext) =
    let rec apply
        (oldCache : PageContext list)
        (newPageContext : PageContext)
        (newCache : PageContext list)
        (attributesChanged : bool) =

        match oldCache with
        | head :: tail ->
            if head.PageId = newPageContext.PageId then
                apply tail newPageContext (newPageContext :: newCache) (head.Attributes <> newPageContext.Attributes)
            else
                apply tail newPageContext (head :: newCache) attributesChanged

        | [] ->
            newCache, attributesChanged

    apply cache newPageContext [] false

let update (msg : Msg) (model : Model) =
    match msg with
    | LoadMenuFile filePath ->
        let action () =
            initMenuFiles model.Config.SourceFolder filePath
            |> Promise.map(
                function
                | Ok menuConfig ->
                    (filePath, menuConfig)

                | Error errorMessage ->
                    failwith errorMessage
            )
            |> Promise.catch (fun error ->
                Log.error $"Failed to load menu file: %s{filePath}"
                JS.console.error error
                failwith ""
            )

        model
        , Cmd.OfPromise.perform action () MenuFiledLoaded

    | DependencyFileChanged filePath ->
        let dependencyOpt =
            model.LayoutDependencies
            |> List.tryFind (fun dependency ->
                dependency.Source = filePath
            )

        let cmd =
            match dependencyOpt with
            | Some dependency ->
                Cmd.ofMsg (CopyFileWithDestination (dependency.Source, dependency.Destination))

            | None ->
                Log.error $"Dependency file %s{filePath} changed but is not found in the tracked dependency list. This is likely a bug please report it."
                Cmd.none

        model
        , cmd

    | CopyFile _ ->
        Log.log "todo copyfile"

        model
        , Cmd.none

    | CopyFileWithDestination (source, destination) ->
        let args =
            model.Config.DestinationFolder
            , source
            , destination

        let action args =
            Write.copyFileWithDestination args
            |> Promise.map( fun _ ->
                Log.log $"Dependency file %s{source} copied to %s{destination}"
            )
            |> Promise.catchEnd (fun error ->
                Log.error $"Error while copying %s{source} to %s{destination}\n%A{error}"
            )

        model
        , Cmd.OfFunc.exec action args

    | MenuFiledLoaded (filePath, menuConfig) ->
        Log.log $"Menu '%s{filePath}' changed re-generate pages of section '%s{menuConfig.Section}'"

        // Store the new menuConfig in the cache
        let newMenuList =
            model.Menus
            |> List.filter (fun storedMenu ->
                storedMenu.Section <> menuConfig.Section
            )
            |> List.append [ menuConfig ]

        // Regenerate the pages corresponding to the new menu section
        let cmd =
            model.Pages
            |> List.filter (fun page ->
                page.Section = menuConfig.Section
            )
            |> List.map (fun page ->
                Cmd.ofMsg (ProcessMarkdown page)
            )
            |> Cmd.batch

        { model with
            Menus = newMenuList
        }
        , cmd

    | InitMarkdownFile filePath ->
        let action filePath =
            promise {
                let! pageContext =
                    initPageContext model.Config.SourceFolder filePath

                match pageContext with
                | Ok pageContext ->
                    return pageContext

                | Error errorMessage ->
                    return failwith errorMessage
            }
            |> Promise.catch (fun error ->
                raise (InitMarkdownFileErrorException (filePath, error))
            )

        model
        , Cmd.OfPromise.either action filePath ProcessMarkdown InitMarkdownFileError

    | InitMarkdownFileError error ->
        Log.error $"Error while initializing context of file: %s{error.filePath}"
        Log.error $"%A{error.original}"

        model
        , Cmd.none

    | ProcessMarkdown pageContext ->

        let (newPagesCache, attributesChanged) =
            updatePagesCache model.Pages pageContext

        let rec containsPage (pageId : string) (menu : Menu) =
            menu
            |> List.exists (fun menuItem ->
                match menuItem with
                | MenuItem.Link _ ->
                    false

                | MenuItem.Page info ->
                    info.PageId = pageId

                | MenuItem.List info ->
                    containsPage pageId info.Items
            )

        let rec extractPageId (menu : Menu) =
            menu
            |> List.collect (fun menuItem ->
                match menuItem with
                | MenuItem.Link _ ->
                    [ ]

                | MenuItem.Page info ->
                    [ info.PageId ]

                | MenuItem.List info ->
                    extractPageId info.Items
            )

        let reProcessPageDependingOnChangedPage =
            if attributesChanged then
                Log.log $"The attributes of the page %s{pageContext.PageId} have changed, all page depending on it will be re-processed"
                model.Menus
                // Search for the menu containing the page Id
                |> List.filter (fun menuConfig ->
                    containsPage pageContext.PageId menuConfig.Items
                )
                // Extract all the page Id from the menus found
                |> List.collect (fun menuConfig ->
                    extractPageId menuConfig.Items
                )
                // Remove duplicates
                |> List.distinct
                // Remove the page which trigger the change from the re-processing it will alrady happen
                |> List.filter (fun pageId ->
                    pageId <> pageContext.PageId
                )
                // Generate the list of command to re-process the pages
                |> List.map (fun pageId ->
                    let pageContextToReProcess =
                        model.Pages
                        |> List.tryFind (fun page ->
                            page.PageId = pageId
                        )

                    match pageContextToReProcess with
                    | Some pageContext ->
                        Cmd.ofMsg (ProcessMarkdown pageContext)

                    | None ->
                        Log.error $"Page '%s{pageId}' not found"
                        Cmd.none
                )
            else
                []



        let args =
            {
                PageContext = pageContext
                Layouts = model.LayoutRenderer
                Menus = model.Menus
                Config = model.Config
                Pages = newPagesCache
                LightnerCache = model.LightnerCache
            } : Write.ProcessMarkdownArgs

        let action args =
            Write.markdown args
            |> Promise.map (fun _ ->
                Log.log $"Processed: %s{pageContext.RelativePath}"
            )
            |> Promise.catchEnd (fun error ->
                Log.error $"Error while processing markdown file: %s{pageContext.PageId}"
                JS.console.error error
            )

        { model with
            Pages = newPagesCache
        }
        , Cmd.batch [
            Cmd.OfFunc.exec action args
            yield! reProcessPageDependingOnChangedPage
        ]

    | ProcessOther filePath ->
        let args =
            model.Config.DestinationFolder
            , model.Config.SourceFolder
            , filePath

        let action =
            Write.copyFile
            >> Promise.map (fun _ ->
                Log.log $"Copied: %s{filePath}"
            )
            >> Promise.catchEnd (fun error ->
                Log.error $"Error while copying %s{filePath}"
                JS.console.log error
            )

        model
        , Cmd.OfFunc.exec action args

    | ProcessSass filePath ->
        let args =
            Sass.OutputStyle.Expanded
            , model.Config.DestinationFolder
            , model.Config.SourceFolder
            , filePath

        let action =
            Write.sassFile
            >> Promise.map (fun _ ->
                Log.log $"Processed: %s{filePath}"
            )
            >> Promise.catchEnd (fun error ->
                Log.error $"Error while processing SASS file: %s{filePath}"
                JS.console.error error
            )

        model
        , Cmd.OfFunc.exec action args
