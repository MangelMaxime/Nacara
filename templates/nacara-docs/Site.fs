module Docs.Site

open Feliz.ViewEngine
open Nacara.Core
open Nacara.Plugins
open Nacara.Theme

let theme =
    Theme.defaults
    |> Theme.navbar [ NavbarSection("Guide", "guide", "/guide/introduction/") ]
    |> Theme.navbarEnd
        [
            NavbarDynamicWidget Search.trigger
            NavbarIcon("GitHub", "https://github.com/SITE_REPOSITORY", Icons.github)
        ]
    |> Theme.editUrl "https://github.com/SITE_REPOSITORY/edit/main/docs"
    |> Theme.footer (Html.p [ Html.text "Built with Nacara" ])

let site =
    Site.create "SITE_TITLE"
    |> Site.baseUrl "SITE_BASE_URL"
    |> Site.output "output"
    |> Site.staticFiles "static"
    |> Markdown.register
    |> TextMate.register
    |> Search.register
    |> Sitemap.register
    |> LightningCss.register
    |> Nuglify.minifyHtml
    |> Nuglify.minifyJs
    |> Theme.register theme
    |> Site.collection (Theme.docs theme "content")

[<EntryPoint>]
let main argv = Nacara.run site argv
