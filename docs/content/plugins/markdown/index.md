---
title: Markdown
---

Turns `.md` files into pages: front matter, GitHub-flavoured markdown, callouts and tabs, a table of
contents, and links checked while the site builds.

Almost every site starts with this one.

## Add it

```bash frame=terminal
dotnet add package Nacara.Plugin.Markdown --prerelease
```

```fsharp ins="|> Markdown.register"
let site =
    Site.create "My library"
    |> Site.baseUrl "/"
    |> Markdown.register
    |> Theme.register theme
    |> Site.collection (Theme.docs theme "content")
```

Without it the engine has no idea what a `.md` file is, and says so with
`nacara/unknown-front-matter-format`.

## Options

```fsharp
|> Markdown.registerWith (fun options ->
    { options with
        GithubRepo = Some "MangelMaxime/Nacara"
        StrictLinks = false
    }
)
```

| Option | Default | Effect |
|---|---|---|
| `Toc` | `{ From = 2; To = 3 }` | Heading levels in the table of contents, for pages that do not say |
| `StrictLinks` | `true` | A link or anchor pointing nowhere fails the build. `false` warns instead - and `nacara check`, which every CI should run, fails on warnings too |
| `GithubRepo` | `None` | Repository used to expand `#12` and commit references |
| `WarnOnUnknownLanguage` | `true` | Report a fence naming a language no highlighter covers |

## Markdown outside the body

`Markdown.renderInline context text` renders markdown from front matter or data in a layout, with
no paragraph around it. `Markdown.renderBlock context text` keeps the paragraphs.

```fsharp
Html.p [ Markdown.renderInline context context.FrontMatter.Summary ]
```

Links resolve as they do in the body: to a file beside the page, or from the project root with a
leading `/`. A link that names no file is looked up by source path, as `context.LinkTo` does, then
kept as a path in the site. A link that resolves to nothing is written as it is, and the
[link validator](../checks/link-validator.md) reports it.

Directives and code blocks are rendered by Markdig itself, without the plugin's markup.

## What it reports

Whatever it finds, it tells you where and what to do about it:

```text frame="terminal"
! content/guide/deploy.md(12,5): warning markdown/link-target-missing: This link points at an unknown page 'setup.md'
    hint: A link names a file: one beside this page, or one from the project root with a leading '/'.
```

Every diagnostic carries a code, a position your editor can jump to, and a hint.

## Under the hood

[Markdig](https://github.com/xoofx/markdig) does the parsing, with automatic heading identifiers,
pipe and grid tables, task lists, footnotes, auto links, custom containers, generic attributes,
emphasis extras, definition lists and media links enabled.

Everything else on a page comes from elsewhere: the engine owns
[code blocks](../../guide/code-blocks.md), a [highlighter](../highlight/index.md) colours them, and
the [theme](../themes/default/index.md) draws the markup around all of it.

## Reference

Every function and option of it, signature by signature: [`Markdown`](../../reference/nacara-plugin-markdown/nacara-plugins/markdown.md).
