---
title: Lightning CSS
---

Compiles the CSS your site ships for the browsers you name, and minifies it, with
[Lightning CSS](https://lightningcss.dev).

Two things, and the second is the one that matters: your stylesheets come out about a quarter
smaller, and you get to write modern CSS without worrying which browsers understand it yet. That is
how the theme is written, with no build step of its own.

## Add it

```bash frame=terminal
dotnet add package Nacara.Plugin.Assets.LightningCss --prerelease
```

```fsharp ins={4}
Site.create "My library"
|> Markdown.register
|> Theme.register theme
|> LightningCss.register
```

The first build fetches the pinned Lightning CSS release into `~/.cache/nacara`. On this site it
takes the stylesheet from 22.5 KB to 17.7 KB, and a browser sees less again over gzip.

Every stylesheet the build writes goes through it: the theme's, the plugins', and your own static
CSS.

It has a sibling: [`Nuglify`](nuglify.md) takes the HTML and the JavaScript, and leaves `<style>`
blocks exactly as it found them. Each format is handled once, by a tool that understands it.

`Nuglify` can minify CSS too. Register one or the other, never both - two minifiers on one
stylesheet is two sets of rules deciding what it means. This one is the better choice whenever your
CSS is modern, because it compiles as well as minifies.

## Browsers to compile for

Name the browsers you support, in browserslist syntax, and Lightning CSS makes the CSS work for
them:

```fsharp
|> LightningCss.registerWith (fun options -> { options with Targets = "> 0.5%, last 2 versions" })
```

Write it here rather than in a `.browserslistrc` - a file in your repository is ignored.

| Option | Default | Effect |
|---|---|---|
| `Targets` | `"defaults"` | Browsers to compile for, in browserslist syntax |
| `BinaryPath` | `None` | Use your own lightningcss instead of the pinned release |
| `MinifyWhileWatching` | `false` | Minify during watch builds too |

## Reference

Every function and option of it, signature by signature: [`LightningCss`](../../reference/nacara-plugin-assets-lightningcss/nacara-plugins/lightningcss.md).
