module Write

open Nacara.Core.Types
open Fable.Core.JsInterop
open Fable.Core
open Fable.React
open Node

// While waiting for React 18
// Adding .js to the import fix the ESM import issue
// https://github.com/facebook/react/issues/20235#issuecomment-861836181
[<Import("default", "react-dom/server")>]
let ReactDomServer: IReactDomServer = jsNative

let sassFile (outputStyle : Sass.OutputStyle, destinationFolder : string, sourceFolder : string, relativeFilePath : string) =
    promise {

        let source =
            relativeFilePath
            |> Directory.join sourceFolder

        let sassOption =
            jsOptions<Sass.Options>(fun o ->
                o.file <- source
                o.outputStyle <- outputStyle
            )

        let sassResult = Sass.sass.renderSync sassOption

        let destination =
            relativeFilePath
            |> Directory.join destinationFolder
            |> File.changeExtension "css"

        do! File.write destination (sassResult.css.toString())
        return source
    }

let copyFile (destinationFolder : string, sourceFolder : string, relativeFilePath : string) =
    promise {
        let destination =
            relativeFilePath
            |> Directory.join destinationFolder

        let source =
            relativeFilePath
            |> Directory.join sourceFolder

        do! File.copy source destination
        return source
    }

let copyFileWithDestination (destinationFolder : string, sourceFileName : string, destinationFileName : string) =
    promise {
        let destination =
            destinationFileName
            |> Directory.join destinationFolder

        do! File.copy sourceFileName destination
        return (sourceFileName, destination)
    }

[<NoComparison>]
type ProcessMarkdownArgs =
    {
        PageContext : PageContext
        Layouts : JS.Map<string, LayoutRenderFunc>
        Partials : Partial list
        Menus : MenuConfig list
        Config : Config
        Pages : PageContext list
        RemarkPlugins : RemarkPlugin array
        RehypePlugins : RehypePlugin array
    }

exception ProcessFileErrorException of pageContext : PageContext * errorMessage : string

let markdown (args : ProcessMarkdownArgs) =
    promise {
        let layoutRenderer =
            args.Layouts.get(args.PageContext.Layout)

        if isNull (box layoutRenderer) then
            return raise (ProcessFileErrorException (args.PageContext, $"Layout renderer '%s{args.PageContext.Layout}' is unknown"))
        else
            try
                let categoryMenu =
                    args.Menus
                    |> List.tryFind (fun menuConfig ->
                        menuConfig.Section = args.PageContext.Section
                    )
                    |> Option.map (fun menuConfig -> menuConfig.Items)

                let rendererContext =
                    {
                        Config = args.Config
                        SectionMenu = categoryMenu
                        Partials = args.Partials |> List.toArray
                        Menus = args.Menus |> List.toArray
                        Pages = args.Pages |> List.toArray
                        // MarkdownToHtml = markdownToHtml args.RemarkPlugins args.RehypePlugins
                    }

                let! reactContent =
                    layoutRenderer rendererContext args.PageContext

                let fileContent = ReactDomServer.renderToStaticMarkup reactContent

                let destination =
                    args.PageContext.RelativePath
                    |> Directory.join args.Config.DestinationFolder
                    |> File.changeExtension "html"

                do! File.write destination fileContent

                return args.PageContext
            with
                | error ->
                    return raise (ProcessFileErrorException (args.PageContext, error.Message))
    }
