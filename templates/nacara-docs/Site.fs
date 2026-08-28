module Docs.Site

open Feliz.ViewEngine
open Nacara.Core
open Nacara.Plugins
open Nacara.Theme

//#if (plugins == "full")
let versions = [ SiteVersion.root "1.0" ]

//#endif
let theme =
    Theme.defaults
    |> Theme.navbar [ NavbarSection("Guide", "guide", "/guide/introduction/") ]
    |> Theme.navbarEnd
        [
            //#if (plugins != "minimal")
            NavbarDynamicWidget Search.trigger
            //#endif
            //#if (plugins == "full")
            NavbarDynamicWidget(Versions.switcher (Versions.versions versions Versions.defaults))
            //#endif
            NavbarIcon("GitHub", "https://github.com/SITE_REPOSITORY", Icons.github)
        ]
    |> Theme.editUrl "https://github.com/SITE_REPOSITORY/edit/main/docs"
    |> Theme.footer (Html.p [ Html.text "Built with Nacara" ])

let site =
    Site.create "SITE_TITLE"
    |> Site.baseUrl "SITE_BASE_URL"
    |> Site.origin "SITE_ORIGIN"
    |> Site.output "output"
    |> Site.staticFiles "static"
    |> Markdown.register
    |> TreeSitter.register
    //#if (plugins == "full")
    |> Literate.register
    //#endif
    //#if (plugins != "minimal")
    |> Search.register
    |> Sitemap.register
    //#endif
    //#if (plugins == "full")
    |> LinkValidator.register
    |> Rumdl.register
    //#endif
    //#if (plugins != "minimal")
    |> LightningCss.register
    |> Esbuild.register
    |> Nuglify.minifyHtml
    //#endif
    //#if (plugins == "full")
    |> Versions.register versions
    //#endif
    //#if (plugins != "minimal")
    |> GitHubPages.register
    //#endif
    |> Theme.register theme
    |> Site.collection (Theme.docs theme "content")

[<EntryPoint>]
let main argv = Nacara.run site argv
