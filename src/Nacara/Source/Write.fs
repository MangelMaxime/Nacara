module Write

open Types
open System
open Fable.Core.JsInterop
open Fable.React
open Fable.React.Props
open Fulma

let sassFile (model : Model, filePath : string) =
    promise {
        let sassOption =
            jsOptions<Sass.Options>(fun o ->
                o.file <- filePath
                o.outputStyle <- Sass.OutputStyle.Expanded
            )

        let sassResult = Sass.sass.renderSync sassOption

        let outputPath =
            filePath
            |> Directory.moveUp
            |> Directory.join model.Config.Output
            |> Directory.join model.WorkingDirectory
            |> File.changeExtension "css"

        do! File.write outputPath (sassResult.css.toString())
        return filePath
    }

let standard (model : Model, pageContext : PageContext) =
    promise {

        Fable.Core.JS.console.log(pageContext.Path)
        Fable.Core.JS.console.log(pageContext.Path |> Directory.moveUp)
        Fable.Core.JS.console.log(pageContext.Path |> Directory.moveUp |> Directory.join model.Config.Output)

        let outputPath =
            pageContext.Path
            |> Directory.moveUp
            |> Directory.join model.Config.Output
            |> Directory.join model.WorkingDirectory
            |> File.changeExtension "html"

        do! File.write outputPath pageContext.Content
        return pageContext.Path
    }

// let changelog (model : Model, changelog : ChangelogParser.Types.Changelog, path : string) =
//     promise {
//         let outputPath =
//             path.ToLower()
//             |> Directory.join model.Config.Output
//             |> Directory.join model.WorkingDirectory
//             |> File.changeExtension "html"

//         let html =
//             // changelog
//             // |> Templates.Centered.Changelog.toHtml model

//             nothing
//             |> Helpers.parseReactStatic

//         do! File.write outputPath html
//         return path
//     }
