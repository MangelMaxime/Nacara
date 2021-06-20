module Main

open Fable.Import

open Fable.Core
open Fable.Core.JsInterop
open Fulma
open Fable.React
open Fable.React.Props
open Thoth.Json
open Types
open System

open System.Text.RegularExpressions
open System.Collections.Generic

// We use importSideEffects so the files are included in the output by fable-splitter
// importSideEffects "./../js/markdown-it-anchored.js"
// importSideEffects "./../js/markdown-it-toc.js"

// let processCodeHighlights (lightnerConfig : Map<string, CodeLightner.Config>) (pageContext : PageContext) =
//     let codeBlockRegex =
//         // Regex("""<pre\b[^>]*><code class="language-([^"]*)">(.*?)<\/code><\/pre>""", RegexOptions.Multiline ||| RegexOptions.Singleline)
//         JS.Constructors.RegExp.Create("""<pre\b[^>]*><code class="language-([^"]*)">(.*?)<\/code><\/pre>""", "gms")

//     let rec apply (text : string) =
//         promise {
//             let m = codeBlockRegex.Match pageContext.Content
//             if m.Success then
//                 let wholeText = m.Groups.[0].Value
//                 let lang = m.Groups.[1].Value
//                 let codeText =
//                     m.Groups.[2].Value
//                     |> Helpers.unEscapeHTML
//                     // Escape single `$` caracter otherwise vscode-textmaste inject
//                     // source code at `$` place.
//                     |> (fun str -> str.Replace("$", "$$"))

//                 match Map.tryFind lang lightnerConfig with
//                 | Some config ->
//                     let! formattedText = CodeLightner.lighten config codeText
//                     return! text.Replace(wholeText, formattedText)
//                             |> apply
//                 | None ->
//                     Log.warnFn "No grammar found for language: `%s`" lang
//                     return! pageContext.Content
//                             |> apply
//             else
//                 return text
//         }

//     promise {
//         let! processedContent = apply pageContext.Content
//         return { pageContext with Content = processedContent }
//     }

let cwd = Node.Api.``process``.cwd()

Log.infoFn "Current directory:\n%s" cwd

open Chokidar

let (|MarkdownFile|JavaScriptFile|SassFile|UnsupportedFile|) (path : string) =
    let ext = Node.Api.path.extname(path)

    match ext.ToLower() with
    | ".md" -> MarkdownFile
    | ".js" -> JavaScriptFile
    | ".scss" | ".sass" -> SassFile
    | _ -> UnsupportedFile ext

[<NoComparison>]
type Msg =
    | ProcessBuildMode
    | ProcessMarkdown of string
    | ProcessSass of string
    | ProcessMarkdownResult of Result<PageContext, string * string>
    | ProcessFailed of exn
    | ProcessUnkownFile of string
    | WriteFileSuccess of string
    | WriteFileFailed of exn
    | ProgramExitFailed of exn

let processFile (path : string, model : Model) =
    promise {
        let! fileContent = File.read path
        let fm = FrontMatter.fm.Invoke(fileContent)

        match Decode.fromValue "$" PageAttributes.Decoder fm.attributes with
        | Error msg ->
            let errorMsg =
                sprintf "The attributes of %s are invalid.\n%s" path msg
            return Error (path, errorMsg)

        | Ok pageAttributes ->
            match Map.tryFind pageAttributes.Layout model.Config.LayoutConfig with
            | Some layoutInfo ->
                let pageContext =
                    {
                        Path = path
                        Attributes = pageAttributes
                        Content = fm.body
                        StaticRessources = layoutInfo.ScriptDependencies
                    }

                let! layout = layoutInfo.RenderFunc.Invoke(model, pageContext)

                let result =
                    { pageContext with
                        Content =
                            Helpers.parseReactStatic layout
                    }

                return Ok result

            | None ->
                let errorMsg =
                    sprintf "No layout '%s' found in your config file." pageAttributes.Layout
                return Error (path, errorMsg)
    }

open Elmish

