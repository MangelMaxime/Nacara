---
title: Writing a plugin
---

Everything Nacara does is a plugin, including markdown, so yours can do anything the built-in ones
do. This page builds one from nothing, then lists what you can hook into.

## Your first plugin

Start a library that references the engine:

```bash frame=terminal
dotnet new classlib -lang F# -n Nacara.Plugin.Rss
cd Nacara.Plugin.Rss
dotnet add package Nacara.Core --prerelease
```

A plugin is a name and a function from a registry to a registry. Registration is pure: you say what
the build should do, and none of it happens yet.

```fsharp title="Rss.fs"
namespace Nacara.Plugins

open Nacara.Core

type RssOptions = { Title: string; Path: string }

[<RequireQualifiedAccess>]
module Rss =

    let defaults = { Title = "Updates"; Path = "rss.xml" }

    type private RssPlugin(options: RssOptions) =
        interface IPlugin with
            member _.Name = "rss"

            member _.Configure registry =
                registry
                |> Registry.onBuildComplete (fun context ->
                    context.Write options.Path (renderFeed options context.Pages) |> ignore
                )

    /// The plugin itself.
    let create () = RssPlugin(defaults) :> IPlugin
    let createWith configure = RssPlugin(configure defaults) :> IPlugin

    /// How a site adds it.
    let register (site: Site) = Site.plugin (create ()) site
    let registerWith configure (site: Site) = Site.plugin (createWith configure) site
```

Then a site adds it exactly like any other:

```fsharp
Site.create "Docs"
|> Markdown.register
|> Rss.register
```

Point a site's project at your library with `dotnet add reference` while you work on it, and publish
it as an ordinary NuGet package when it is ready. You have no plugin registry to get listed in and
no manifest to write: a plugin is a package, and adding one is a line of F#.

Four conventions make yours feel like the rest:

- `create` / `createWith` build the plugin, `register` / `registerWith` put it on a site. Users
  write `register`, and that is what the guides show.
- Options are a **record** with a `defaults` value, so a user gets completion and the compiler
  checks their configuration.
- Name the plugin after the package's last segment - `rss` for `Nacara.Plugin.Rss`. Diagnostics are
  stamped with it.
- Ship your CSS and JS as assets of the plugin rather than asking the theme to carry them.

## Extension points

| Registration | When it runs |
|---|---|
| `Registry.contentSource` | Discovery: contribute files or generated content |
| `Registry.collection` | Discovery: contribute a whole collection |
| `Registry.markdownExtension` | Parsing: extend the markdown pipeline |
| `Registry.codeBlockAnnotation` | Rendering: extend code blocks |
| `Registry.layout` | Rendering: contribute a named layout |
| `Registry.asset` | Output: ship CSS, JS or files |
| `Registry.assetTransform` | Output: change a text asset on its way out, such as minifying it |
| `Registry.onPagesRouted` | After every page is routed and rendered, before writing |
| `Registry.onBuildComplete` | After the output is written: indexes, sitemaps, manifests |
| `Registry.frontMatter` | Discovery: teach the engine how a kind of file carries its front matter |
| `Registry.extra` | Contribute a typed value other plugins read back |
| `Registry.command` | Add a subcommand to the site's command line |

Some of those typed values are extension points the engine looks for by name:

| Type | Who provides it | What it decides |
|---|---|---|
| `IHighlighter` | the highlight plugin | how code is tokenised |
| `ICodeBlockRenderer` | the theme | what a code block looks like |
| `PageAsset` | any plugin | stylesheets and scripts every page loads |

The last `ICodeBlockRenderer` registered wins, so a site can override the one its theme provides.
With none registered the engine falls back to plain `<pre><code>`, so even a site with no theme
renders readable code.

## Commands of your own

A build builds. For work that is not that - rewriting sources, clearing a cache, printing what your
plugin knows - add a command, and a reader runs it when they want it:

```fsharp
registry
|> Registry.command (
    PluginCommand.create "fmt" "Fix the markdown rumdl can fix, in place" (fun projectRoot arguments ->
        …
    )
)
```

```bash frame="terminal"
dotnet run --project docs -- fmt docs/content/guide
```

`Run` is given the project root and everything typed after the command's name, and returns the exit
code. Nacara does not read those arguments, so their shape is yours.

Say what that shape is, or nobody will guess it:

