# Nacara

A documentation engine for F#, where the site is an F# program.

```fsharp
let site =
    Site.create "My library"
    |> Site.baseUrl "/"
    |> Markdown.register
    |> TextMate.register
    |> Theme.register theme
    |> Site.collection (Theme.docs theme "content")

[<EntryPoint>]
let main argv = Nacara.run site argv
```

Nacara runs on .NET, needs no Node.js, and type-checks your configuration. Front matter is decoded
into types you declare, links are resolved against the route table so a broken one fails the build,
and markdown, highlighting, search, changelogs, versions, literate F#, link checking and the API
reference of your library are all plugins on a small core.

```bash
dotnet new install Nacara.Templates
dotnet new nacara-docs -o docs
cd docs && dotnet run -- watch
```

## What is here

| Package | What it does |
|---|---|
| `Nacara.Core` | The engine: collections, routing, diagnostics, the plugin pipeline, the CLI |
| `Nacara.Plugin.Markdown` | Markdown through Markdig: directives, table of contents, link resolution |
| `Nacara.Plugin.Highlight.TextMate` | Syntax highlighting from TextMate grammars, emitted as CSS classes |
| `Nacara.Plugin.Highlight.TreeSitter` | The same, from tree-sitter grammars compiled to wasm |
| `Nacara.Plugin.Search` | Static search, powered by pagefind |
| `Nacara.Plugin.Changelog` | Keep a Changelog files as pages |
| `Nacara.Plugin.Versions` | Several versions deployed side by side, with a switcher |
| `Nacara.Plugin.Literate` | F# source files as pages |
| `Nacara.Plugin.LiveExample` | F# snippets a reader edits and runs, compiled in the browser by Fable |
| `Nacara.Plugin.FSharpApi` | Reference pages for an F# library, read from the assemblies it ships |
| `Nacara.Plugin.LinkValidator` | Every link the site published, checked - anchors and external ones too |
| `Nacara.Plugin.Linter.Rumdl` | The site's markdown linted by rumdl, reported as build diagnostics |
| `Nacara.Plugin.Sitemap` | `sitemap.xml` and `robots.txt`, with `hreflang` cross-references |
| `Nacara.Plugin.Assets.LightningCss` | CSS compiled for the browsers you name, bundled and minified |
| `Nacara.Plugin.Assets.Esbuild` | JavaScript bundled from what it imports, and minified |
| `Nacara.Plugin.Assets.Nuglify` | HTML, JavaScript and CSS minified through NUglify |
| `Nacara.Theme.Default` | Layout, design tokens and web components |
| `Nacara.Templates` | `dotnet new nacara-docs` |

`docs/` is this repository's own site, built by the engine it documents.

## Development

You need the .NET SDK pinned in `global.json`, and Node. After cloning:

```bash
dotnet tool restore
dotnet husky install     # commit-message hook
```

`build.sh` and `build.bat` are the same entry point on either platform, and they install the Node
dependencies themselves. Run `npm ci` yourself if you build with `dotnet build` instead.

```bash
./build.sh test                 # the test suite (-u accepts new snapshots)
./build.sh format               # Fantomas the F#, Biome the css and the javascript
./build.sh docs watch           # this repository's site, served and live-reloading
./build.sh docs check           # build every page, write nothing, fail on anything wrong
./build.sh --help               # everything else
```

`test` and `docs check` both run in CI on every push, so run them before opening a pull request.

Commits follow [Conventional Commits](https://www.conventionalcommits.org), checked by a git hook
and again in CI. The changelogs are written from them.

### The tree-sitter runtime

`Nacara.Plugin.Highlight.TreeSitter` needs two native libraries - tree-sitter built with wasm
support, and wasmtime. They are looked for in this order:

1. `src/Nacara.Plugin.Highlight.TreeSitter/runtimes/<rid>/native`, copied beside whatever you build
2. `~/.cache/nacara/tree-sitter-runtime/<version>`
3. `@nacara/tree-sitter-runtime-<rid>` on npm, downloaded into that cache

On a fresh clone you do nothing: the first build that colours code fetches them.

Build them yourself after bumping `Runtime.Version` in
`src/Nacara.Plugin.Highlight.TreeSitter/Runtime.fs`, since npm has nothing under the new version
until CI publishes it:

```bash
./build.sh tree-sitter runtime           # needs a C compiler; cl.exe on Windows
./build.sh tree-sitter bundle            # the grammars that ship in the package
./build.sh tree-sitter publish --dry-run
```

A copy in `runtimes/` wins over the cache and over npm whatever its version, so delete it when you
want to check what a user gets.

The `Native runtimes` workflow builds all six platforms and publishes them to npm when dispatched
with `publish=true`.

## Releasing

Each package carries its own `CHANGELOG.md` beside its project, written by
[EasyBuild.ShipIt](https://github.com/easybuild-org/EasyBuild.ShipIt) from the commits that touched
that package.

```bash
dotnet shipit --dry-run --allow-branch main --skip-merge-commit --skip-invalid-commit
```

CI runs the same after a green build on `main` and opens a pull request updating the changelogs.
Merging it writes a `chore: release …` commit, and that commit publishes.

The first release of a package needs `force_version` in its changelog's front matter. Remove the
line once it has been released.

## Status

Version 3 is under development. Version 2 - a Fable and Node.js generator with a different
architecture - lives on `master` and is in maintenance.

## Licence

Apache-2.0
