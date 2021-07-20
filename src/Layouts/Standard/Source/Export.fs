module Layout.Standard.Export

open Fable.Core.JsInterop
open Types
open Fable.React
open Fable.Core

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

                            return Page.Minimal.render {
                                Config = rendererContext.Config
                                Section = pageContext.Section
                                TitleOpt = pageContext.Title
                                Content =
                                    Html.div [
                                        prop.dangerouslySetInnerHTML pageContent
                                    ]
                            }
                        }
            }
            {
                Name = "nacara-changelog"
                Func = Page.Changelog.render
            }
        |]
        Dependencies = [|
            {
                Source = path.join(__dirname, "./../scripts/menu.js")
                Destination = Dependencies.menu
            }
        |]
    }
