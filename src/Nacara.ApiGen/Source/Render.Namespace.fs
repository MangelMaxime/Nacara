namespace Nacara.ApiGen.Render

open FSharp.Formatting.ApiDocs
open Markdown
open Giraffe.ViewEngine
open Nacara.ApiGen.CommentFormatter

module Namespace =

    let renderDeclaredModules
        (linkGenerator : string -> string)
        (entities : ApiDocEntity list) =

        let moduleDeclarations =
            entities
            |> List.filter (fun ns ->
                ns.Symbol.IsFSharpModule
            )

        if moduleDeclarations.IsEmpty then
            [ ]

        else
            let header =
                InlineHtmlBlock (
                    p [ _class "is-size-5" ]
                        [
                            strong [ ]
                                [ str "Declared modules" ]
                        ]
                )

            let moduleTableBody =
                moduleDeclarations
                |> List.map (fun moduleDeclaration ->
                    let url = linkGenerator moduleDeclaration.UrlBaseName

                    tr [ ]
                        [
                            td [ ]
                                [
                                    a [ _href url ]
                                        [ str moduleDeclaration.Name ]
                                ]
                            td [ ]
                                [
                                    str (formatXmlComment moduleDeclaration.Comment.XmlText)
                                ]
                        ]
                )

            [
                header

                Paragraph [ HardLineBreak ]

                Common.renderDescriptiveTable
                    "Module"
                    moduleTableBody
            ]

    let page
        (linkGenerator : string -> string)
        (apiDoc : ApiDocNamespace) =

        [
            InlineHtmlBlock (
                h2
                    [ _class "title is-3" ]
                    [ str apiDoc.Name ]
            )

            yield! renderDeclaredModules
                    linkGenerator
                    apiDoc.Entities
        ]
