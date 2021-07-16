module Build

open Types
open Fable.Core
open Fable.React
open Elmish
open Node

exception ProcessFileErrorException of pageContext : PageContext * errorMessage : string * original : exn
exception ProcessSassFileErrorException of filePath : string * original : exn
exception ProcessOtherFileErrorException of filePath : string * original : exn
exception ProcessCopyLayoutDepErrorException of layoutDependency : LayoutDependency * original : exn

[<NoComparison>]
type Msg =
    | ProcessNextFile
    | ProcessMarkdownFileSuccess of filePath : string
    | ProcessMarkdownFileError of ProcessFileErrorException
    | ProcessOtherFileSuccess of filePath : string
    | ProcessOtherFileError of ProcessOtherFileErrorException
    | ProcessSassFileSuccess of filePath : string
    | ProcessSassFileError of ProcessSassFileErrorException
    | ProcessCopyLayoutDepSuccess of layoutDependency : LayoutDependency
    | ProcessCopyLayoutDepError of ProcessCopyLayoutDepErrorException

[<RequireQualifiedAccess; NoComparison>]
type QueueFile =
    | Markdown of PageContext
    | Sass of filePath : string
    | JavaScript of filePath : string
    | LayoutDependency of LayoutDependency
    | Other of filePath : string

[<NoComparison>]
type Model =
    {
        GenerationIsOk : bool
        Config : Config
        Layouts : JS.Map<string, LayoutRenderFunc>
        Pages : PageContext list
        Menus : MenuConfig list
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
        LightnerCache : JS.Map<string, CodeLightner.Config>
    }

let init (args : InitArgs) =
    let dependencies =
        args.Layouts
        |> Array.collect (fun info ->
            info.Dependencies
        )

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
        ProcessQueue = args.ProcessQueue
        LightnerCache = args.LightnerCache
    }
    , Cmd.ofMsg ProcessNextFile

let private processMarkdown (pageContext : PageContext, model : Model) =
    promise {
        let layoutRenderer =
            model.Layouts.get(pageContext.Layout)

        if isNull (box layoutRenderer) then
            return raise (ProcessFileErrorException (pageContext, $"Layout renderer '%s{pageContext.Layout}' is unknown", null))
        else
            try
                let categoryMenu =
                    model.Menus
                    |> List.tryFind (fun menuConfig ->
                        menuConfig.Section = pageContext.Section
                    )
                    |> Option.map (fun menuConfig -> menuConfig.Items)

                let rendererContext =
                    {
                        Config = model.Config
                        SectionMenu = categoryMenu
                        Menus = model.Menus |> List.toArray
                        Pages = model.Pages |> List.toArray
                        MarkdownToHtml = markdownToHtml model.LightnerCache
                        MarkdownToHtmlWithPlugins = markdownToHtmlWithPlugins model.LightnerCache
                    }

                let! reactContent =
                    layoutRenderer rendererContext pageContext

                let fileContent = ReactDomServer.renderToStaticMarkup reactContent

                let args =
                    model.Config.DestinationFolder
                    , pageContext.RelativePath
                    , fileContent

                return! Write.markdown args
            with
                | error ->
                    return raise (ProcessFileErrorException (pageContext, error.Message, error))
    }

let update (msg : Msg) (model : Model) =
    match msg with
    | ProcessNextFile ->
        match model.ProcessQueue with
        | file :: tail ->
            let cmd =
                match file with
                | QueueFile.Markdown pageContext ->
                    Cmd.OfPromise.either processMarkdown (pageContext, model) ProcessMarkdownFileSuccess ProcessMarkdownFileError

                | QueueFile.Sass filePath ->
                    if filePath.Replace("\\", "/").StartsWith("scss/")
                        || filePath.Replace("\\", "/").StartsWith("sass/") then

                        if model.Config.IsVerbose then
                            Log.warn $"File %s{filePath} ignored has it is under a scss or sass folder at the root of the source"

                        Cmd.ofMsg ProcessNextFile

                    else

                        let args =
                            Sass.OutputStyle.Compressed
                            , model.Config.DestinationFolder
                            , model.Config.SourceFolder
                            , filePath

                        let action =
                            Write.sassFile
                            >> Promise.catch (fun error ->
                                raise (ProcessSassFileErrorException (filePath, error))
                            )

                        Cmd.OfPromise.either
                            action
                            args
                            ProcessSassFileSuccess
                            ProcessSassFileError

                | QueueFile.JavaScript filePath
                | QueueFile.Other filePath ->
                    let args =
                        model.Config.DestinationFolder
                        , model.Config.SourceFolder
                        , filePath

                    let action =
                        Write.copyFile
                        >> Promise.catch (fun error ->
                            raise (ProcessOtherFileErrorException (filePath, error))
                        )

                    Cmd.OfPromise.either
                        action
                        args
                        ProcessOtherFileSuccess
                        ProcessOtherFileError

                | QueueFile.LayoutDependency layoutDependency ->
                    let args =
                        model.Config.DestinationFolder
                        , layoutDependency

                    let action =
                        Write.copyLayoutDependency
                        >> Promise.catch (fun error ->
                            raise (ProcessCopyLayoutDepErrorException (layoutDependency, error))
                        )

                    Cmd.OfPromise.either
                        action
                        args
                        ProcessCopyLayoutDepSuccess
                        ProcessCopyLayoutDepError


            { model with
                ProcessQueue = tail
            }
            , cmd

        | [] ->

            // Log result of the build and exist accordingly
            // We exit when all the generation has been done like that, if they are several error
            // the user see all of them at the same time
            if model.GenerationIsOk then
                Log.success "Generation done with success"
                ``process``.exit(ExitCode.OK)

            else
                Log.error "Generation done with error"
                ``process``.exit(ExitCode.COMPLETED_WITH_ERROR)

            model
            , Cmd.none

    | ProcessMarkdownFileSuccess filePath ->
        Log.log $"Processed: %s{filePath}"

        model
        , Cmd.ofMsg ProcessNextFile

    | ProcessMarkdownFileError error ->
        Log.error $"Error while processing markdown file: %s{error.pageContext.PageId}\n%s{error.errorMessage}"
        Log.error $"%A{error.original}"

        { model with
            GenerationIsOk = false
        }
        , Cmd.none

    | ProcessSassFileSuccess filePath ->
        Log.log $"Processed: %s{filePath}"

        model
        , Cmd.ofMsg ProcessNextFile

    | ProcessSassFileError error ->
        Log.error $"Error while processing SASS file: %s{error.filePath}\nOriginal error:\n%s{error.original.Message}"

        { model with
            GenerationIsOk = false
        }
        , Cmd.none

    | ProcessOtherFileSuccess filePath ->
        Log.log $"Processed: %s{filePath}"

        model
        , Cmd.ofMsg ProcessNextFile

    | ProcessOtherFileError error ->
        Log.error $"Error while copying file: %s{error.filePath}\nOriginal error:\n%s{error.original.Message}"

        { model with
            GenerationIsOk = false
        }
        , Cmd.ofMsg ProcessNextFile

    | ProcessCopyLayoutDepSuccess layoutDependency ->
        Log.log $"Layout dependency %s{layoutDependency.Source} has been copied to %s{layoutDependency.Destination}"

        model,
        Cmd.ofMsg ProcessNextFile

    | ProcessCopyLayoutDepError error ->
        Log.error $"Error while copying %s{error.layoutDependency.Source}"

        { model with
            GenerationIsOk = false
        }
        , Cmd.ofMsg ProcessNextFile

