---
title: Nacara
description: A documentation engine for F#, where the site is an F# program
layout: bare
---

Nacara turns markdown into a documentation site. It runs on .NET, needs no Node.js, and your
configuration is F# that the compiler checks.

```fsharp title="docs/Site.fs"
let site =
    Site.create "Nacara"
    |> Site.baseUrl "/"
    |> Markdown.register
    |> TextMate.register
    |> Theme.register theme
    |> Site.collection (Theme.docs theme "content")

[<EntryPoint>]
let main argv = Nacara.run site argv
```

## Why another generator

**Your site is a program.** The engine is a library your project references, so the tool can never
be a version behind the site it builds, and everything you configure has a type.

**Front matter is typed.** A collection declares the front matter it expects. A missing field is a
build error with a file, a line and a column - not a blank heading in production.

**Broken links fail the build.** You write links the way they work on GitHub - `../guide/index.md` -
and the engine resolves them against the route table. A link or an anchor that points nowhere stops
the build.

**Everything is a plugin.** Markdown, highlighting, changelogs and search sit on a small core, and
you wire them with the same pipeline you use to describe the site.

**It is fast, and it stays fast.** Transforms are memoized on content hashes, only changed files are
written, and orphaned output is pruned. Watch mode rebuilds and reloads without touching files that
would make the browser reload for nothing.

[Get started](guide/getting-started.md) · [Write a plugin](plugins/authoring.md)
