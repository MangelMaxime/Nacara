---
title: Sitemap
---

Writes `sitemap.xml` and `robots.txt` from the pages you built, with translations cross-referenced.

## Add it

```bash frame=terminal
dotnet add package Nacara.Plugin.Sitemap --prerelease
```

```fsharp
Site.create "My library"
|> Site.origin "https://example.com"
|> Markdown.register
|> Sitemap.register
```

`Site.origin` is required. A sitemap holds absolute URLs, and only you know where your site is
published. Leave it out and nothing is written, and the build tells you why:

```text frame="terminal"
! warning sitemap/origin-missing: No sitemap was written: the site does not say where it is published
    hint: Declare it with Site.origin "https://example.com"
```

A sitemap full of URLs from the wrong origin is worse than no sitemap, so you get a warning and no
file rather than a guess.

## What you get

```xml title="sitemap.xml"
<url>
  <loc>https://example.com/guide/setup/</loc>
  <xhtml:link rel="alternate" hreflang="en" href="https://example.com/guide/setup/" />
  <xhtml:link rel="alternate" hreflang="fr" href="https://example.com/fr/guide/setup/" />
</url>
```

Translations cross-reference each other, so a search engine can offer a reader the language they
asked for instead of picking one. You get that from [locales](../guide/i18n.md) with nothing to
configure.

The origin you declared here also lets the default theme put `<link rel="canonical">` on every page,
so a site published under several locales does not compete with itself in results. That part belongs
to the theme, and it happens whether or not you register this plugin.

## Options

Leave out drafts, or anything else a search engine should not be offered:

```fsharp
|> Sitemap.registerWith (fun options ->
    { options with
        ExcludeCollections = [ "drafts" ]
    }
)
```

| Option | Default | Effect |
|---|---|---|
| `Path` | `"sitemap.xml"` | Where the sitemap is written, relative to the output |
| `WriteRobots` | `true` | Also write a `robots.txt` pointing at it |
| `ExcludeCollections` | `[]` | Collections to leave out, by name |

Turn `WriteRobots` off when you keep a `robots.txt` of your own in the static directory. Otherwise
both want the same file and the build reports `nacara/duplicate-output`.

## Versioned sites

Each version is its own build, so each one writes its own sitemap under its own prefix. Submit the
current version's, and leave the older ones where they are for anyone who follows a link into
them.

## Reference

Every function and option of it, signature by signature: [`Sitemap`](../reference/nacara-plugin-sitemap/nacara-plugins/sitemap.md).
