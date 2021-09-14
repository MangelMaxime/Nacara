module Build

open Nacara.Core.Types
open Fable.Core
open Fable.React
open Elmish
open Node

[<NoComparison>]
type Msg =
    | ProcessNextFile
    | ErrorWhileProcessingAFile of exn
    | GenerateNoJekyllFile

[<NoComparison>]
type Model =
    {
        GenerationIsOk : bool
        Config : Config
        Layouts : JS.Map<string, LayoutRenderFunc>
        Pages : PageContext list
        Menus : MenuConfig list
        Partials : Partial list
        ProcessQueue : QueueFile list
        LightnerCache : JS.Map<string, CodeLightner.Config>
    }

[<NoComparison; NoEquality>]
type InitArgs =
    {
        Layouts : LayoutInfo array
        Config : Config
        ProcessQueue : QueueFile list
        Pages : PageContext list
        Menus : MenuConfig list
        Partials : Partial list
        LightnerCache : JS.Map<string, CodeLightner.Config>
    }

let init (args : InitArgs) =
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

    {
        GenerationIsOk = true
        Layouts = layoutCache
        Config = args.Config
        Pages = args.Pages
        Menus = args.Menus
        Partials = args.Partials
        ProcessQueue = args.ProcessQueue
        LightnerCache = args.LightnerCache
    }
    , Cmd.batch [
        Cmd.ofMsg ProcessNextFile
        Cmd.ofMsg GenerateNoJekyllFile
    ]

let update (msg : Msg) (model : Model) =
    match msg with
    | GenerateNoJekyllFile ->
        let action () =
            File.write
                (model.Config.DestinationFolder + "/" + ".nojekyll")
                ""

        model
        , Cmd.OfPromise.attempt action () ErrorWhileProcessingAFile

    | ProcessNextFile ->
        match model.ProcessQueue with
        | file :: tail ->
            let cmd =
                match file with
                | QueueFile.Markdown pageContext ->
                    let args =
                        {
                            PageContext = pageContext
                            Layouts = model.Layouts
                            Partials = model.Partials
                            Menus = model.Menus
                            Config = model.Config
                            Pages = model.Pages
                            LightnerCache = model.LightnerCache
                        } : Write.ProcessMarkdownArgs

                    let action =
                        Write.markdown
                        >> Promise.map (fun _ ->
                            Log.log $"Processed: %s{pageContext.RelativePath}"
                        )
                        >> Promise.catch (fun error ->
                            Log.error $"Error while processing markdown file: %s{pageContext.PageId}"
                            JS.console.error error
                            raise error
                        )

                    Cmd.OfPromise.either action args (fun _ -> ProcessNextFile) ErrorWhileProcessingAFile

                | QueueFile.Sass filePath ->
                    let args =
                        Sass.OutputStyle.Compressed
                        , model.Config.DestinationFolder
                        , model.Config.SourceFolder
                        , filePath

                    let action =
                        Write.sassFile
                        >> Promise.map (fun _ ->
                            Log.log $"Processed: %s{filePath}"
                        )
                        >> Promise.catch (fun error ->
                            Log.error $"Error while processing SASS file: %s{filePath}"
                            JS.console.error error
                            raise error
                        )

                    Cmd.OfPromise.either action args (fun _ -> ProcessNextFile) ErrorWhileProcessingAFile

                | QueueFile.JavaScript filePath
                | QueueFile.Other filePath ->
                    let args =
                        model.Config.DestinationFolder
                        , model.Config.SourceFolder
                        , filePath

                    let action =
                        Write.copyFile
                        >> Promise.map (fun _ ->
                            Log.log $"Copied: %s{filePath}"
                        )
                        >> Promise.catch (fun error ->
                            Log.error $"Error while copying %s{filePath}"
                            JS.console.log error
                        )

                    Cmd.OfPromise.either action args (fun _ -> ProcessNextFile) ErrorWhileProcessingAFile

                | QueueFile.LayoutDependency layoutDependency ->
                    let args =
                        model.Config.DestinationFolder
                        , layoutDependency.Source
                        , layoutDependency.Destination

                    let action args =
                        Write.copyFileWithDestination args
                        |> Promise.catch (fun error ->
                            Log.error $"Error while copying %s{layoutDependency.Source} to %s{layoutDependency.Destination}"
                            JS.console.log error
                            raise error
                        )

                    Cmd.OfPromise.either action args (fun _ -> ProcessNextFile) ErrorWhileProcessingAFile

            { model with
                ProcessQueue = tail
            }
            , cmd

        | [] ->

            // Log result of the build and exist accordingly
            // We exit when all the generation has been done like that, if they are several error
            // the user see all of them at the same time
            if model.GenerationIsOk then
                Log.success "Generation succeeded"
                ``process``.exit(ExitCode.OK)

            else
                Log.error "Generation failed"
                ``process``.exit(ExitCode.COMPLETED_WITH_ERROR)

            model
            , Cmd.none

    | ErrorWhileProcessingAFile _ ->
        { model with
            GenerationIsOk = false
        }
        , Cmd.ofMsg ProcessNextFile
