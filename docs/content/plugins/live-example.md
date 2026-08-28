---
title: Live examples
---

Make F# snippet interactive so a reader can run and change, compiled by [Fable](https://fable.io/)
in their own browser.

## Add it

```bash frame=terminal
dotnet add package Nacara.Plugin.LiveExample --prerelease
```

```fsharp ins={3}
Site.create "My library"
|> Markdown.register
|> LiveExample.register
```

Then mark a code block `live`:

:::preview

````markdown "live"
```fsharp live
printfn "hello"
```
````

:::

## Try it

Press Run.

```fsharp live
open Demo

printfn "Hello from F#, compiled in your browser."

[ 1..15 ] |> List.map fizzbuzz |> String.concat " " |> printfn "%s"

printfn "distance = %.3f" (distance { X = 0.0; Y = 0.0 } { X = 3.0; Y = 4.0 })
```

`fizzbuzz` and `distance` come from this site's preset rather than from the snippet, you can hover
over them to get their siganture.

A live block takes the same annotations as any other, so you can fold the setup away:

```fsharp live collapse={1-5}
open Demo

let corners =
    [ { X = 0.0; Y = 0.0 }; { X = 3.0; Y = 4.0 }; { X = 6.0; Y = 8.0 } ]

let travelled =
    corners
    |> List.pairwise
    |> List.sumBy (fun (a, b) -> distance a b)

printfn "travelled %.1f" travelled
```

## What a reader gets

Nothing, until they ask for it. A live block is an ordinary code block with a Run button.

Run fetches the compiler once for the page, turns the block into an editor, and puts the result
underneath in three tabs - **Result** for what the snippet drew, **Console** for what it printed,
**JavaScript** for what Fable made of it. **Reset** puts your example back.

The editor is a real one: errors are marked on the range that caused them as the reader types,
hovering gives a type, and Tab completes.

**Ctrl+Enter** - **⌘+Enter** on a Mac - compiles what they have written without reaching for the
button.

**Expand** make the editor fullscreen allowing to edit comfortably longer snippet. Escape puts it
back, and the page returns to where they left it.

Snippets run in a frame of their own, so one that throws, loops forever or rewrites the whole
document breaks only itself.

