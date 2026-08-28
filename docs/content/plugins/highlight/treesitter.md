---
title: tree-sitter
---

Colours code blocks with [tree-sitter](https://tree-sitter.github.io) grammars: a real parser per
language, and the grammar's own queries to decide what its nodes mean.

## Add it

```bash frame=terminal
dotnet add package Nacara.Plugin.Highlight.TreeSitter --prerelease
```

```fsharp ins={4}
let site =
    Site.create "My library"
    |> Markdown.register
    |> TreeSitter.register
    |> Theme.register theme
```

**Twelve languages come inside the package**, and nothing is compiled to colour them.

The parser is native, and Nacara publishes one build per platform. The first build on a machine
fetches the pair it needs - about 8 MB, kept in `~/.cache/nacara` and shared by every site built
there.

| | |
|---|---|
| F#, C# | `fsharp` `fs` `fsx` `fsi`, `csharp` `cs` |
| Web | `javascript` `js` `jsx`, `typescript` `ts`, `html`, `css` |
| Configuration file | `json`, `yaml` `yml`, `toml`, `xml` |
| Content | `markdown` `md` |
| Scripts | `bash` `sh`, `shell`, `zsh` |

### The names a fence can use

A language answers to more than one name, so every one of these reaches the F# grammar:

````markdown
```fsharp    ```fs    ```fsx    ```fsi
````

The same goes for `js`/`javascript`, `yml`/`yaml`, `sh`/`bash` and the rest. Add a name of your own
on the grammar:

```fsharp
TreeSitter.bundled "fsharp" |> TreeSitter.aliases [ "dotnet" ]     // ```dotnet works too
```

## Difference with TextMate

A TextMate grammar matches patterns, tree-sitter parses. The difference shows in what a name is
*used as*:

```text
type Page = { Title: string }
     ^^^^          ^^^^^^
     TextMate: an identifier      tree-sitter: a type
```

For F# that is not a nuance: the TextMate grammar emits no type scope at all, so every type in a
signature is ordinary text, while tree-sitter tells a type from a constructor from a field.

## Adding a new language

For any language with a tree-sitter grammar on GitHub, give a name, a repository and a
reference:

```fsharp
|> TreeSitter.registerWith (fun options ->
    { options with
        Grammars =
            [
                TreeSitter.fromGitHub
                    "nix"
                    "https://github.com/nix-community/tree-sitter-nix"
                    "b3cda619248e7dd0f216088bd152f59ce0bbe488"
            ]
    }
)
```

The grammar is built once and kept, so every later build reads what the first one left. The
reference can be a **branch, a tag or a commit**. Use a commit: it cannot move under your site, and
it pins the queries to the grammar they were written for.

A language you name wins over the one in the package, so you can use a newer F# grammar without
giving up the other eleven. Set `UseBundledGrammars = false` to drop all twelve and provide
everything yourself.

Most repositories hold one grammar at the top with its queries under `queries/`, and need nothing
more than the lines above. Two cases need more:

```fsharp
// Several grammars in one repository.
TreeSitter.fromGitHub "xml" "https://github.com/tree-sitter-grammars/tree-sitter-xml" "5000ae8f22"
|> TreeSitter.inDirectory "xml"

// Queries kept elsewhere - or queries of your own, to recolour a language without
// touching its parser.
TreeSitter.fromGitHub "vhs" "https://github.com/charmbracelet/tree-sitter-vhs" "main"
|> TreeSitter.queriesAt "queries/highlights.scm"
```

If you have already built a grammar, name its two files directly - which is what you want when your
repository keeps its own:

```fsharp
TreeSitter.grammar "fsharp" "grammars/fsharp/grammar.wasm.gz" "grammars/fsharp/highlights.scm"
```

### What building one costs

The twelve in the package cost you nothing. Anything else is compiled where you build the site, and
the first one brings a toolchain with it: the tree-sitter CLI and the wasi-sdk it compiles with,
some 150 MB to download and around 600 MB unpacked. It all lands in `~/.cache/nacara/tree-sitter/`,
so you pay it once per machine. A grammar itself takes seconds and under a megabyte.

Nacara ignores whatever tree-sitter you have installed, even at the right version: a grammar built
by one tree-sitter and loaded by another fails at run time.

### Building on CI

Cache `~/.cache/nacara` and the toolchain is fetched once instead of once per run.

Set `AutoBuild = false` for a build that must fetch nothing. A grammar already in the cache is still
used; one that is not becomes an error naming where it was looked for.

### Incomplete snippets

Documentation is full of fragments. An F# snippet that opens with `|>` is a pipeline with its first
line left out, and a parser reading it alone takes that bar for the start of a match case - which is
how one page ends up colouring `|>` two different ways.

So when a parse comes back with errors, Nacara tries again with something in front of the fragment,
and keeps the second reading only if the errors go away. F# hangs on `()`, and you can say what
another language needs:

```fsharp
TreeSitter.bundled "fsharp" |> TreeSitter.continuedFrom "()"
```

## Colours

Every capture a grammar's queries make - `keyword`, `variable.parameter`, `constructor` - becomes
one of the theme's [token classes](index.md#colours), the same ones TextMate produces. Recolour a
token there and it changes everywhere, whichever highlighter drew it.

To change what is captured, rather than how it is coloured, point `queriesAt` at a `highlights.scm`
of your own.

## Options

| Option | Default | Effect |
|---|---|---|
| `Grammars` | `[]` | The languages to add, one entry each |
| `UseBundledGrammars` | `true` | Keep the twelve languages inside the package |
| `AutoBuild` | `true` | Build a grammar named by repository when the cache has none |
| `RuntimePath` | `None` | Where the tree-sitter and wasmtime libraries are |
| `RuntimeSource` | npm | Where the pair is published, with `{version}` and `{rid}` in it |
| `CliSource`, `WasiSdkSource` | upstream releases | Where the toolchain is downloaded from |

Point the three sources at a mirror when your build cannot reach upstream. For a build that must
fetch nothing at all, point `RuntimePath` at a directory holding the
two libraries. Nacara publishes them for Linux, macOS and Windows on x64 and arm64, so in theory you
should not need to configure any of it.

## Reference

Every function and option of it, signature by signature:
[`TreeSitter`](../../reference/nacara-plugin-highlight-treesitter/nacara-plugins/treesitter.md).
