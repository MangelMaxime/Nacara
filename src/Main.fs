module Main

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
importSideEffects "./js/markdown-it-anchored.js"
importSideEffects "./js/markdown-it-toc.js"

let processTableOfContent (pageContext : PageContext) =
    promise {
        let tocRegex = new Regex("""(<nav class="toc-container">.*<\/nav>)""")

        let tocContent =
            let result = tocRegex.Match(pageContext.Content)

            if result.Success then
                result.Value
            else
                """<nav class="toc-container"></nav>"""

        let pageContent =
            tocRegex.Replace(pageContext.Content, "")

        return { pageContext with TableOfContent = tocContent
                                  Content = pageContent }
    }

let rec replacePostJavaScript (importedModule : obj) (config : PostRenderDemos) (result : string) =
    promise {
        let jsRenderRegex = new Regex("\[@js:(.*)\]")

        match jsRenderRegex.Match(result) with
        | m when m.Success ->
            let toReplace = m.Groups.[0].Value
            let functioName : string = m.Groups.[1].Value

            try
                let htmlOutput =
                    importedModule?(config.ImportSelector)?(functioName)()
                    |> Helpers.parseReactStatic


                return result.Replace(toReplace, htmlOutput)
            with
                | _ ->
                    Log.warn "An error occured when trying to execute post JavaScript rendering"
                    Log.warn "Module path: %s" config.Script
                    Log.warn "Selector: %s" config.ImportSelector
                    Log.warn "Function: %s" functioName

                    return! result.Replace(toReplace, "")
                            |> replacePostJavaScript importedModule config
        | _ ->
            return result
    }

let processPostJavaScriptRender (pageContext : PageContext) =
    promise {
        match pageContext.Attributes.PostRenderDemos with
        | Some config ->
            let scriptPath =
                pageContext.Path
                |> Directory.dirname
                |> (fun dir -> Directory.join dir config.Script)
                |> File.absolutePath

            let importedModule : obj = require scriptPath

            let! newContent = replacePostJavaScript importedModule config pageContext.Content

            return { pageContext with Content = newContent }
        | None ->
            return pageContext
    }

let processCodeHighlights (lightnerConfig : Map<string, CodeLightner.Config>) (pageContext : PageContext) =
    let codeBlockRegex = Regex("""<pre\b[^>]*><code class="language-([^"]*)">([\S\s]*?)<\/code><\/pre>""", RegexOptions.Multiline)

    let rec apply searchIndex (text : string) =
        promise {
            let m = codeBlockRegex.Match(text, searchIndex)
            if m.Success then
                let lang = m.Groups.[1].Value
                match Map.tryFind lang lightnerConfig with
                | Some config ->
                    let codeText =
                         m.Groups.[2].Value
                        |> Helpers.unEscapeHTML
                        // Escape single `$` caracter otherwise vscode-textmaste inject
                        // source code at `$` place.
                        |> (fun str -> str.Replace("$", "$$"))
                    let! formattedText = CodeLightner.lighten config codeText
                    let text = text.Substring(0, m.Index) + formattedText + text.Substring(m.Index + m.Length)
                    return! apply (searchIndex + m.Length) text
                | None ->
                    Log.warnFn "No grammar found for language: `%s`" lang
                    return! apply (searchIndex + m.Length) text
            else
                return text
        }

    promise {
        let! processedContent = apply 0 pageContext.Content
        return { pageContext with Content = processedContent }
    }

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

type Msg =
    | ProcessAllFiles of string list
    | ProcessMarkdown of string
    | ProcessSass of string
    | ProcessChangelogResult of string * Result<Changelog.Types.Changelog, string>
    | ProcessMarkdownResult of Result<PageContext, string * string>
    | ProcessFailed of exn
    | WriteFileSuccess of string
    | WriteFileFailed of exn

