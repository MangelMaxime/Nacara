module Docs.Site

open Feliz.ViewEngine
open System
open System.IO
open Nacara.Core
open Nacara.Plugins
open Nacara.Theme

/// Versions of the documentation, as deployed side by side. One for now - an entry here is a
/// promise that `<deployment root>/<its directory>/` exists, and the switcher links straight to it.
let versions = [ SiteVersion.root "v2" ]

/// Nacara's own API, read from the assemblies this site is built with.
let apiOptions =
    { FSharpApi.defaults with
        Root = "reference"
        Title = "API reference"
        Exclude = [ "Nacara.Plugins.Internal" ]
        WarnOnUndocumented = true
        Sources =
            [
                let beside =
                    Reflection.Assembly.GetExecutingAssembly().Location |> Path.GetDirectoryName

                for name in
                    [
                        "Nacara.Core"
                        "Nacara.Plugin.Markdown"
                        "Nacara.Plugin.Highlight.TextMate"
                        "Nacara.Plugin.Highlight.TreeSitter"
                        "Nacara.Plugin.Literate"
                        "Nacara.Plugin.Changelog"
                        "Nacara.Plugin.Search"
                        "Nacara.Plugin.Sitemap"
                        "Nacara.Plugin.Versions"
                        "Nacara.Plugin.Deploy.GitHubPages"
                        "Nacara.Plugin.Assets.LightningCss"
                        "Nacara.Plugin.Assets.Esbuild"
                        "Nacara.Plugin.Assets.Nuglify"
                        "Nacara.Plugin.LinkValidator"
                        "Nacara.Plugin.Linter.Rumdl"
                        "Nacara.Plugin.LiveExample"
                        "Nacara.Plugin.FSharpApi"
                        "Nacara.Theme.Default"
                    ] -> FSharpApiSource.create (Path.Combine(beside, $"%s{name}.dll"))
            ]
    }

let theme =
    Theme.defaults
    |> Theme.navbar
        [
            NavbarSection("Guide", "guide", "/Nacara/guide/getting-started/")
            NavbarSection("Plugins", "plugins", "/Nacara/plugins/overview/")
            NavbarSection("Reference", "reference", "/Nacara/reference/")
            NavbarSection("Changelog", "changelog", "/Nacara/changelog/nacara-core/")
        ]
    |> Theme.navbarEnd
        [
            NavbarDynamicWidget Search.trigger
            // NavbarDynamicWidget(Versions.switcher (Versions.versions versions Versions.defaults))
            NavbarLocalePicker
            NavbarIcon("GitHub", "https://github.com/MangelMaxime/Nacara", Icons.github)
            NavbarDivider
        ]
    |> Theme.menu
        "guide"
        [
            Menu.section
                "Getting started"
                [
                    Menu.page "guide/getting-started.md"
                    Menu.page "guide/project-layout.md"
                ]
            Menu.section
                "Writing"
                [
                    Menu.page "guide/content.md"
                    Menu.page "guide/code-blocks.md"
                ]
            Menu.section
                "Publishing"
                [
                    Menu.page "guide/theme.md"
                    Menu.page "guide/command-line.md"
                    Menu.page "guide/i18n.md"
                    Menu.page "guide/deploy.md"
                ]
        ]
    |> Theme.menu
        "plugins"
        [
            Menu.section "Overview" [ Menu.page "plugins/overview.md" ]
            Menu.section
                "Content"
                [
                    Menu.group
                        "plugins/markdown/index.md"
                        [
                            Menu.page "plugins/markdown/syntax.md"
                            Menu.page "plugins/markdown/directives.md"
                            Menu.page "plugins/markdown/links.md"
                        ]
                    Menu.group
                        "plugins/highlight/index.md"
                        [
                            Menu.page "plugins/highlight/textmate.md"
                            Menu.page "plugins/highlight/treesitter.md"
                        ]
                    Menu.group "plugins/literate/index.md" [ Menu.page "plugins/literate/demo.fsx" ]
                    Menu.page "plugins/live-example.md"
                    Menu.page "plugins/changelogs.md"
                ]
            Menu.section
                "Publishing"
                [
                    Menu.page "plugins/search.md"
                    Menu.page "plugins/sitemap.md"
                    Menu.page "plugins/versions.md"
                    Menu.page "plugins/github-pages.md"
                    Menu.section
                        "Assets"
                        [
                            Menu.page "plugins/assets/lightningcss.md"
                            Menu.page "plugins/assets/esbuild.md"
                            Menu.page "plugins/assets/nuglify.md"
                        ]
                    Menu.page "plugins/fsharp-api.md"
                    Menu.section
                        "Checks"
                        [
                            Menu.page "plugins/checks/link-validator.md"
                            Menu.page "plugins/checks/rumdl.md"
                        ]
                ]
            Menu.section
                "Themes"
                [
                    Menu.group
                        "plugins/themes/default/index.md"
                        [
                            Menu.page "plugins/themes/default/navbar.md"
                            Menu.page "plugins/themes/default/menu.md"
                            Menu.page "plugins/themes/default/front-matter.md"
                            Menu.page "plugins/themes/default/customising.md"
                            Menu.page "plugins/themes/default/components.md"
                        ]
                ]
            Menu.section "Writing plugins" [ Menu.page "plugins/authoring.md" ]
        ]
    |> Theme.editUrl "https://github.com/MangelMaxime/Nacara/edit/main/docs"
    |> Theme.footer (
        Html.p
            [
                Html.text "Nacara is built with F# · "
                Html.a
                    [
                        prop.href "https://github.com/MangelMaxime/Nacara"
                        prop.text "Source"
                    ]
            ]
    )
    |> Theme.css """[data-section="reference"] { --nacara-sidebar-width: 20rem; }"""