let baseUrlMiddleware (baseUrl : string) : LiveServer.Middleware = import "default" "./../js/base-url-middleware.js"

let private startServerIfNeeded (config : Config) =
    if config.IsWatch then
        // Start the LiveServer instance
        let liveServerOption =
            jsOptions<LiveServer.Options>(fun o ->
                o.root <- Node.Api.path.join(cwd, config.Output)
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
            let address = server.address()
            Log.success "Server started at: http://%s:%i" address?address address?port
        ))
        |> ignore

        Some server
    else
        None

let startFileWatcherIfNeeded (config : Config) =
    if config.IsWatch then
        Some (chokidar.watch(config.Source))
    else
        None

let init (config : Config, processQueue : string list, docFiles : JS.Map<string, PageContext>, lightnerCache : JS.Map<string,CodeLightner.Config>) =
    let cmd =
        if config.IsWatch then
            Cmd.none
        else
            Cmd.ofMsg ProcessBuildMode

    {
        ProcessQueue = processQueue
        Config = config
        FileWatcher = startFileWatcherIfNeeded config
        Server = startServerIfNeeded config
        WorkingDirectory = cwd
        IsDebug = config.IsDebug
        DocFiles = docFiles
        LightnerCache = lightnerCache
        StaticRessources = [ ]
    }
    , cmd

let update (msg : Msg) (model : Model) =
    match msg with
    | ProcessMarkdown filePath ->
        model
        , Cmd.OfPromise.either processFile (filePath, model) ProcessMarkdownResult ProcessFailed

    | ProcessFailed error ->
        Log.error "%s" error.Message
        model, Cmd.none

    | ProcessSass filePath ->
        model
        , Cmd.OfPromise.either Write.sassFile (model, filePath) WriteFileSuccess WriteFileFailed

    | ProcessMarkdownResult result ->
        match result with
        | Error (path, msg) ->
            Log.error "Error when processing file: %s\n%s" path msg
            model, Cmd.none

        | Ok pageContext ->
            let pageId = getFileId model.Config.Source pageContext
            let newDocFiles = model.DocFiles.set(pageId, pageContext) // Map.add pageId pageContext model.DocFiles

            let newStaticRessources =
                List.except model.StaticRessources pageContext.StaticRessources

            let staticRessourcesCmd =
                if newStaticRessources.IsEmpty then
                    Cmd.none
                else
                    newStaticRessources
                    |> List.map (fun source ->
                        let fileName =
                            Node.Api.path.basename(source)

                        let layoutName =
                            match Map.tryFind pageContext.Attributes.Layout model.Config.LayoutConfig with
                            | Some layoutInfo ->
                                Some layoutInfo.LayoutName

                            | None ->
                                Log.errorFn "No layout '%s' found in your config file." pageContext.Attributes.Layout
                                None

                        match layoutName with
                        | Some layoutName ->
                            let destination =
                                Node.Api.path.join(
                                    model.WorkingDirectory,
                                    model.Config.Output,
                                    "static",
                                    layoutName
                                )

                            Cmd.OfPromise.attempt
                                (fun (source, destinationFolder, fileName) ->
                                    promise {
                                        do! Directory.create destinationFolder

                                        let destinationFullPath =
                                            Node.Api.path.join(destinationFolder, fileName)

                                        Node.Api.fs.copyFileSync(source, destinationFullPath)
                                    }
                                )
                                (source, destination, fileName)
                                WriteFileFailed
                        | None ->
                            Cmd.none

                    )
                    |> Cmd.batch

            let newModel =
                { model with
                    DocFiles = newDocFiles
                    StaticRessources = model.StaticRessources @ newStaticRessources
                }

            newModel
            , Cmd.batch [
                Cmd.OfPromise.either Write.standard (newModel, pageContext) WriteFileSuccess WriteFileFailed
                staticRessourcesCmd
            ]

    // Copy unkown file by keeping the folder structure
    | ProcessUnkownFile source ->
        let sourcePath = Node.Api.path.dirname source
        let fileName = Node.Api.path.basename source
        let docFolder = Node.Api.path.join(model.WorkingDirectory, model.Config.Source)
        let relativeSourcePath = Node.Api.path.relative(docFolder, sourcePath)

        let destination =
            Node.Api.path.join(
                model.WorkingDirectory,
                model.Config.Output,
                relativeSourcePath
            )

        let cmd =
            Cmd.OfPromise.attempt
                (fun (source, destinationFolder, fileName) ->
                    promise {
                        do! Directory.create destinationFolder

                        let destinationFullPath =
                            Node.Api.path.join(destinationFolder, fileName)

                        Node.Api.fs.copyFileSync(source, destinationFullPath)
                    }
                )
                (source, destination, fileName)
                WriteFileFailed

        model
        , cmd

    | WriteFileSuccess path ->
        Log.log "Write: %s" path

        let cmd =
            if model.Config.IsWatch then
                Cmd.none
            else
                Cmd.ofMsg ProcessBuildMode

        model, cmd

    | WriteFileFailed error ->
        Log.error "Error when writting a file:\n%s" error.Message
        model, Cmd.none

    | ProcessBuildMode ->
        match model.ProcessQueue with
        | filePath::tail ->
            let cmd =
                match filePath with
                | MarkdownFile ->
                    Cmd.ofMsg (ProcessMarkdown filePath)

                | JavaScriptFile ->
                    Cmd.ofMsg ProcessBuildMode // TODO: Refresh all the page using this file for post process

                | SassFile ->
                    Cmd.ofMsg (ProcessSass filePath)
                | UnsupportedFile _ ->
                    if model.IsDebug then
                        Log.warn "Watcher has been triggered on an unsupported file: %s" filePath
                    Cmd.ofMsg ProcessBuildMode

            { model with
                ProcessQueue = tail
            }
            , cmd

        | [ ] ->
            // All files has been process, kill the process as it was launch in Build mode
            let exit () =
                Node.Api.``process``.exit(0)

            Log.success "Generation done, exiting..."
            model
            , Cmd.OfFunc.attempt exit () ProgramExitFailed

    | ProgramExitFailed error ->
        if model.IsDebug then
            Log.errorFn "%A" error

        Log.error "Failed to exit the process, please kill it manually using `Ctrl+C`"
        model, Cmd.none