```fsharp
PluginCommand.create "fmt" "Fix the markdown rumdl can fix, in place" (fix options)
|> PluginCommand.help
    """fmt - fix the markdown rumdl can fix, in place

USAGE
    fmt [path...]

With no path it starts from the project root."""
```

That is what `fmt --help` prints. Without it a reader gets the one-line summary, which says what the
command does and nothing about what to type after it.

It is listed in `--help` beside the engine's own commands, with your plugin's name after it so a
reader knows which package it came with:

```text frame="terminal"
COMMANDS
    build           Build the site once (default)
    …
    fmt             Fix the markdown rumdl can fix, in place (linter-rumdl)
```

Name it for what it does rather than for your plugin - a reader reaches for `fmt`, not for the tool
behind it. If the obvious word is one another plugin would also want, prefix it with your own name.
Two plugins claiming one word is an error naming both, rather than a coin toss.

## Who owns the markup

The engine follows one rule, and your plugin should too: **plugins emit meaning, the theme decides
appearance.** Contribute elements, data attributes and web components; leave class names and looks
to whoever wrote the stylesheet.

Code blocks are the worked example. The engine decides what a block *is* - which lines are marked,
which words, what the tokens are - and hands that over as a `PreparedCodeBlock`. The theme decides
what it looks like, and the class names live beside the CSS that styles them. That is why another
theme can render the same block completely differently and no plugin needs to know.

## Front matter

The engine knows nothing about `---`. A format is data - an extension, the line that opens the
block, the line that closes it, and the wrapper it sits in - and a plugin provides it:

```fsharp
// markdown: the block is at the top of the file
{ Name = "markdown"
  Extensions = [ ".md"; ".markdown" ]
  Opening = "---"
  Closing = "---"
  Wrapper = None }

// literate F#: a file starting with --- does not compile, so the block sits in a comment
{ Name = "literate"
  Extensions = [ ".fsx"; ".fs" ]
  Opening = "---"
  Closing = "---"
  Wrapper = Some("(**", "*)") }
```

A file whose extension no format claims fails the build with `nacara/unknown-front-matter-format`,
naming the extension - so a site learns it needs a plugin, rather than getting a page whose body is
full of delimiters.

Two formats may claim the same extension. Nothing prevents it, and sometimes it is what you want, to
override what another plugin brought. The last one registered is used, and the clash is reported as
`nacara/duplicate-front-matter-format`, so the choice is never silent.

## Writing files

A hook writes through the build, not through the file system:

```fsharp
Registry.onBuildComplete (fun context ->
    context.Write "feed.xml" (renderFeed context.Pages) |> ignore
)
```

That does two things a bare `File.WriteAllText` cannot. It leaves the file alone when the content is
identical, so a build that changes nothing changes nothing on disk - a file rewritten every time is
one a dev server, a deploy tool or another process reacts to every time. And it tells the build the
file exists, so pruning does not remove it as an orphan on the next run.

Use `Registry.preserve` for output the build cannot write itself, like a search index produced by an
external binary. It is the exception, not the habit.

If two things write the same path in one build, the build says so with `nacara/duplicate-output`,
because each would overwrite the other and the file would change on every run.

## Plugins that extend each other

`Registry.extra` is a typed bag. The highlight plugin contributes an `IHighlighter`, the markdown
plugin asks for every one registered, and neither knows about the other:

```fsharp
// in the highlight plugin
registry |> Registry.extra (TextMateHighlighter(options) :> IHighlighter)

// in the markdown plugin
let highlighters = Registry.extras<IHighlighter> context.Registry
```

The same mechanism lets a plugin add a stylesheet or a script to every page without knowing which
theme is in use:

```fsharp
registry |> Registry.extra (Script("assets/search.js", true))
```

## Generated pages

Your content does not have to exist on disk. A producer returns documents, front matter included,
and they enter the pipeline exactly like files: same transforms, same routing, same link
checking:

```fsharp
Collection.create "api" decoder
|> Collection.producer "api" (fun context ->
    assemblies
    |> List.map (fun assembly ->
        GeneratedContent.create $"{assembly.Name}.md" (renderMarkdown assembly)
        |> GeneratedContent.dependsOn [ assembly.Path ]
    )
)
```

## Registration composes

`register` and `registerWith` are what users write:

```fsharp
Site.create "Docs"
|> Markdown.register
|> MyPlugin.registerWith (fun options -> { options with Loud = true })
```

They are `Site -> Site` functions, so they compose, and you can turn a shared setup into a value:

