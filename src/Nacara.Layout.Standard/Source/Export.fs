module Layout.Standard.Export

open Fable.Core.JsInterop
open Nacara.Core.Types
open Node
open Feliz
open Feliz.Bulma

exportDefault
    {
        Renderers = [|
            {
                Name = "standard"
                Func = Page.Standard.render
            }
            {
                Name = "navbar-only"
                Func =
                    fun rendererContext pageContext ->
                        promise {
                            let! pageContent =
                                rendererContext.MarkdownToHtml(
                                    pageContext.Content,
                                    pageContext.RelativePath
                                )

                            let content =
                                Html.div [
                                    prop.dangerouslySetInnerHTML pageContent
                                ]

                            return Page.Minimal.render rendererContext pageContext content
                        }
            }
            {
                Name = "api"
                Func =
                    fun rendererContext pageContext ->
                        promise {
                            let! pageContent =
                                rendererContext.MarkdownToHtml(
                                    pageContext.Content,
                                    pageContext.RelativePath
                                )

                            let content =
                                Bulma.container [
                                    Bulma.columns [
                                        Bulma.column [
                                            column.is10Desktop
                                            column.isOffset1Desktop

                                            prop.children [
                                                Bulma.section [
                                                    Bulma.content [
                                                        prop.dangerouslySetInnerHTML pageContent
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]


                            return Page.Minimal.render rendererContext pageContext content
                        }
            }
            {
                Name = "changelog"
                Func = Page.Changelog.render
            }
        |]
        Dependencies = [|
            {
                Source = path.join(Module.__dirname, "./../scripts/menu.js")
                Destination = Dependencies.menu
            }
        |]
    }
