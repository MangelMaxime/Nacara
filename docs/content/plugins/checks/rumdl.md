---
title: Markdown linter
---

Lints your site's markdown with [rumdl](https://github.com/rvben/rumdl), a fast markdown linter, and
reports what it finds as ordinary Nacara diagnostics, file and line and column included.

## Add it

```bash frame="terminal"
dotnet add package Nacara.Plugin.Linter.Rumdl --prerelease
```

```fsharp ins={4}
let site =
    Site.create "My library"
    |> Markdown.register
    |> Rumdl.register
    |> Theme.register theme
```

## What it reads

**The pages in the build**, and only the markdown ones. It does not read a directory, so a note you
keep beside your content that no collection publishes is not linted - and a generated page, like an
API reference or a changelog, has no file to lint.

## What it reports

```text frame="terminal"
✗ content/guide/deploy.md(166,20): error linter-rumdl/md009: 3 trailing spaces found
```

A finding is **a warning while you watch and an error when you build**, so it shows up where you
are writing without stopping the page from rendering, and it stops a release build. Set `Severity`
to pick one for both.

## Two rules it turns off, and why

The defaults this plugin ships are not rumdl's. Two rules are off:

| Rule | Why it is off |
|---|---|
| `MD057` relative link exists | Links are resolved against [the route table](../../guide/content.md#links-follow-files-not-urls), which knows the pages a plugin generates. A file-system check calls every one of those broken. |
| `MD033` inline HTML | `<kbd>` and friends are how a documentation page says what it means. |

The rest are set for prose: 100 columns instead of 80, and neither code blocks nor tables measured
at all.

```fsharp
// What ships, and what a site can drop with UseDefaults = false.
MD057.enabled = false
MD033.enabled = false
MD013.line-length = 100
MD013.code-blocks = false
MD013.tables = false
```

## Configuring it

Three layers, and yours has the last word:

1. the defaults above, unless `UseDefaults = false`
2. a `.rumdl.toml`, which rumdl finds by itself - or `ConfigPath` to name one, or `Isolated = true`
   to read none
3. `Settings`, as inline TOML

```fsharp
|> Rumdl.registerWith (fun options ->
    { options with
        Settings = [ "MD013.line-length = 120" ]
        Disable = [ "MD041" ]
    }
)
```

## Options

| Option | Default | Effect |
|---|---|---|
| `BinaryPath` | `None` | An existing rumdl to use; unset, it is downloaded and cached |
| `Source` | rumdl's releases | Where that release is published, with `{version}` and `{target}` in it |
| `UseDefaults` | `true` | Ship the settings above, under anything the site says |
| `ConfigPath` | `None` | A `rumdl.toml` to read, when it is not the one rumdl would find |
| `Isolated` | `false` | Ignore configuration files entirely |
| `Settings` | `[]` | Inline TOML, applied last: `"MD013.line-length = 120"` |
| `Disable` | `[]` | Rules to switch off by name |
| `Severity` | `WarningWhileWatching` | Or `AlwaysWarning`, or `AlwaysError` |
| `LintWhileWatching` | `true` | It costs milliseconds, so it runs while you write |

## When it cannot run

A linter that will not start is a warning, never a failed build:

```text frame="terminal"
! warning linter-rumdl/not-linted: Markdown is not linted: rumdl has no build for Windows on Arm
    hint: The site is built and correct, only unchecked. Set BinaryPath to use your own rumdl.
```

That is the one platform upstream publishes no binary for. Everywhere else it is fetched for
you.

## Fixing what it finds

rumdl fixes some findings itself, and says which: one it can fix carries a hint. A build never
rewrites your files, so fixing is a command you run:

```bash frame="terminal"
dotnet run --project docs -- fmt
```

It uses the binary the plugin already fetched - you do not need rumdl on your `PATH` - with the same
rules the build lints by, so what it writes is what the build then accepts. Name paths to narrow it:

```bash frame="terminal"
dotnet run --project docs -- fmt docs/content/guide
```

With nothing named it starts from the project root.