let fileWatcherSubscription (model : Model) =
    let handler dispatch =
        // Register behavior when:
        // - a new file is added to the `Source` directory
        // - a tracked file change
        match model.FileWatcher with
        | Some fileWatcher ->
            fileWatcher.on(Chokidar.Events.All, (fun event path ->
                match event with
                | Chokidar.Events.Add
                | Chokidar.Events.Change ->
                    match path with
                    | MarkdownFile ->
                        ProcessMarkdown path
                        |> dispatch
                    // | JavaScriptFile -> () // TODO: Refresh all the page using this file for post process
                    | SassFile ->
                        ProcessSass path
                        |> dispatch
                    | JavaScriptFile
                    | UnsupportedFile _ ->
                        ProcessUnkownFile path
                        |> dispatch
                        // if model.IsDebug then
                        //     Log.warn "Watcher has been triggered on an unsupported file: %s" path
                | Chokidar.Events.Unlink
                | Chokidar.Events.UnlinkDir
                | Chokidar.Events.Ready
                | Chokidar.Events.Raw
                | Chokidar.Events.Error
                | Chokidar.Events.AddDir
                | _ -> ()
            ))
        | None ->
            ()

    [ handler ]

let tryBuildPageContext (path : string) =
    promise {
        let! fileContent = File.read path
        let fm = FrontMatter.fm.Invoke(fileContent)

        match Decode.fromValue "$" PageAttributes.Decoder fm.attributes with
        | Error msg ->
            let errorMsg =
                sprintf "The attributes of %s are invalid.\n%s" path msg
            return Error (path, errorMsg)
        | Ok pageAttributes ->
            return Ok { Path = path
                        Attributes = pageAttributes
                        Content = fm.body
                        StaticRessources = [ ]
                         }
    }

