module Write

open Types
open Fable.Core.JsInterop
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
        let outputPath =
            pageContext.Path
            |> Directory.moveUp
            |> Directory.join model.Config.Output
            |> Directory.join model.WorkingDirectory
            |> File.changeExtension "html"

        do! File.write outputPath pageContext.Content
        return pageContext.Path
    }
