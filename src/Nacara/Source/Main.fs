module Main

open Fable.Import

open Fable.Core
open Fable.Core.JsInterop
open Thoth.Json
open Nacara.Core.Types
open Elmish
open Node

let cwd = ``process``.cwd()

Log.info $"Current directory:\n%s{cwd}"

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

let private initLighterCache (config : Config) =
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
                        Log.error $"Unable to find `scopeName` in `%s{filePath}`.\Sub decoder error:\n%s{msg}"
                        None
                else
                    Log.error $"File not found: %s{filePath}"
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

type FilesAccumulator =
    {
        MarkdownFiles : string list
        MenuFiles : string list
        SassFiles : string list
        JavaScriptFile : string list
        OtherFiles : string list
    }

    static member Empty =
        {
            MarkdownFiles = []
            MenuFiles = []
            SassFiles = []
            JavaScriptFile = []
            OtherFiles = []
        }

let private cliArgs =
    ``process``.argv
    // 0. Is node program
    // 1. Is the JavaScript file
    |> Seq.skip 2
    |> Seq.toList

let private hasBoolArgs (flags: string list) =
    cliArgs
    |> List.exists (fun a ->
        flags
        |> List.exists (fun flag -> a = flag)
    )

let private isWatch =
    hasBoolArgs ["--watch"; "-w"]

let start () =
    promise {
        let configPath = Node.Api.path.join(cwd, "nacara.config.json")
        let! hasDocsConfig = File.exist(configPath)

        // Check if the Nacara config file exist
        if hasDocsConfig then
            let! configJson = File.read configPath
            // Check if the Nacara config file is valid
            match Decode.fromString (Config.decoder cwd isWatch) configJson with
            | Ok config ->
                Log.log $"Source folder: %s{config.SourceFolder}"

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
                                JavaScriptFile = path :: acc.JavaScriptFile
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

                let lightnerCache = initLighterCache config

                let layouts =
                    config.Layouts
                    |> Array.map (fun layoutPath ->
                        let layout : LayoutInterface =
                            // The path is relative, so load it relatively from the CWD
                            if layoutPath.StartsWith("./") then
                                let newPath =
                                    path.join(cwd, layoutPath)

                                require.Invoke newPath |> unbox
                            // The path is not relative, require it as an npm module
                            else
                                require.Invoke layoutPath |> unbox

                        layout.``default``
                    )

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

                let processQueue =
                    [
                        for sassFile in files.SassFiles do
                            QueueFile.Sass sassFile

                        for javaScriptFile in files.JavaScriptFile do
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
                            LightnerCache = lightnerCache
                        }

                    Program.mkProgram Watch.init Watch.update (fun _ _ -> ())
                    |> Program.withSubscription Watch.fileWatcherSubscription
                    |> Program.withSubscription Watch.layoutDependencyWatcherSubscription
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
                                LightnerCache = lightnerCache
                            }

                        Program.mkProgram Build.init Build.update (fun _ _ -> ())
                        |> Program.runWith elmishArgs

            | Error errorMessage ->
                Log.error $"Invalid config file. Error:\n{errorMessage}"
                ``process``.exit(ExitCode.INVALID_CONFIG_FILE)
        else
            Log.error "Missing 'nacara.config.json' file"
            ``process``.exit(ExitCode.MISSING_CONFIG_FILE)
        return ()
    }
    |> Promise.start
