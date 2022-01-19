namespace Nacara.ApiGen.Render

open FSharp.Formatting.ApiDocs
open Markdown
open Giraffe.ViewEngine
open Html.Extra
open Nacara.ApiGen.CommentFormatter

module Module =

    let renderDeclaredTypes
        (linkGenerator : string -> string)
        (entities : ApiDocEntity list) =

        let typeDeclarations =
            entities
            |> List.filter (fun entity ->
                entity.IsTypeDefinition
            )

        if typeDeclarations.IsEmpty then
            [ ]

        else
            let headerSection =
                InlineHtmlBlock (
                    p [ _class "is-size-5" ]
                        [
                            strong [ ] [ str "Declared types" ]
                        ]
                )

            let declaredTypesTableBody =
                typeDeclarations
                |> List.map (fun typ ->
                    let url = linkGenerator typ.UrlBaseName

                    tr [ ]
                        [
                            td [ ]
                                [
                                    a [ _href url ]
                                        [ str typ.Name ]
                                ]
                            td [ ]
                                [
                                    str (formatXmlComment typ.Comment.XmlText)
                                ]
                        ]
                )

            [
                headerSection

                Paragraph [ HardLineBreak ]

                Common.renderDescriptiveTable
                    "Type"
                    declaredTypesTableBody
            ]

    let content
        (linkGenerator : string -> string)
        (entity : ApiDocEntityInfo) =

        [
            entity.Entity.Comment.TryGetXmlText
            |> Option.map (formatXmlComment >> str)
            |> Html.ofOption
            |> InlineHtmlBlock

            InlineHtmlBlock (
                hr [ ]
            )

            yield! Common.renderValueOrFunction
                        linkGenerator
                        entity.Entity.ValuesAndFuncs

            yield! renderDeclaredTypes
                        linkGenerator
                        entity.Entity.NestedEntities
        ]
