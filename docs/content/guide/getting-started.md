---
title: Getting started
---

A Nacara site is an F# console project that references the engine and calls it.

## Create the project

The template offers different sets of plugins:

```bash frame="terminal"
dotnet new install Nacara.Templates
dotnet new nacara-docs -o docs
# or use a preset
dotnet new nacara-docs -o docs --plugins full
```

| `--plugins` | What you get |
|---|---|
| `minimal` | Markdown, highlighting and the theme |
| `standard` | Plus search, a sitemap, minified assets and publishing to GitHub Pages |
| `full` | Plus literate F#, versions, link checking and a markdown linter |

### By hand

A site is a console project, so you can also start from one:

```bash frame="terminal"
mkdir docs && cd docs
dotnet new console -lang F# -o .
dotnet add package Nacara.Core --prerelease
dotnet add package Nacara.Plugin.Markdown --prerelease
dotnet add package Nacara.Theme.Default --prerelease
```

Those three are the least a site needs. Every other feature is a package of its own -
[the plugins](../plugins/overview.md) lists them.

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
    |> Theme.register theme
    |> Site.collection (Theme.docs theme "content")

[<EntryPoint>]
let main argv = Nacara.run site argv
```

Each plugin is a line, and order does not matter, except that the last plugin to claim something
wins. Some need you to say something first:

| | |
|---|---|
| [Link checking](../plugins/checks/link-validator.md) | on for a deploy, slow enough that you may not want it on every build |
| [API reference](../plugins/fsharp-api.md) | which assemblies to read |
| [Changelogs](../plugins/changelogs.md) | which changelog files to publish |
| [Versions](../plugins/versions.md) | which versions you have deployed |

### Highlighting

There are two, and a site can have both:

| | |
|---|---|
| [TextMate](../plugins/highlight/textmate.md) | about fifty languages, nothing to fetch |
| [tree-sitter](../plugins/highlight/treesitter.md) | twelve languages done properly, fetched once per machine |

```fsharp
|> TextMate.register
|> TreeSitter.register
```

The last one registered is asked first, so tree-sitter takes F#, JSON and its others, and TextMate
covers the rest. Register tree-sitter alone to have an unknown language reported instead of
covered.

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
