---
title: All plugins
---

The engine reads nothing and renders nothing on its own. Markdown, highlighting, search - you add
each of them as a plugin, so your site carries what it uses and nothing else.

Every plugin is a NuGet package and one line in your pipeline:

```bash frame=terminal
dotnet add package Nacara.Plugin.Markdown --prerelease
```

```fsharp
let site =
    Site.create "My library"
    |> Site.baseUrl "/"
    |> Markdown.register
    |> TextMate.register
    |> Theme.register theme
    |> Site.collection (Theme.docs theme "content")
```

Order does not matter, except that the last plugin to claim something wins. That is how you replace
a piece rather than add to it.

## Content

| Package | What it gives you |
|---|---|
| [`Nacara.Plugin.Markdown`](markdown/index.md) | `.md` pages, front matter, directives, links checked at build time |
| [`Nacara.Plugin.Highlight.TextMate`](highlight/index.md) | Colour in code blocks, from TextMate grammars |
| [`Nacara.Plugin.Highlight.TreeSitter`](highlight/treesitter.md) | The same, from tree-sitter grammars - a parser rather than patterns |
| [`Nacara.Plugin.Literate`](literate/index.md) | `.fs` and `.fsx` files as pages, prose in comments |
| [`Nacara.Plugin.LiveExample`](live-example.md) | F# snippets a reader edits and runs, compiled in the browser |
| [`Nacara.Plugin.Changelog`](changelogs.md) | `CHANGELOG.md` files published as pages |

## Publishing

| Package | What it gives you |
|---|---|
| [`Nacara.Plugin.Search`](search.md) | A search box and modal, indexed by Pagefind |
| [`Nacara.Plugin.Sitemap`](sitemap.md) | `sitemap.xml`, `robots.txt`, canonical links |
| [`Nacara.Plugin.Versions`](versions.md) | Several versions side by side, with a switcher |
| [`Nacara.Plugin.Assets.LightningCss`](assets/lightningcss.md) | Smaller CSS, compiled for the browsers you name |
| [`Nacara.Plugin.Assets.Esbuild`](assets/esbuild.md) | One file out of JavaScript that imports its neighbours, minified |
| [`Nacara.Plugin.Assets.Nuglify`](assets/nuglify.md) | Smaller HTML, JavaScript and CSS |
| [`Nacara.Plugin.FSharpApi`](fsharp-api.md) | Reference pages for an F# library, from the assemblies it ships |

## Checks

| Package | What it gives you |
|---|---|
| [`Nacara.Plugin.LinkValidator`](checks/link-validator.md) | Every link the site published, checked |
| [`Nacara.Plugin.Linter.Rumdl`](checks/rumdl.md) | Every page's markdown, linted by [rumdl](https://github.com/rvben/rumdl) |

## Themes

| Package | What it gives you |
|---|---|
| [`Nacara.Theme.Default`](themes/default/index.md) | The layout this site wears: navbar, sidebar, table of contents, dark mode |

Its own pages cover [customising](themes/default/customising.md) it,
[the navbar](themes/default/navbar.md), [menus](themes/default/menu.md), and the
[components](themes/default/components.md) you can build your own layout from.
[Theming](../guide/theme.md) in the guide explains what a theme is here, whichever one you use.

## Configuring a plugin

Every plugin has `register` for its defaults and `registerWith` for anything else. The options are a
record, so your editor lists them and the compiler checks them:

```fsharp
|> Markdown.registerWith (fun options ->
    { options with
        GithubRepo = Some "MangelMaxime/Nacara"
        StrictLinks = false
    }
)
```

The pages above list every option, its default, and what it changes.

## Writing your own

A plugin is a `Registry -> Registry` function in a package of your own: a few lines for a markdown
extension, more for something that generates pages. [Writing a plugin](authoring.md) walks you
through it.