module Process =

    let markdownFile (path : string, lightnerConfig : Map<string, CodeLightner.Config>) =
        promise {
            let! fileContent = File.read path
            let fm = FrontMatter.fm.Invoke(fileContent)

            match Decode.fromValue "$" PageAttributes.Decoder fm.attributes with
            | Error msg ->
                let errorMsg =
                    sprintf "The attributes of %s are invalid.\n%s" path msg
                return Error (path, errorMsg)
            | Ok pageAttributes ->
                let markdown = Helpers.markdown fm.body

                let! pageContext =
                    { Path = path
                      Attributes = pageAttributes
                      TableOfContent = ""
                      Content = markdown }
                    |> processTableOfContent
                    |> Promise.bind (processCodeHighlights lightnerConfig)
                    |> Promise.bind processPostJavaScriptRender

                return Ok pageContext
        }

    let changelog (path : string) =
        promise {
            let! fileContent = File.read path
            return path, Changelog.parse fileContent
        }

open Elmish

let baseUrlMiddleware (baseUrl : string) : LiveServer.Middleware = import "default" "./js/base-url-middleware.js"

let runServer (config : Config) =
    if not config.IsServer then None
    else
        // Start the LiveServer instance
        let liveServerOption =
            jsOptions<LiveServer.Options>(fun o ->
                o.root <- Node.Api.path.join(cwd, config.Output)
                o.``open`` <- false
                o.logLevel <- 0
                o.port <- config.ServerPort
            )

        if config.BaseUrl <> "/" then
            liveServerOption.middleware <- [|
                    baseUrlMiddleware config.BaseUrl
                |]

        let server = LiveServer.liveServer.start(liveServerOption)

        // We need to register in the event in order have access to the server info
        // Otherwise, the server isn't ready yet
        server.on("listening", (fun _ ->
            let address = server.address()
            Log.success "Server started at: http://%s:%i" address?address address?port
        ))
        |> ignore

        Some server

let processFile isDebug path dispatch =
    match path with
    | MarkdownFile ->
        ProcessMarkdown path |> dispatch
    | SassFile ->
        ProcessSass path |> dispatch
    | JavaScriptFile ->
        () // TODO: Refresh all the page using this file for post process
    | UnsupportedFile _ ->
        if isDebug then
            Log.warn "Unsupported file cannot be processed: %s" path

let init (config : Config, docFiles : Map<string, PageContext>, lightnerCache : Map<string,CodeLightner.Config>) =
    let cmd =
        if not config.IsWatch then
            Cmd.OfPromise.either (fun (config : Config) ->
                    Directory.getFiles true config.Source
                    |> Promise.map (fun paths ->
                        paths |> List.map (fun p -> Node.Api.path.join(config.Source, p))))
                config ProcessAllFiles ProcessFailed
        else Cmd.none

    { Config = config
      Server = runServer config
      WorkingDirectory = cwd
      IsDebug = config.IsDebug
      JavaScriptFiles = Dictionary<string, string>()
      DocFiles = docFiles
      LightnerCache = lightnerCache }, cmd

let update (msg : Msg) (model : Model) =
    match msg with
    | ProcessAllFiles filePaths ->
        let cmd1 = filePaths |> List.map (processFile model.IsDebug)
        let cmd2 =
            match model.Config.Changelog with
            | Some changelogPath ->
                Cmd.OfPromise.either Process.changelog changelogPath ProcessChangelogResult ProcessFailed
            | None -> Cmd.none
        model, Cmd.batch [cmd1; cmd2]

    | ProcessMarkdown filePath ->
        let cmd =
            match model.Config.Changelog with
            | Some changelogPath when filePath = changelogPath ->
                Cmd.OfPromise.either Process.changelog filePath ProcessChangelogResult ProcessFailed
            | _ ->
                Cmd.OfPromise.either Process.markdownFile (filePath, model.LightnerCache) ProcessMarkdownResult ProcessFailed

        model, cmd

    | ProcessFailed error ->
        Log.error "%s" error.Message
        model, Cmd.none

    | ProcessSass filePath ->
        model, Cmd.OfPromise.either Write.sassFile (model, filePath) WriteFileSuccess WriteFileFailed

    | ProcessMarkdownResult result ->
        match result with
        | Error (path, msg) ->
            Log.error "Error when processing file: %s\n%s" path msg
            model, Cmd.none

        | Ok pageContext ->
            let pageId = getFileId model.Config.Source pageContext
            let newDocFiles = Map.add pageId pageContext model.DocFiles
            let newModel = { model with DocFiles = newDocFiles }
            newModel, Cmd.OfPromise.either Write.standard (newModel, pageContext) WriteFileSuccess WriteFileFailed

    | ProcessChangelogResult (path, result) ->
        match result with
        | Error msg ->
            Log.error "Error when processing file: %s\n%s" path msg
            model, Cmd.none

        | Ok changelog ->
            model, Cmd.OfPromise.either Write.changelog (model,changelog, path) WriteFileSuccess WriteFileFailed

    | WriteFileSuccess path ->
        Log.log "Write: %s" path
        model, Cmd.none

    | WriteFileFailed error ->
        Log.error "Error when writting a file:\n%s" error.Message
        model, Cmd.none

