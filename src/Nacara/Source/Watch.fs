module Watch

open Nacara.Core.Types
open Fable.Core
open Fable.Core.JsInterop
open Elmish
open Node
open Chokidar
open Thoth.Json

exception InitMarkdownFileErrorException of filePath : string * original : exn
exception LoadMenuFileErrorException of filePath : string * original : exn
exception LayoutSourceLoadErrorException of filePath : string * original : exn

[<NoComparison; NoEquality>]
type FsharpFileLoadedResult =
    | ProcessAsMarkdown of pageContext : PageContext
    | CopyToDestination of filePath : string

[<NoComparison; NoEquality>]
type Msg =
    | InitMarkdownFile of filePath : string
    | PartialChanged of filePath : string
    | LoadFsharpFile of filePath : string
    | FsharpFileLoaded of FsharpFileLoadedResult
    | LayoutSourceChanged of filePath : string
    | LayoutSourceLoaded of LayoutInfo
    | LayoutSourceLoadError of LayoutSourceLoadErrorException
    | PartialLoaded of Partial
    | InitPartialError of exn
    | InitMarkdownFileError of InitMarkdownFileErrorException
    | ProcessMarkdown of pageContext : PageContext
    | ProcessOther of filePath: string
    | LoadMenuFile of filePath : string
    | LoadMenuFileError of LoadMenuFileErrorException
    | MenuFiledLoaded of filePath : string * menuConfig : MenuConfig
    | ProcessSass of filePath: string
    | CopyFileWithDestination of source : string * destination : string
    | DependencyFileChanged of filePath : string
    | SendRefreshCSS
    | SendReloadPage of filePath : string
    | SendFullReload

[<NoComparison; NoEquality>]
type Model =
    {
        FileWatcher : Chokidar.FSWatcher
        LayoutDependencyWatcher : Chokidar.FSWatcher
        LayoutSourcesWatcher : Chokidar.FSWatcher
        OutputFileWatcher : Chokidar.FSWatcher
        HttpServer : Http.Server
        WssServer : Ws.WebSocket.Server
        Config : Config
        LayoutRenderer : JS.Map<string, LayoutRenderFunc>
        LayoutDependencies : LayoutDependency list
        Pages : PageContext list
        Menus : MenuConfig list
        Partials : Partial list
    }