A [diff](../guide/code-blocks.md#writing-a-diff) runs as the code it leaves behind - the lines you
marked deleted are shown but never compiled, so a block can show a change and still run.

## Other languages

The editor is powered by [Fable](https://fable.io/) allowing you to also showcase output for Fable
supported targets.

:::preview

````markdown
```fsharp live target=python
let greet name =
    printfn "hello %s" name
```
````

:::

Supported targets are:

- `javascript`, `js`
- `typescript`, `ts`
- `python`, `py`
- `rust`, `rs`
- `dart`
- `php`
- `erlang`, `beam`

Only JavaScript can runs the generated code. Everything else compiles and shows you the code,
the Result tab is not drawn, and the output tab is named after the language:

Set a target for the whole site when most of your snippets share one:

```fsharp
|> LiveExample.registerWith (LiveExample.target Python)
```

### Colouring the output

Every target is coloured, with a grammar the plugin ships. Nothing to configure, and a grammar is
fetched only when a reader first opens the tab that needs it.

Name one yourself to override what ships, or to colour a language Fable does not target:

```fsharp
|> LiveExample.registerWith (
    LiveExample.outputGrammar (
        TreeSitter.fromGitHub
            "gleam"
            "https://github.com/gleam-lang/tree-sitter-gleam"
            "cefbd6863983b4df3214b7934bde5e9ca63d5b7f"
    )
)
```

That is the same declaration the [highlighting plugin](highlight/treesitter.md) takes, built once
into the same cache, so a site already colouring Gleam blocks hands over the grammar it has.

## Presets

A preset is what a snippet gets on top of F# and the browser: code to `open`, a library to call, a
stylesheet, a page to draw into. Define one, and a fence asks for it by name.

```fsharp
LiveExample.registerWith (
    LiveExample.preset (
        LiveExamplePreset.create "core"
        |> LiveExamplePreset.files [ "docs/preludes/Core.fs" ]
        |> LiveExamplePreset.project "docs/snippets/Snippets.fsproj"
        |> LiveExamplePreset.css "docs/snippets/snippet.css"
        |> LiveExamplePreset.template "docs/snippets/snippet.html"
        |> LiveExamplePreset.asDefault
    )
)
```

````markdown
```fsharp live preset=core
// everything the preset opens or defines is in scope
```
````

Every part is optional. Define as many presets as you have kinds of snippet.

### Files

```fsharp
|> LiveExamplePreset.files [ "docs/preludes/Core.fs" ]
```

F# files, in compilation order, relative to the project root. They are compiled with every snippet,
so hovering a function from one gives its real signature rather than a guess.

Keep them small - they are type-checked on every compile, so a preset is the `open` lines and a
helper or two.

### A project

```fsharp
|> LiveExamplePreset.project "docs/snippets/Snippets.fsproj"
```

An `.fsproj`, with everything it references and everything it declares.
Point it at your own library's project and snippets get it as it is in your working tree.

One project serves the whole site, so one preset names it, and naming two fails the build.

### A stylesheet

```fsharp
|> LiveExamplePreset.css "docs/snippets/snippet.css"
```

Applies inside the frame a snippet runs in, and nowhere else - the page around it is untouched.
Without one, the frame has the browser's defaults.

### A template

```fsharp
|> LiveExamplePreset.template "docs/snippets/snippet.html"
```

A full HTML document for the snippet to run inside. Use it to give the snippet something to draw
into:

```html title="docs/snippets/snippet.html"
<!doctype html>
<html>
  <body>
    <div id="app"></div>
  </body>
</html>
```

The snippet is put into the body, so whatever the template lays out is there before it runs.

### The same for every preset

A stylesheet and a template can be set once for the site instead, and a preset that names none of
its own uses them:

```fsharp
|> LiveExample.defaultCss "docs/snippets/snippet.css"
|> LiveExample.defaultTemplate "docs/snippets/snippet.html"
```

### The default preset

```fsharp
|> LiveExamplePreset.asDefault
```

Marks the preset a fence gets when it names none, which is what you want when you document a single
library. A fence can still ask for another by name. Mark two and the build stops.

## Options

| Option | Default | Effect |
|---|---|---|
| `Presets` | none | What a snippet gets on top of F#: code, a library, a stylesheet, a page |
| `Css` | `None` | The stylesheet a preset that names none of its own uses |
| `Template` | `None` | The page a preset that names none of its own uses |
| `Tab` | `None` | Which tab a snippet opens on |
| `Target` | JavaScript | What a snippet is compiled to |
| `OutputGrammars` | none | Grammars for colouring what a target produced |
| `Highlighting` | editor | How an edited snippet is coloured |
| `Fable` | pinned | Which build of the compiler snippets use |
| `FableTool` | `None` | Precompile with a Fable of your own instead of the matching one |
| `Stats` | `false` | Show a tab with what the compile cost |

### Tab

Left alone, a snippet opens on the console - or on the result when it drew something and printed
nothing. Say which you want and it always opens there:

```fsharp
|> LiveExample.registerWith (LiveExample.tab OutputTab)
```

A block can ask for its own, which wins over what you set here:

````markdown
```fsharp live tab=output
printfn "look at what Fable made of this"
```
````

A snippet that failed to compile always opens on the console, whatever either of you asked for: the
errors are there.

### Highlighting

An edited snippet is coloured by the editor's own F# mode. Colour it with the same tree-sitter
grammar as the rest of your site and it looks exactly like the block it replaced, at the cost of
fetching the F# grammar alongside the compiler:

```fsharp
|> LiveExample.registerWith (LiveExample.highlighting TreeSitterHighlighting)
```

### Fable

Snippets compile with the version this plugin was built against. Name your own to follow Fable
without waiting for a release here:

```fsharp
|> LiveExample.registerWith (LiveExample.fable (Pinned("3.1.0", "2.1.0")))
```

:::caution Important
The versions here are not the one coming from Fable CLI on NuGet but the version of

- [fable-standalone](https://www.npmjs.com/package/@fable-org/fable-standalone)
- [fable-metadata](https://www.npmjs.com/package/@fable-org/fable-metadata)
:::

Most of the time, you will want to use the lastest version of both available at the time.

You can also always take the newest of each:

```fsharp
|> LiveExample.registerWith (LiveExample.fable Latest)
```

This asks npm on every build, so two builds of the same commit can differ. The versions it picked
are written to the build log.

## Reference

Every function and option of it, signature by signature: [`LiveExample`](../reference/nacara-plugin-liveexample/nacara-plugins/liveexample.md).