let checkCliArgs (config: Config) =
    let args =
        Node.Api.``process``.argv
        // 0. Is node program
        // 1. Is the JavaScript file
        |> Seq.skip 2
        |> Seq.toList

    let onFlag (flags: string list) func (config: Config) =
        args
        |> List.exists (fun a ->
            flags
            |> List.exists (fun flag -> a = flag)
        )
        |> function
            | true -> func config
            | false -> config

    config
    |> onFlag ["--watch"; "-w"] (fun c -> { c with IsWatch = true })

let start () =
    promise {
        let configPath = Node.Api.path.join(cwd, "nacara.config.js")
        let! hasDocsConfig = File.exist(configPath)

        if hasDocsConfig then
            let importedModule : obj = require configPath
            match Decode.fromValue "$" Config.Decoder importedModule  with
            | Ok config ->
                let config = checkCliArgs config

                let! files = Directory.getFiles true config.Source

                let! pageContexts =
                    files
                    |> List.map (fun file ->
                        config.Source + "/" + file
                    )
                    |> List.filter (fun path ->
                        match path with
                        | MarkdownFile ->
                            true
                        | JavaScriptFile
                        | SassFile
                        | UnsupportedFile _ ->
                            false
                    )
                    |> List.map tryBuildPageContext
                    |> Promise.all

                let (validContext, erroredContext) =
                    pageContexts
                    |> Array.partition (fun result ->
                        match result with
                        | Ok _ -> true
                        | Error _ -> false
                    )

                erroredContext
                |> Array.iter (function
                    | Error (path, msg) ->
                        Log.error "Error when processing file: %s\n%s" path msg
                    | Ok _ -> failwith "Should not happen we filtered them before"
                )

                let docFiles =
                    validContext
                    |> Array.map (function
                        | Ok pageContext ->
                            let id = getFileId config.Source pageContext
                            (id, pageContext)
                        | Error _ -> failwith "Should not happen we filtered them before"
                    )

                let docFiles =
                    JS.Constructors.Map.Create(docFiles)

                let lightnerCache =
                    match config.LightnerConfig with
                    | Some lightnerConfig ->
                        let lightnerConfig =
                            lightnerConfig.GrammarFiles
                            |> List.map (fun filePath ->
                                if File.existSync filePath then
                                    let grammarText = File.readSync filePath
                                    match Decode.fromString (Decode.field "scopeName" Decode.string) grammarText with
                                    | Ok scopeName ->
                                        Some (scopeName, filePath)
                                    | Error msg ->
                                        Log.error "Unable to find `scopeName` in `%s`.\Sub decoder error:\n%s" filePath msg
                                        None
                                else
                                    Log.error "File not found: %s" filePath
                                    None
                            )
                            |> List.filter Option.isSome
                            |> List.map (function
                                | Some (scopeName, grammarPath) ->
                                    let config =
                                        jsOptions<CodeLightner.Config>(fun o ->
                                            o.backgroundColor <- lightnerConfig.BackgroundColor
                                            o.textColor <- lightnerConfig.TextColor
                                            o.themeFile <- lightnerConfig.ThemeFile
                                            o.scopeName <- scopeName
                                            o.grammarFiles <- [| Directory.join cwd grammarPath |]
                                        )
                                    scopeName.Split('.').[1], config
                                | None -> failwith "Should not happen, we filtered the list before"
                            )
                            |> List.toArray

                        JS.Constructors.Map.Create(lightnerConfig)

                    | None ->
                        JS.Constructors.Map.Create()

                let processQueue =
                    if config.IsWatch then
                        [ ]
                    else
                        files
                        |> List.map (fun filePath ->
                            Directory.join config.Source filePath
                        )

                Program.mkProgram init update (fun _ _ -> ())
                |> Program.withSubscription fileWatcherSubscription
                |> Program.runWith (config, processQueue, docFiles, lightnerCache)

            | Error msg ->
                Log.error "Your config file seems invalid."
                Log.errorFn "%s" msg
        else
            Log.error "File `nacara.config.js` not found."
            Node.Api.``process``.exit(1)
    }
    |> Promise.start
