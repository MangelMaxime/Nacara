---
title: Project layout
---

Nacara has no required directory names. Your site says where its files are, so the layout below is
a convention, not a rule.

```text title="A typical site"
docs/
├── Docs.fsproj        the site is a project
├── Site.fs            its description, in F#
├── content/           markdown, read by a collection
│   ├── index.md
│   └── guide/
│       └── getting-started.md
├── static/            copied verbatim into the output
└── output/            generated - do not commit
```

## The site is the program

There is no `nacara` binary deciding how your site is built. The engine is a library, your project
references it, and `Nacara.run` gives that project a command line:

```fsharp
[<EntryPoint>]
let main argv = Nacara.run site argv
```

That gives you two things. The engine version is a package reference, so the tool and the site
cannot drift apart. And your configuration is ordinary F#: you can split it across files, share it
between sites, generate it, or test it.

## Where things are resolved from

Paths in the configuration - the collection source, the static directory, the output directory - are
relative to the **project root**, which is the directory holding your site's project file.

All three of these do the same thing:

```bash frame="terminal"
cd docs && dotnet run -- build
dotnet run --project docs/Docs.fsproj -- build
dotnet run --project /path/to/docs/Docs.fsproj -- build
```

`--root <dir>` overrides it, and `--verbose` prints the root it settled on. A published binary has
no project file beside it, so it uses the current directory instead - pass `--root` to point it
somewhere else.

## Content and output

A collection reads files from one directory. Anything in the static directory is copied as it is.
The output directory is rebuilt in place: files that nothing produces any more are deleted, so a
page you delete does not stay online.