let content =
    Theme.docs theme "content"
    // Every static host looks for 404.html at the root, and will not use 404/index.html.
    |> Collection.route (fun page ->
        if RelativePath.value page.RelativePath = "404.md" then
            Route.file page.Locale "404.html"
        else
            Collection.defaultRoute page
    )

let reference =
    FSharpApi.collection "reference" DocFrontMatter.decoder apiOptions
    |> Collection.title _.Title
    |> Collection.layout (Theme.layout theme)

let changelogs =
    [
        ChangelogSource.create "Nacara.Core" "../src/Nacara.Core/CHANGELOG.md"
        |> ChangelogSource.group "Engine"

        ChangelogSource.matching "../src/Nacara.Plugin.*/CHANGELOG.md"
        |> ChangelogSource.group "Plugins"

        ChangelogSource.matching "../src/Nacara.Theme.*/CHANGELOG.md"
        |> ChangelogSource.group "Themes"

        ChangelogSource.create "Nacara.Templates" "../templates/CHANGELOG.md"
        |> ChangelogSource.group "Templates"
    ]

let changelog =
    Changelog.collection "changelog" DocFrontMatter.decoder changelogs
    |> Collection.title _.Title
    |> Collection.routePrefix "changelog"
    |> Collection.layout (Theme.layout theme)

let site =
    Site.create "Nacara"
    |> Site.description "A documentation engine for F#, where the site is an F# program"
    |> Site.baseUrl "/Nacara/"
    |> Site.origin "https://mangelmaxime.github.io"
    |> Site.staticFiles "static"
    |> Site.stylesheet "assets/landing.css"
    |> Markdown.register
    |> TreeSitter.register
    |> Literate.register
    |> Changelog.registerWith "changelog" changelogs
    |> Search.register
    |> Sitemap.register
    |> FSharpApi.register apiOptions
    |> LinkValidator.registerWith (
        LinkValidator.checkExternal (
            System.Environment.GetEnvironmentVariable "NACARA_CHECK_LINKS" = "1"
        )
    )
    |> LiveExample.registerWith (
        LiveExample.preset (
            LiveExamplePreset.create "demo"
            |> LiveExamplePreset.files [ "preludes/Demo.fs" ]
            |> LiveExamplePreset.project "snippets/Snippets.fsproj"
            |> LiveExamplePreset.css "snippets/snippet.css"
            |> LiveExamplePreset.template "snippets/snippet.html"
            |> LiveExamplePreset.asDefault
        )
        >> LiveExample.stats true
        >> LiveExample.highlighting LiveExampleHighlighting.TreeSitterHighlighting
    )
    |> Rumdl.register
    |> LightningCss.register
    |> Nuglify.minifyHtml
    |> Esbuild.register
    |> Versions.register versions
    |> GitHubPages.register
    |> Theme.register theme
    |> Site.collection content
    |> Site.collection changelog
    |> Site.collection reference

[<EntryPoint>]
let main argv = Nacara.run site argv
