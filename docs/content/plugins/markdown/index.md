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

## What it reports

Whatever it finds, it tells you where and what to do about it:

```text frame="terminal"
! content/guide/deploy.md(12,5): warning markdown/link-target-missing: This link points at an unknown page 'setup.md'
    hint: A link names a file: one beside this page, or one from the project root with a leading '/'.
```

Every diagnostic carries a code, a position your editor can jump to, and a hint. We list none of
them here, because the message itself is the documentation and a list would go stale.

## Under the hood

[Markdig](https://github.com/xoofx/markdig) does the parsing, with automatic heading identifiers,
pipe and grid tables, task lists, footnotes, auto links, custom containers, generic attributes,
emphasis extras, definition lists and media links enabled.

Everything else on a page comes from elsewhere: the engine owns
[code blocks](../../guide/code-blocks.md), a [highlighter](../highlight/index.md) colours them, and
the [theme](../themes/default/index.md) draws the markup around all of it.

## Reference

Every function and option of it, signature by signature: [`Markdown`](../../reference/nacara-plugin-markdown/nacara-plugins/markdown.md).
