module Write

open Types
open Fable.Core.JsInterop

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

let copyLayoutDependency (destinationFolder : string, layoutDependency : LayoutDependency) =
    promise {
        let destination =
            layoutDependency.Destination
            |> Directory.join destinationFolder

        do! File.copy layoutDependency.Source destination
        return layoutDependency
    }

let markdown (destinationFolder : string, relativeFilePath : string, content : string) =
    promise {
        let destination =
            relativeFilePath
            |> Directory.join destinationFolder
            |> File.changeExtension "html"

        do! File.write destination content
        return relativeFilePath
    }
