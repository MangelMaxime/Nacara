namespace Nacara.ApiGen.Render

open FSharp.Formatting.ApiDocs
open Markdown
open Giraffe.ViewEngine

module Index =

    let page
        (linkGenerator : string -> string)
        (namespaces : ApiDocNamespace list) =

        let globalNamespace, standardNamespaces  =
            namespaces
            |> List.partition (fun ns ->
                ns.Name = "global"
            )

        let declaredNamespaceTableBody =
            standardNamespaces
            |> List.map (fun standardNamespace ->
                let url = linkGenerator standardNamespace.UrlBaseName

                tr [ ]
                    [
                        td [ ]
                            [
                                a [ _href url ]
                                    [ str standardNamespace.Name ]
                            ]
                        // TODO: Support <namespacedoc> tag as supported by F# formatting
                        // Namespace cannot have documentatin so this is a trick to support it
                        td [ ] [ ]
                    ]
            )

        [
            if not standardNamespaces.IsEmpty then

                InlineHtmlBlock (
                    p [ _class "is-size-5" ]
                        [
                            strong [ ]
                                [ str "Declared namespaces"]
                        ]
                )

                Paragraph [ HardLineBreak ]

                Common.renderDescriptiveTable
                    "Namespace"
                    declaredNamespaceTableBody

                if not globalNamespace.IsEmpty then
                    yield! Namespace.renderDeclaredModules
                                linkGenerator
                                globalNamespace.Head.Entities
        ]
