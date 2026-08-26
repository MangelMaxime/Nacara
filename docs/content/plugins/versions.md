---
title: Versions
---

Publishes several versions of your documentation side by side, with a switcher in the navbar and a
notice on pages that are no longer current.

## How versioned sites work here

A version is a **build**, not a dimension of your content. You build the docs from a tag and deploy
that build into a directory of its own, so building the current docs costs the same whether you keep
two versions or twenty. An old version stays as you published it because its sources are a tag and
tags do not move - nothing here locks it, and you rebuild one with the same checkout and copy that
made it. This is how mike does it for MkDocs, and Read the Docs for Sphinx.

```text
example.com/          ← the current version, built from main
example.com/2.0/      ← built from the 2.0 tag, deployed once
example.com/1.0/      ← built from the 1.0 tag, deployed once
example.com/versions.json
```

## Add it

```bash frame=terminal
dotnet add package Nacara.Plugin.Versions --prerelease
```

```fsharp ins={1-5,10-12,18}
let versions =
    [
        SiteVersion.root "3.0"          // served from the deployment root
        SiteVersion.create "2.0" "2.0"  // served from /2.0/
        SiteVersion.create "1.0" "1.0"
    ]

let theme =
    Theme.defaults
    |> Theme.navbarEnd
        [
            NavbarDynamicWidget(Versions.switcher (Versions.versions versions Versions.defaults))
        ]

let site =
    Site.create "My library"
    |> Markdown.register
    |> Versions.register versions
    |> Theme.register theme
```

You pass the same list in both places: the plugin writes the manifest, and the widget says where the
switcher sits.

The first argument of `SiteVersion.create` is the **label** a reader sees, and the second is the URL
segment. They do not have to match - `SiteVersion.create "3.0 (latest)" "3.0"` and
`SiteVersion.root "next"` are both fine.

## Building a version

Tell the build which version it is, or every URL it writes points at the root:

```bash
dotnet run -- build                # the current version, at the root
dotnet run -- build --version 2.0  # everything under /2.0/
```

...or say it in the site itself, when the branch you build from decides:

```fsharp
Site.create "My library" |> Site.version "2.0"
```

Deploying is then a copy: the current version's output at the root, and each older build in its own
directory. They never overwrite each other, because their prefixes differ.

## What a reader gets

A switcher listing every version, with the one they are reading selected. It reads that from the
URL, so it stays right even on a 404 served from the root. Picking another version keeps them on the
same page where it exists, and takes them to that version's home page where it does not.

Pages of an older version carry a notice with one click back to the current one:

```text
⚠ Older version
You are reading the documentation for 2.0. The current version is 3.0. Go to 3.0
```

## Options

| Option | Default | Effect |
|---|---|---|
| `Versions` | `[]` | The versions to list, in the order readers should see them |
| `ManifestPath` | `"versions.json"` | Where the manifest is written, relative to the output |
| `ShowOutdatedNotice` | `true` | Show the notice on pages of an older version |

## The manifest

```json title="versions.json"
[{"label":"3.0","prefix":"","latest":true,"current":true},
 {"label":"2.0","prefix":"2.0","latest":false,"current":false}]
```

Every build writes one describing what it knows. It belongs at the root of the deployment, above the
version directories, so publishing the current version keeps it up to date. Your deployment tooling
can read it to find out what exists.

## Locales and versions together

They compose. A version is a whole build and locales live inside it, so `/2.0/fr/guide/` is the
French guide of version 2.0, and you configure nothing extra.

## Reference

Every function and option of it, signature by signature: [`Versions`](../reference/nacara-plugin-versions/nacara-plugins/versions.md).
