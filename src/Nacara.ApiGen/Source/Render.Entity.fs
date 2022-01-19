namespace Nacara.ApiGen.Render

open FSharp.Formatting.ApiDocs
open Markdown
open Giraffe.ViewEngine
open Html.Extra

module Entity =

    let namespaceAndParentSection
        (linkGenerator : string -> string)
        (entity : ApiDocEntityInfo) =

        let parentInfoOpt =
            entity.ParentModule
            |> Option.map (fun parentModule ->
                let parentModuleUrl =
                    linkGenerator parentModule.UrlBaseName

                div [ ]
                    [
                        strong [ ]
                            [ str "Parent:"]
                        str " "
                        a [ _href parentModuleUrl ]
                            [ str parentModule.Symbol.FullName ]
                    ]
            )

        InlineHtmlBlock (
            p [ ]
                [
                    div [ ]
                        [
                            strong [ ]
                                [ str "Namespace:" ]
                            str " "
                            a [ _href (linkGenerator entity.Namespace.UrlBaseName) ]
                                [ str entity.Namespace.Name ]
                        ]

                    Html.ofOption parentInfoOpt
                ]
        )


    let page
        (linkGenerator : string -> string)
        (entity : ApiDocEntityInfo) =

        [
            InlineHtmlBlock (
                h2
                    [ _class "title is-3" ]
                    [ str entity.Entity.Name ]
            )

            namespaceAndParentSection
                linkGenerator
                entity

            if entity.Entity.Symbol.IsFSharpModule then
                yield! Module.content linkGenerator entity
            else if entity.Entity.Symbol.IsFSharpRecord then
                yield! Record.content linkGenerator entity.Entity
        ]
