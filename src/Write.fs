module Write

open Types
open System
open Fable.Core.JsInterop

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
        let fileContent =
            Render.DocPage.toHtml model pageContext
            |> Helpers.parseReactStatic

        let outputPath =
            pageContext.Path
            |> Directory.moveUp
            |> Directory.join model.Config.Output
            |> Directory.join model.WorkingDirectory
            |> File.changeExtension "html"

        do! File.write outputPath fileContent
        return pageContext.Path
    }

let changelog (model : Model, changelog : Changelog.Types.Changelog, path : string) =
    promise {
        // let versionsList =
        //     changelog.Versions
        //     |> List.map (fun version ->
        //         match version.Version with
        //         | Some versionText ->
        //             fragment [ ]
        //                 [ yield renderVersion versionText version.Date
        //                   for category in version.Categories do
        //                     yield!
        //                         category.Value
        //                         |> List.map (fun body ->
        //                             body.ToHtml(category.Key)
        //                         ) ]

        //         | None -> nothing
        //     )

        let outputPath =
            path.ToLower()
            |> Directory.join model.Config.Output
            |> Directory.join model.WorkingDirectory
            |> File.changeExtension "html"

        let html =
            // Content.content [ ]
            //     [ section [ Class "changelog" ]
            //         [ ul [ Class "changelog-list" ]
            //             versionsList ] ]
            changelog
            |> Render.Changelog.toHtml model
            |> Helpers.parseReactStatic

        do! File.write outputPath html
        return path
    }
