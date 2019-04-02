module Main

open Fable.Import

open Fable.Core
open Fable.Core.JsInterop
open Fulma
open Fable.Helpers.React
open Fable.Helpers.React.Props
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
    let codeBlockRegex = JS.RegExp.Create("""<pre\b[^>]*><code class="language-([^"]*)">(.*?)<\/code><\/pre>""", "gms")

    let rec apply (text : string) =
        promise {
            let m = codeBlockRegex.exec pageContext.Content
            if isNotNull m then
                let wholeText = m.[0]
                let lang = m.[1]
                let codeText =
                    m.[2]
                    |> Helpers.unEscapeHTML
                    // Escape single `$` caracter otherwise vscode-textmaste inject
                    // source code at `$` place.
                    |> (fun str -> str.Replace("$", "$$"))

                match Map.tryFind lang lightnerConfig with
                | Some config ->
                    let! formattedText = CodeLightner.lighten config codeText
                    return! text.Replace(wholeText, formattedText)
                            |> apply
                | None ->
                    Log.warnFn "No grammar found for language: `%s`" lang
                    return! pageContext.Content
                            |> apply
            else
                return text
        }

    promise {
        let! processedContent = apply pageContext.Content
        return { pageContext with Content = processedContent }
    }

let cwd = Node.Globals.``process``.cwd()

Log.infoFn "Current directory:\n%s" cwd

open Chokidar

let (|MarkdownFile|JavaScriptFile|SassFile|UnsupportedFile|) (path : string) =
    let ext = Node.Exports.path.extname(path)

    match ext.ToLower() with
    | ".md" -> MarkdownFile
    | ".js" -> JavaScriptFile
    | ".scss" | ".sass" -> SassFile
    | _ -> UnsupportedFile ext

type Msg =
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

let init (config : Config, docFiles : Map<string, PageContext>, lightnerCache : Map<string,CodeLightner.Config>) =
    // Start the LiveServer instance
    let liveServerOption =
        jsOptions<LiveServer.Options>(fun o ->
            o.root <- Node.Exports.path.join(cwd, config.Output)
            o.``open`` <- false
            o.logLevel <- 0
        )

    let server = LiveServer.liveServer.start(liveServerOption)

    // We need to register in the event in order have access to the server info
    // Otherwise, the server isn't ready yet
    server.on("listening", (fun _ ->
        let address = server.address()
        Log.success "Server started at: http://%s:%i" address?address address?port
    ))
    |> ignore

    { Config = config
      FileWatcher = chokidar.watch(config.Source)
      Server = server
      WorkingDirectory = cwd
      IsDebug = config.IsDebug
      JavaScriptFiles = Dictionary<string, string>()
      DocFiles = Map.empty
      LightnerCache = lightnerCache }, Cmd.none

let update (msg : Msg) (model : Model) =
    match msg with
    | ProcessMarkdown filePath ->
        let cmd =
            match model.Config.Changelog with
            | Some changelogPath ->
                if filePath = changelogPath then
                    Cmd.ofPromise Process.changelog filePath ProcessChangelogResult ProcessFailed
                else
                    Cmd.ofPromise Process.markdownFile (filePath, model.LightnerCache) ProcessMarkdownResult ProcessFailed
            | None ->
                Cmd.ofPromise Process.markdownFile (filePath, model.LightnerCache) ProcessMarkdownResult ProcessFailed

        model, cmd

    | ProcessFailed error ->
        Log.error "%s" error.Message
        model, Cmd.none

    | ProcessSass filePath ->
        model, Cmd.ofPromise Write.sassFile (model, filePath) WriteFileSuccess WriteFileFailed

    | ProcessMarkdownResult result ->
        match result with
        | Error (path, msg) ->
            Log.error "Error when processing file: %s\n%s" path msg
            model, Cmd.none

        | Ok pageContext ->
            let newDocFiles = Map.add pageContext.Path pageContext model.DocFiles
            { model with DocFiles = newDocFiles }, Cmd.ofPromise Write.standard (model, pageContext) WriteFileSuccess WriteFileFailed

    | ProcessChangelogResult (path, result) ->
        match result with
        | Error msg ->
            Log.error "Error when processing file: %s\n%s" path msg
            model, Cmd.none

        | Ok changelog ->
            model, Cmd.ofPromise Write.changelog (model,changelog, path) WriteFileSuccess WriteFileFailed

    | WriteFileSuccess path ->
        Log.log "Write: %s" path
        model, Cmd.none

    | WriteFileFailed error ->
        Log.error "Error when writting a file:\n%s" error.Message
        model, Cmd.none

let fileWatcherSubscription (model : Model) =
    let handler dispatch =
        match model.Config.Changelog with
        | Some filePath ->
            model.FileWatcher.add(filePath)
        | None -> ()

        // Register behavior when:
        // - a new file is added to the `Source` directory
        // - a tracked file change
        model.FileWatcher.on(Chokidar.Events.All, (fun event path ->
            match event with
            | Chokidar.Events.Add
            | Chokidar.Events.Change ->
                match path with
                | MarkdownFile ->
                    ProcessMarkdown path
                    |> dispatch
                | JavaScriptFile -> () // TODO: Refresh all the page using this file for post process
                | SassFile ->
                    ProcessSass path
                    |> dispatch
                | UnsupportedFile _ ->
                    if model.IsDebug then
                        Log.warn "Watcher has been triggered on an unsupported file: %s" path
            | Chokidar.Events.Unlink
            | Chokidar.Events.UnlinkDir
            | Chokidar.Events.Ready
            | Chokidar.Events.Raw
            | Chokidar.Events.Error
            | Chokidar.Events.AddDir
            | _ -> ()
        ))

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
            return Ok (path, { Path = path
                               Attributes = pageAttributes
                               TableOfContent = ""
                               Content = fm.body })
    }

promise {
    let configPath = Node.Exports.path.join(cwd, "nacara.json")
    let! hasDocsConfig = File.exist(configPath)

    if hasDocsConfig then
        let! fileContent = File.read configPath
        match Decode.fromString Config.Decoder fileContent  with
        | Ok config ->
            let! files = Directory.getFiles config.Source

            let! pageContexts =
                files
                |> Array.map (fun file ->
                    config.Source + "/" + file
                )
                // Remove directories from the list
                |> Array.filter (fun path ->
                    let stats = File.statsSync path
                    stats.isDirectory()
                    |> not
                )
                |> Array.map tryBuildPageContext
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
                    | Ok (path, pageContext) ->
                        (path, pageContext)
                    | Error _ -> failwith "Should not happen we filtered them before"
                )
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
        Node.Globals.``process``.exit(1)
}
|> Promise.start
