module Main

open Fable.Import

open Fable.Core
open Fable.Core.JsInterop
open Thoth.Json
open Nacara.Core.Types
open Elmish
open Node

let cwd = ``process``.cwd()

type Model =
    | Initializing
    | ServerMode
    | BuildMode

type Msg =
    |NoOp

let private init () =
    Initializing
    , Cmd.none

let private update (msg : Msg) (model : Model) =
    match msg with
    | NoOp ->
        model
        , Cmd.none

type FilesAccumulator =
    {
        MarkdownFiles : string list
        MenuFiles : string list
        SassFiles : string list
        JavaScriptFiles : string list
        PartialFiles : string list
        OtherFiles : string list
    }

    static member Empty =
        {
            MarkdownFiles = []
            MenuFiles = []
            SassFiles = []
            JavaScriptFiles = []
            PartialFiles = []
            OtherFiles = []
        }

let private buildOrWatch (isWatch : bool) (config : Config) =
    promise {
        Log.log $"Source folder: %s{config.SourceFolder}"

        // Clean the output folder
        do! Clean.clean config

        // The config so now load the files from the source folder
        let! files = Directory.getFiles true config.SourceFolder

        // For the markdown files, initialize their context
        let files =
            files
            |> List.fold (fun acc path ->
                match path with
                | MarkdownFile ->
                    { acc with
                        MarkdownFiles = path :: acc.MarkdownFiles
                    }

                | JavaScriptFile ->
                    { acc with
                        JavaScriptFiles = path :: acc.JavaScriptFiles
                    }

                | PartialFile ->
                    { acc with
                        PartialFiles = path :: acc.PartialFiles
                    }

                | SassFile ->
                    // Ignore files under special folders
                    if path.Replace("\\", "/").StartsWith("scss/")
                        || path.Replace("\\", "/").StartsWith("sass/") then

                        acc

                    else
                        { acc with
                            SassFiles = path :: acc.SassFiles
                        }

                | MenuFile ->
                    { acc with
                        MenuFiles = path :: acc.MenuFiles
                    }

                | OtherFile _ ->
                    { acc with
                        OtherFiles = path :: acc.OtherFiles
                    }
            ) FilesAccumulator.Empty

        let! pageContextResults =
            files.MarkdownFiles
            |> List.map (initPageContext config.SourceFolder)
            |> Promise.all

        let (validPageContext, erroredPageContext) =
            pageContextResults
            |> Array.partitionMap (fun x ->
                match x with
                | Ok validPageContext ->
                    Choice1Of2 validPageContext

                | Error errorMessage ->
                    Choice2Of2 errorMessage
            )

        let! layouts =
            config.Layouts
            |> Array.map (Layout.load config)
            |> Promise.all

        let! menuFiles =
            files.MenuFiles
            |> List.map (initMenuFiles config.SourceFolder)
            |> Promise.all

        let (validMenuFiles, erroredMenuFiles) =
            menuFiles
            |> Array.partitionMap (fun x ->
                match x with
                | Ok validMenuFile ->
                    Choice1Of2 validMenuFile

                | Error errorMessage ->
                    Choice2Of2 errorMessage
            )

        let layoutDependencies =
            layouts
            |> Array.collect (fun layout ->
                layout.Dependencies
            )
            |> Array.toList

        let! partials =
            files.PartialFiles
            |> List.filter (fun path ->
                match path with
                | Js
                | Jsx ->
                    true
                | Other _ ->
                    Log.error $"Partial files must be JavaScript or JSX files: %s{path}"
                    false
            )
            |> List.map (fun partial ->
                match partial with
                | Js ->
                    Partial.loadFromJavaScript config partial

                | Jsx ->
                    Partial.loadPartialFromJsx config partial

                | Other _ ->
                    failwith "Should not happen, partials files have been filtered before"
            )
            |> Promise.all

        let processQueue =
            [
                for sassFile in files.SassFiles do
                    QueueFile.Sass sassFile

                for javaScriptFile in files.JavaScriptFiles do
                    QueueFile.JavaScript javaScriptFile

                for otherFile in files.OtherFiles do
                    QueueFile.Other otherFile

                for markdownFile in validPageContext do
                    QueueFile.Markdown markdownFile

                for layoutDependency in layoutDependencies do
                    QueueFile.LayoutDependency layoutDependency
            ]

        if isWatch then
            let elmishArgs : Watch.InitArgs =
                {
                    ProcessQueue = processQueue
                    Layouts = layouts
                    Config = config
                    Pages = validPageContext |> Array.toList
                    Menus = validMenuFiles |> Array.toList
                    Partials = partials |> Array.toList
                }

            Program.mkProgram Watch.init Watch.update (fun _ _ -> ())
            |> Program.withSubscription Watch.fileWatcherSubscription
            |> Program.withSubscription Watch.layoutDependencyWatcherSubscription
            |> Program.withSubscription Watch.layoutSourcesWatcherSubscription
            |> Program.runWith elmishArgs

        else
            // In build mode we are more strict about the initial context because we can't recover from it
            // All files should be valid otherwise stop the generation and report an error
            if erroredPageContext.Length > 0 then
                for errorMessage in erroredPageContext do
                    Log.error errorMessage

                ``process``.exit(ExitCode.INVALID_MARKDOWN_FILE_IN_BUILD_MODE)

            else

                let elmishArgs : Build.InitArgs =
                    {
                        Layouts = layouts
                        Config = config
                        ProcessQueue = processQueue
                        Pages = validPageContext |> Array.toList
                        Menus = validMenuFiles |> Array.toList
                        Partials = partials |> Array.toList
                    }

                Program.mkProgram Build.init Build.update (fun _ _ -> ())
                |> Program.runWith elmishArgs
    }