let fileWatcherSubscription (model : Model) =
    let handler (fileWatcher: Chokidar.FSWatcher) dispatch =
        match model.Config.Changelog with
        | Some filePath ->
            fileWatcher.add(filePath)
        | None -> ()

        // Register behavior when:
        // - a new file is added to the `Source` directory
        // - a tracked file change
        fileWatcher.on(Chokidar.Events.All, (fun event path ->
            match event with
            | Chokidar.Events.Add
            | Chokidar.Events.Change ->
                processFile model.IsDebug path dispatch
            | Chokidar.Events.Unlink
            | Chokidar.Events.UnlinkDir
            | Chokidar.Events.Ready
            | Chokidar.Events.Raw
            | Chokidar.Events.Error
            | Chokidar.Events.AddDir
            | _ -> ()
        ))

    if model.Config.IsWatch then
        [ chokidar.watch(model.Config.Source) |> handler ]
    else []

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
                        TableOfContent = ""
                        Content = fm.body }
    }

let checkCliArgs (config: Config) =
    let args = Node.Api.``process``.argv |> Seq.skip 2 |> Seq.toList
    let ifHasFlag (flags: string list) f (config: Config) =
        args |> List.exists (fun a -> flags |> List.exists (fun f -> a = f))
        |> function true -> f config | false -> config

    config
    |> ifHasFlag ["--watch"; "-w"] (fun c -> { c with IsWatch = true })
    |> ifHasFlag ["--server"] (fun c -> { c with IsServer = true })

let start () =
    promise {
        let configPath = Node.Api.path.join(cwd, "nacara.json")
        let! hasDocsConfig = File.exist(configPath)

        if hasDocsConfig then
            let! fileContent = File.read configPath
            match Decode.fromString Config.Decoder fileContent  with
            | Ok config ->
                let config = checkCliArgs config

                let! files = Directory.getFiles true config.Source

                let! pageContexts =
                    files
                    |> List.map (fun file ->
                        config.Source.TrimEnd('/', '\\') + "/" + file
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
                    Helpers.resultPartition pageContexts

                for (path, msg) in erroredContext do
                    Log.error "Error when processing file: %s\n%s" path msg

                let docFiles =
                    validContext
                    |> Array.map (fun pageContext ->
                        getFileId config.Source pageContext, pageContext)
                    |> Map.ofArray

                let lightnerCache =
                    match config.LightnerConfig with
                    | Some lightnerConfig ->
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
                        |> Map.ofList
                    | None ->
                        Map.empty

                Program.mkProgram init update (fun _ _ -> ())
                |> Program.withSubscription fileWatcherSubscription
                |> Program.runWith (config, docFiles, lightnerCache)

            | Error msg ->
                Log.error "Your config file seems invalid."
                Log.errorFn "%s" msg
        else
            Log.error "No file `nacara.json` found."
            Node.Api.``process``.exit(1)
    }
    |> Promise.start
