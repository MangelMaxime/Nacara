---
title: Getting started
---

A Nacara site is an F# console project that references the engine and calls it.

## Create the project

```bash frame="terminal"
mkdir docs && cd docs
dotnet new console -lang F# -o .
dotnet add package Nacara.Core --prerelease && \
dotnet add package Nacara.Plugin.Markdown --prerelease && \
dotnet add package Nacara.Plugin.Highlight.TextMate --prerelease && \
dotnet add package Nacara.Plugin.Search --prerelease && \
dotnet add package Nacara.Plugin.Sitemap --prerelease && \
dotnet add package Nacara.Plugin.Assets.LightningCss --prerelease && \
dotnet add package Nacara.Plugin.Assets.Nuglify --prerelease && \
dotnet add package Nacara.Theme.Default --prerelease
```

That is one package per thing it does. Take out any line you do not want, now or later.

Or start from the template, which sets up the same project with a first page and a menu in it. The
templates come as their own package, so install them once:

```bash frame="terminal"
dotnet new install Nacara.Templates
dotnet new nacara-docs -o docs
```

## Describe the site

Replace `Program.fs` with a description of your site. Everything here is checked by the compiler:

```fsharp title="Program.fs"
module Docs.Site

open Nacara.Core
open Nacara.Plugins
open Nacara.Theme

let theme =
    Theme.defaults
    |> Theme.navbar [ NavbarSection("Guide", "guide", "/guide/introduction/") ]

let site =
    Site.create "My library"
    |> Site.baseUrl "/"
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
```

Those eight are markdown, syntax highlighting, search, a sitemap, and smaller HTML, JavaScript and
CSS on the way out. Order does not matter, except that the last plugin to claim something wins.

The rest are a line of your own, mostly because they need you to say something first:

| | |
|---|---|
| [Link checking](../plugins/checks/link-validator.md) | on for a deploy, slow enough that you may not want it on every build |
| [API reference](../plugins/fsharp-api.md) | which assemblies to read |
| [Changelogs](../plugins/changelogs.md) | which changelog files to publish |
| [Versions](../plugins/versions.md) | which versions you have deployed |

### Sharper highlighting

[TextMate](../plugins/highlight/textmate.md) above knows about fifty languages. Add
[tree-sitter](../plugins/highlight/treesitter.md) after it when you want the twelve languages it
ships done properly:

```fsharp
|> TextMate.register
|> TreeSitter.register
```

The last highlighter registered is asked first, so tree-sitter takes F#, JSON and its others, and
TextMate still covers everything else.

If you would rather *know* when tree-sitter cannot colour something, drop the `TextMate.register`
line so nothing quietly covers for it.

## Write a page

Put your content in the directory the collection points at - `content` above. Every page starts
with front matter:

```markdown title="content/guide/introduction.md"
---
title: Introduction
order: 1
---

## Installation

Run `dotnet add package My.Library`.
```

## Build it

```bash frame="terminal"
dotnet run -- watch
```

The site is served on <http://localhost:8080>, rebuilt when you save, and reloaded in the browser.
When you are happy:

```bash frame="terminal"
dotnet run -- build
```

:::tip What next
[Project layout](project-layout.md) explains what belongs where, and
[Content and collections](content.md) covers front matter, links and directives.
:::