```fsharp
let standard = Markdown.register >> TextMate.register >> Search.register

Site.create "Docs" |> standard |> Theme.register theme
```

`create` stays public as the primitive underneath: it hands you the plugin as a value, for tests,
for conditional registration - `|> (if isCI then Search.register else id)` - and for anyone wrapping
Nacara in something of their own.

## One setter per option

`registerWith` takes `Options -> Options`, so a record update works and always will. Ship a setter
per option as well, and users can compose them instead:

```fsharp
|> MyPlugin.registerWith (
    MyPlugin.loud true
    >> MyPlugin.retries 3)
```

Each one is three lines, and reuses the field's own summary so the two cannot drift:

```fsharp
/// <summary>Say what it is doing while it works.</summary>
/// <param name="value">The value to use.</param>
/// <param name="options">The options so far.</param>
let loud value (options: MyPluginOptions) =
    { options with
        Loud = value
    }
```

Two rules every plugin here follows:

- **Booleans take the value.** `loud true`, not a bare `loud` that only switches on. A bare toggle
  reads well until a shared setup turns something on and a site needs it off.
- **The name is the field, camel-cased.** If a private function already owns that name, rename the
  private one - the public surface is what users have to live with.

:::note What register may do
`register` should do everything your plugin needs of the site, and nothing else. If it adds a
collection as well as the plugin, say so in your documentation: a route appearing from a one-word
call is a surprise unless you wrote it down. `create` is the "only the plugin" escape hatch.
:::

## Plugins that drive a program

If your plugin needs a binary - a search indexer, a minifier, a linter - say which one and the
engine fetches it: pinned, cached under `~/.cache/nacara/<name>/<version>/`, downloaded once per
machine, unpacked whatever it arrives in, and marked executable.

```fsharp
let private request =
    Tool.platform ()
    |> Result.map (fun platform ->
        {
            Name = "pagefind"
            Version = "1.5.2"
            Url = $"https://github.com/Pagefind/pagefind/releases/download/v1.5.2/{archive platform}"
            // TarGzip, Zip, Gzip for one gzipped program, or Raw
            Archive = TarGzip
            // Found wherever the archive keeps them - an npm tarball puts everything under
            // package/ - and left in one directory
            Files = [ "pagefind" ]
            Executable = [ "pagefind" ]
            // Verified before anything is unpacked, when the publisher offers one
            Checksum = None
        }
    )

Tool.file "pagefind" request
```

`Tool.platform` tells you what this machine is: a RID, an architecture, and which operating system.
Every publisher names those differently, and only your plugin knows the naming.

Nothing reaches the network unless you allow it: `Tool.resolve false` refuses, with an error naming
the directory it looked in, which is what an offline build needs.

## Where a plugin keeps what it works out

There are two caches, and the one you want depends on whether the site's own files are in the key.

`~/.cache/nacara`, which `Tool` writes to, is for what you **fetched**: a pinned binary, a grammar at
a commit. Those bytes are the same for every project, so every site on the machine shares them.

`.nacara`, beside the site, is for what you **worked out from the site's sources**. Ask
`ProjectCache` for a directory, naming the job and what the entry is keyed by:

```fsharp
let staged = ProjectCache.directory context.ProjectRoot "bundles" hash
```

An entry keyed by a source file is orphaned the moment that file changes, so say which entries this
build used and the rest of the group goes:

```fsharp
ProjectCache.forgetOthers context.ProjectRoot "bundles" [ hash ]
```

The directory ignores itself in git and the watcher skips it, so a site needs no rule of its own.
Deleting it costs a rebuild and nothing else, and `nacara clean` removes it along with the output.

## Diagnostics

Report problems through the build's diagnostics rather than throwing. Give the rule, the position,
and above all the fix:

```fsharp
context.Diagnostics.Add(
    Diagnostic.error "site-url-missing" "The feed needs an absolute site url"
    |> Diagnostic.withHint "Set it with Site.baseUrl"
)
```

A reader sees `rss/site-url-missing`: you name the rule, and the engine stamps your plugin's name on
it, so nothing has to be allocated or coordinated. Anything you register while your plugin is being
configured reports under it, transforms and hooks alike.

Name a rule after the situation, not the remedy: `link-target-missing`, `pagefind-failed`,
`css-not-minified`. Keep it once you have published it - it is what a user searches for, and what CI
greps for.
