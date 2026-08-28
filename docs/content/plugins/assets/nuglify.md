---
title: NUglify
---

Makes the files your site ships smaller. Nothing to install, nothing to download.

## Add it

```bash frame="terminal"
dotnet add package Nacara.Plugin.Assets.Nuglify --prerelease
```

Ask for the formats you want:

```fsharp ins={4,5}
Site.create "My library"
|> Markdown.register
|> Theme.register theme
|> Nuglify.minifyHtml
|> Nuglify.minifyJs
```

On this site that takes the HTML down by 2.5% and the JavaScript by half, both measured after gzip -
the scripts are where the win is.

## One tool per format

Each of these claims one kind of file and leaves the others alone. `minifyHtml` will not touch the
CSS in a `<style>` block or the code in a `<script>`, so whatever you registered for those handles
them, once.

There is a `Nuglify.minifyCss` too. Use it *or* [`LightningCss`](lightningcss.md), never both.
Lightning CSS compiles as well as minifies, so prefer it for modern CSS.

## Is it safe?

Yes, with the defaults. Your pages read the same, your scripts still work, and anything a browser or
your markup calls by name keeps that name.

Two options can change that, so both are off:

- `RemoveOptionalTags` drops `</body>` and friends. Valid HTML, but tools that look for `</body>` to
  insert something - Nacara's dev server included - stop finding it.
- Turning off `KeepOneSpace` lets words run together where a line break used to separate them.

Turn either on and read a few pages afterwards.

## Options

```fsharp
|> Nuglify.minifyHtmlWith(
    Nuglify.htmlMinifyWhileWatching true
)
```

| HTML | Default | Effect |
|---|---|---|
| `CollapseWhitespace` | `true` | Collapse runs of whitespace between elements |
| `KeepOneSpace` | `true` | Leave one space where a run was, so nothing the reader sees moves |
| `RemoveComments` | `true` | Drop HTML comments |
| `RemoveAttributeQuotes` | `false` | Drop quotes from attribute values that do not need them |
| `RemoveOptionalTags` | `false` | Drop end tags HTML5 allows you to leave out, such as `</body>` |
| `MinifyWhileWatching` | `false` | Minify during watch builds too |

| JavaScript | Default | Effect |
|---|---|---|
| `ShortenNames` | `true` | Shorten local variable names; anything reachable from outside keeps its own |
| `KeepLicenceComments` | `true` | Keep `/*! … */` comments, where a licence lives |
| `MinifyWhileWatching` | `false` | Minify during watch builds too |

| CSS | Default | Effect |
|---|---|---|
| `KeepLicenceComments` | `true` | Keep `/*! … */` comments, where a licence lives |
| `MinifyWhileWatching` | `false` | Minify during watch builds too |

## Reference

Every function and option of it, signature by signature: [`Nuglify`](../../reference/nacara-plugin-assets-nuglify/nacara-plugins/nuglify.md).