let initialize
    (isWatch : bool)
    (func : Config -> JS.Promise<unit>) =
    promise {
        Log.info $"Current directory:\n%s{cwd}"

        let nacaraJsonConfigPath = path.join(cwd, "nacara.confi2g.json")
        let nacaraJsConfigPath = path.join(cwd, "nacara.config.js")

        let! hasJsonConfig = File.exist(nacaraJsonConfigPath)
        let! hasJsConfig = File.exist(nacaraJsConfigPath)

        // Check if the Nacara JSON config file exist
        if hasJsonConfig then
            let! configJson = File.read nacaraJsonConfigPath

            // Check if the Nacara config file is valid
            match Decode.fromString (Config.decoder cwd isWatch) configJson with
            | Ok config ->
                do! func config

            | Error errorMessage ->
                Log.error $"Invalid config file. Error:\n{errorMessage}"
                ``process``.exit(ExitCode.INVALID_CONFIG_FILE)

        // Try to find the config as a JS file
        else if hasJsConfig then
            let! configInstance =
                // './' is important to treat the import as a relative file
                // and not an NPM package
                Interop.importDynamic cwd "./nacara.config.js"

            // Check if the Nacara config file is valid
            match Decode.fromValue "$" (Config.decoder cwd isWatch) configInstance?``default`` with
            | Ok config ->
                do! func config

            | Error errorMessage ->
                Log.error $"Invalid config file. Error:\n{errorMessage}"
                ``process``.exit(ExitCode.INVALID_CONFIG_FILE)

        // No config file found
        else
            Log.error "Missing 'nacara.config.json' file"
            ``process``.exit(ExitCode.MISSING_CONFIG_FILE)
    }

let runBuild _ =
    initialize false (buildOrWatch false)


let runWatch _ =
    initialize true (buildOrWatch true)

let runClean _ =
    let func config =
        promise {
            do! Clean.clean config
            Log.success $"Successfully removed generated files"
            ``process``.exit ExitCode.OK
        }

    initialize false func

let runServe _ =
    initialize false Serve.serve
