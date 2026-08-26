---
title: esbuild
---

Resolves what your JavaScript imports into one file, and minifies it, with
[esbuild](https://esbuild.github.io).

Two things, and the first is the one that matters: a script you register with
[`Site.script`](../themes/default/customising.md#your-own-script) may be written as several
files that `import` each other, and it arrives at a reader as one. The second is that it
minifies what it produces, and understands syntax an older minifier does not.

## Add it

```bash frame=terminal
dotnet add package Nacara.Plugin.Assets.Esbuild --prerelease
```

```fsharp ins={4}
Site.create "My library"
|> Markdown.register
|> Theme.register theme
|> Esbuild.register
```

## It replaces Nuglify for JavaScript

[`Nuglify`](nuglify.md) minifies JavaScript too. Register one or the other, never both - they both
run, each parsing the other's output, and the build says so:

```text frame="terminal"
! warning nacara/duplicate-asset-transform: Several transforms claim '.js': nuglify-js, esbuild
    hint: They all run, one after another. Register the one you want.
```

This one is the better choice whenever your JavaScript is modern.

Keep [`Nuglify`](nuglify.md) registered for your HTML either way: esbuild does not do HTML.

## Options

| Option | Default | Effect |
|---|---|---|
| `BinaryPath` | `None` | Use your own esbuild instead of the pinned release |
| `MinifyWhileWatching` | `false` | Minify during watch builds too |

## Reference

Every function and option of it, signature by signature: [`Esbuild`](../../reference/nacara-plugin-assets-esbuild/nacara-plugins/esbuild.md).
