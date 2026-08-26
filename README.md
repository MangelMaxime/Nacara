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

## Working on it

Requirements: the .NET SDK pinned in `global.json`, and Node. `build.sh` and `build.bat` are the
same entry point on either platform. `npm ci` once: `package.json` pins esbuild, which bundles the
JavaScript that ships inside `Nacara.Theme.Default` and `Nacara.Plugin.LiveExample`, and Biome,
which formats the css and the javascript. Node builds *this repository*; a site needs none of it,
because the packages ship the bundles already built.

```bash
dotnet build Nacara.slnx        # build everything
./build.sh test                 # run the tests (--update-snapshots accepts them)
./build.sh format               # format the F#, the css and the javascript
./build.sh docs watch           # the documentation, served and live-reloading
./build.sh docs watch --host    # the same, reachable from another machine
./build.sh docs check           # build it all, write none of it
./build.sh --help               # everything else the build project does
```

[`ARCHITECTURE.md`](ARCHITECTURE.md) records the decisions and why they were made.
[`ROADMAP.md`](ROADMAP.md) is the durable state of the effort.

## Status

Version 3 is under development on this branch. Version 2 - a Fable and Node.js generator with a
different architecture - lives on `master` and is in maintenance.

## Licence

Apache-2.0

## Releasing

Each package carries its own `CHANGELOG.md` beside its project, written by
[EasyBuild.ShipIt](https://github.com/easybuild-org/EasyBuild.ShipIt) from the commits that touched
that package - so a change to the search plugin releases the search plugin, and nothing else.

```bash
dotnet shipit --dry-run --allow-branch v3 --skip-merge-commit --skip-invalid-commit
```

That prints the version each package would get and the pull request it would open. CI does the same
thing after a green build on `v3`; merging that pull request writes a `chore: release …` commit, and
that commit is what publishes. Versions and release notes come from the changelogs through
[EasyBuild.PackageReleaseNotes.Tasks](https://github.com/easybuild-org/EasyBuild.PackageReleaseNotes.Tasks),
so no version is written by hand anywhere.

Commit messages are [Conventional Commits](https://www.conventionalcommits.org), checked by
`EasyBuild.CommitLinter` in a git hook and again in CI - because they are what the changelogs are
made of. `dotnet husky install` sets the hooks up after a fresh clone.

The first release of a package needs `force_version` in its changelog's front matter, since the
version ShipIt would work out on its own starts from zero. Each package carries its own line:
`Nacara.Core` continues the one already on NuGet and starts at `2.0.0-beta.1`, and every other
package is new and starts at `1.0.0-beta.1`. Remove the line once a package has been released -
it is applied on every run, not only the first.
