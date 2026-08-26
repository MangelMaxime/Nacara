---
title: Literate F#
---

Publishes `.fs` and `.fsx` files as pages: prose in comments, code as code. The sample on the page
is the file that compiles, so it cannot drift away from what it documents.

## Add it

```bash frame=terminal
dotnet add package Nacara.Plugin.Literate --prerelease
```

```fsharp ins={4}
let site =
    Site.create "My library"
    |> Markdown.register
    |> Literate.register
    |> Site.collection (Theme.docs theme "content")
```

Register the plugin and `.fsx` files become content: a collection reads every file whose format a
plugin claims, so you keep no glob in step.

Keep [Markdown](../markdown/index.md) registered as well. A literate file becomes markdown on its
way through, and that is how it gets directives, highlighting, link checking and a table of contents
without this plugin implementing any of them.

## Writing one

Put your prose in `(** … *)`, and everything else is code. Your front matter goes in the comment the
file opens with, because a file that starts with `---` does not compile:

```fsharp
(**
---
title: Getting started
order: 1
---

Everything here is markdown, including [links](other.md) and directives.
*)

type Person = { Name: string; Age: int }

(**
The type above is on the page as a code block, and in the compiler as a type.
*)
```

Prose comments nest the way F# reads them, so `(** … (* aside *) … *)` is one block, and a `*)`
inside a string will not end it early.

## Commands

A `(*** … ***)` comment steers the block that follows.

| Command | Effect |
|---|---|
| `(*** hide ***)` | The block is compiled but left off the page |
| anything else | Becomes the [fence meta](../../guide/code-blocks.md) of the block: `(*** title="Greeting.fs" {2} ***)` |

So you can title a literate block, mark lines in it, collapse or number it - everything a fence can
do in markdown.

## Type checking

A literate page promises that the code on it works, and every build makes good on that: each source
file goes through `dotnet fsi --typecheck-only`, and what the compiler says lands where it
happened.

```text frame="terminal"
✗ content/guide/api.fsx(70,20): error literate/does-not-compile: FS0001: This expression was expected to have type 'int'
    hint: The page is the file, so what the compiler says about the file is about the page
```

Nothing is run: a page that documents `deleteEverything ()` should not delete everything. Checking
is skipped while you watch, because starting a compiler on every save would be felt and your editor
already tells you. Set `TypeCheck = false` to turn it off entirely.

## Options

```fsharp
|> Literate.registerWith (fun options ->
    { options with
        Extensions = [ ".fsx" ]
        DefaultMeta = "showLineNumbers"
    }
)
```

| Option | Default | Effect |
|---|---|---|
| `Extensions` | `[ ".fsx"; ".fs" ]` | Which files are read as literate source |
| `Language` | `"fsharp"` | Language of the generated fences, and so how they are highlighted |
| `DefaultMeta` | `""` | Fence meta applied to every generated block |
| `TypeCheck` | `true` | Check that the sources compile |
| `TypeCheckWhileWatching` | `false` | Check during watch builds too |

Narrow `Extensions` to `[ ".fsx" ]` when a collection also holds `.fs` files that are not
documentation.

## See it

[The demo](demo.fsx) is itself an `.fsx` file - the page you are reading and the file that compiles
are the same thing.

## Reference

Every function and option of it, signature by signature: [`Literate`](../../reference/nacara-plugin-literate/nacara-plugins/literate.md).