[<NoComparison; NoEquality>]
type InitArgs =
    {
        ProcessQueue : QueueFile list
        Layouts : LayoutInfo array
        Config : Config
        Pages : PageContext list
        Menus : MenuConfig list
        Partials : Partial list
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

                | PartialFile ->
                    PartialChanged filePath
                    |> dispatch

                | FsharpFile ->
                    LoadFsharpFile filePath
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

let layoutSourcesWatcherSubscription (model : Model) =
    let handler dispatch =
        // Register behavior when:
        // - a new file is added to the `Source` directory
        // - a tracked file change
        model.LayoutSourcesWatcher.on(Events.All, (fun event path ->
            match event with
            | Events.Change ->
                // Artificially make the layout relative as we are only watching relative files
                // Chokidar doesn't have the leading "./"

                LayoutSourceChanged ("./" + path)
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

let outputFileWatcherSubscription (model : Model) =
    let handler dispatch =
        // Trigger a reload from the browser when a file
        // in the output folder change
        // The event can be anything as even if a file is deleted we should update the website
        model.OutputFileWatcher.on(Events.All, (fun event filePath ->
            let ext = path.extname(filePath)

            match ext.ToLower() with
            | ".css" ->
                dispatch SendRefreshCSS

            | ".html" ->
                dispatch (SendReloadPage filePath)

            // This type of file doesn't have any specific behaviour
            // we force a full reload independently of the page we are currently on
            | _ ->
                dispatch SendFullReload
        ))

    [ handler ]

let private startServer (config : Config) =
    let server = Server.create config

    let wss = Ws.webSocket.WebSocketServer.Create(jsOptions<Ws.WebSocket.ServerOptions>(fun o ->
        o.server <- !^server
    ))

    wss.on("connection", fun (client : Ws.WebSocket) _ ->
        let data =
            Encode.object [
                "type", Encode.string "Connected"
            ]
            |> Encode.toString 0

        client.send(data)
    )
    |> ignore

    server.listen(config.ServerPort, fun () ->
        Log.success $"Server started at: http://localhost:%i{config.ServerPort}"
    )
    |> ignore

    server, wss

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

    let liveReloadDependency =
        {
            Source = path.join(Module.__dirname, "../scripts/live-reload.js")
            Destination = "resources/nacara/scripts/live-reload.js"
        }

    let layoutDependencies =
        args.Layouts
        |> Array.collect (fun info ->
            info.Dependencies
        )
        |> Array.toList
        |> List.append [ liveReloadDependency ]

    let chokidarOptions =
        jsOptions<Chokidar.IOptions>(fun o ->
            o.ignoreInitial <- true
            o.awaitWriteFinish <- U2.Case1 (
                jsOptions<Chokidar.AwaitWriteFinishOptions> (fun o ->
                    o.stabilityThreshold <- 250.
                    o.pollInterval <- 100.
                )
            )
        )

    // Start the watcher empty because we don't know yet where the dependency files are
    let layoutDependencyWatcher =
        chokidar.watch([|
            liveReloadDependency.Source
        |])

    let layoutSourcesWatcherer =
        args.Config.Layouts
        // Keep only the relative layouts
        |> Array.filter (fun layout ->
            layout.StartsWith("./")
        )
        |> chokidar.watch

    let processQueueCmd =
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

    let httpServer, wssServer = startServer args.Config

    {
        FileWatcher = chokidar.watch(args.Config.SourceFolder, chokidarOptions)
        LayoutDependencyWatcher = layoutDependencyWatcher
        LayoutSourcesWatcher = layoutSourcesWatcherer
        OutputFileWatcher = chokidar.watch(args.Config.DestinationFolder, chokidarOptions)
        HttpServer = httpServer
        WssServer = wssServer
        LayoutRenderer = layoutCache
        LayoutDependencies = layoutDependencies
        Config = args.Config
        Pages = args.Pages
        Menus = args.Menus
        Partials = args.Partials
    }
    , Cmd.batch [
        processQueueCmd
        // Manually trigger the copy of the live-reload.js file
        Cmd.ofMsg (CopyFileWithDestination (liveReloadDependency.Source, liveReloadDependency.Destination))
    ]

let private updatePagesCache (cache : PageContext list) (newPageContext : PageContext) =
    let rec apply
        (oldCache : PageContext list)
        (newPageContext : PageContext)
        (newCache : PageContext list)
        (attributesChanged : bool)
        (found : bool) =

        match oldCache with
        | head :: tail ->
            if head.PageId = newPageContext.PageId then
                apply tail newPageContext (newPageContext :: newCache) (head.Attributes <> newPageContext.Attributes) true
            else
                apply tail newPageContext (head :: newCache) attributesChanged found

        | [] ->
            if found then
                newCache, attributesChanged
            // If the page is not found, we add it to the cache
            else
                newPageContext :: newCache, attributesChanged

    apply cache newPageContext [] false false

let private sendReload (model : Model) (page : string option) =
    let data =
        Encode.object [
            "type", Encode.string "reload"
            "page", Encode.option Encode.string page
        ]
        |> Encode.toString 0

    model.WssServer.clients.forEach(fun client key _ ->
        client.send(data)
    )

let private sendRefreshCSS (model : Model) =
    let data =
        Encode.object [
            "type", Encode.string "refreshCSS"
        ]
        |> Encode.toString 0

    model.WssServer.clients.forEach(fun client key _ ->
        client.send(data)
    )

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
                raise (LoadMenuFileErrorException (filePath, error))
            )

        model
        , Cmd.OfPromise.either action () MenuFiledLoaded LoadMenuFileError

    | LoadFsharpFile filePath ->

        let action () =
            promise {
                let fullFilePath =
                    path.join(model.Config.SourceFolder, path.sep, filePath)

                let! fileContent = File.read fullFilePath

                // Try parsing the file as Literate F# file
                match FsharpFileParser.tryParse fileContent with
                | Some markdownContent ->

                    let! pageContext =
                        initPageContextFromContent markdownContent fullFilePath filePath

                    match pageContext with
                    | Ok pageContext ->
                        return (ProcessAsMarkdown pageContext)

                    | Error errorMessage ->
                        return failwith errorMessage

                | None ->
                    return (CopyToDestination filePath)
            }
            |> Promise.catch (fun error ->
                raise (InitMarkdownFileErrorException (filePath, error))
            )

        model
        , Cmd.OfPromise.either action () FsharpFileLoaded InitMarkdownFileError

    | FsharpFileLoaded action ->
        match action with
        | CopyToDestination filePath ->
            model
            , Cmd.ofMsg (ProcessOther filePath)

        | ProcessAsMarkdown pageContext ->
            model
            , Cmd.ofMsg (ProcessMarkdown pageContext)

    | SendRefreshCSS ->
        model
        , Cmd.OfFunc.exec sendRefreshCSS model

    | SendReloadPage filePath ->
        let pageId =
            path.relative(model.Config.DestinationFolder, filePath)
            |> getPageId

        model
        , Cmd.OfFunc.exec (sendReload model) (Some pageId)

    | SendFullReload ->
        model
        , Cmd.OfFunc.exec (sendReload model) None

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

    | LayoutSourceChanged filePath ->
        let action (config, layout) =
            Layout.load config layout
            |> Promise.catch (fun error ->
                raise (LayoutSourceLoadErrorException (filePath, error))
            )

        model
        , Cmd.OfPromise.either action (model.Config, filePath) LayoutSourceLoaded LayoutSourceLoadError

    | LayoutSourceLoadError error ->
        Log.error $"Error while loading layout: %s{error.filePath}"
        Log.error $"%A{error.original}"

        model
        , Cmd.none

    | LayoutSourceLoaded layoutInfo ->
        let layoutNames =
            layoutInfo.Renderers
            |> Array.map (fun renderer ->
                renderer.Name
            )

        // Regenerate pages depending on the changed layouts
        let cmd =
            model.Pages
            |> List.filter (fun page ->
                Array.contains page.Layout layoutNames
            )
            |> List.map (fun page ->
                Cmd.ofMsg (ProcessMarkdown page)
            )
            |> Cmd.batch

        // Update the layout cache
        // It is a mutable JS.Map
        for newRenderer in layoutInfo.Renderers do
            model.LayoutRenderer.set(newRenderer.Name, newRenderer.Func)
            |> ignore

        model
        , cmd

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
                    initPageContextFromFile model.Config.SourceFolder filePath

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

    | LoadMenuFileError error ->
        Log.error $"Error while loading menu: %s{error.filePath}"
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
                // Remove the page which trigger the change from the re-processing it will already happen
                |> List.filter (fun pageId ->
                    pageId <> pageContext.PageId
                )
                // Generate the list of command to re-process the pages
                |> List.map (fun pageId ->
                    let pageContextToReProcess =
                        newPagesCache
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
                Partials = model.Partials
                Menus = model.Menus
                Config = model.Config
                Pages = newPagesCache
                RemarkPlugins = model.Config.RemarkPlugins
                RehypePlugins = model.Config.RehypePlugins
            } : Write.ProcessMarkdownArgs

        let action args =
            Write.markdown args
            |> Promise.map (fun _ ->
                Log.log $"Processed: %s{pageContext.RelativePath}"
            )
            |> Promise.catchEnd (fun error ->
                Log.error $"Error while processing markdown file: %s{pageContext.PageId}"
                match error with
                | :? Write.ProcessFileErrorException as error ->
                    Log.error error.errorMessage
                | _ ->
                    Log.error error.Message
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

    | PartialChanged partialPath ->
        match partialPath with
        | Js ->
            let action (config, partialPath) =
                Partial.loadFromJavaScript config partialPath

            model
            , Cmd.OfPromise.either action (model.Config, partialPath) PartialLoaded InitPartialError

        | Jsx ->
            let action (config, partialPath) =
                Partial.loadPartialFromJsx config partialPath

            model
            , Cmd.OfPromise.either action (model.Config, partialPath) PartialLoaded InitPartialError

        | Other _ ->
            Log.error $"Partial files must be JavaScript or JSX files: %s{partialPath}"
            model, Cmd.none

    | PartialLoaded loadedPartial ->
        let newPartials =
            model.Partials
            |> List.tryFind (fun partial ->
                partial.Id = loadedPartial.Id
            )
            |> function
            // If the partial is already loaded, replace it
            | Some _ ->
                model.Partials
                |> List.map (fun partial ->
                    if partial.Id = loadedPartial.Id then
                        loadedPartial
                    else
                        partial
                )
            // If the partial is not loaded, add it
            | None ->
                loadedPartial :: model.Partials

        let newModel =
            { model with
                Partials = newPartials
            }

        // Regenerate all the pages
        // Right now the partials supported are for the footer or the navbar so it concerns all the pages
        let cmd =
            model.Pages
            |> List.map (fun page ->
                Cmd.ofMsg (ProcessMarkdown page)
            )
            |> Cmd.batch

        newModel
        , cmd

    | InitPartialError error ->
        Log.error error.Message

        model
        , Cmd.none
