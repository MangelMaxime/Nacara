module Layout.Standard.Export

open Fable.Core.JsInterop
open Nacara.Core.Types
open Node
open Feliz



exportDefault
    {
        Renderers = [|
            {
                Name = "nacara-standard"
                Func = Page.Standard.render
            }
            {
                Name = "nacara-navbar-only"
                Func =
                    fun rendererContext pageContext ->
                        promise {
                            let! pageContent =
                                rendererContext.MarkdownToHtml pageContext.Content

                            let content =
                                Html.div [
                                    prop.dangerouslySetInnerHTML pageContent
                                ]

                            return Page.Minimal.render rendererContext pageContext content
                        }
            }
            {
                Name = "nacara-changelog"
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
