---
title: Theme.Core
description: What any Nacara site builds its layouts on
---

`Nacara.Theme.Core` is what any site builds its layouts on: the head, the shell, the shared
script, the default tokens and the stylesheet that draws what plugins emit.

A site that writes its own layouts uses it directly.
[Theme.Docs](../docs/index.md) and the themes are built on it.

## Add it

```bash frame="terminal"
dotnet add package Nacara.Theme.Core --prerelease
```

## A site with its own layouts

`ThemeCore.register` adds what a site that writes its own layouts needs: the code-block renderer,
the behaviours script, the default tokens and the content stylesheet.
`ThemeCore.registerWith (ThemeCore.defaultTokens false)` leaves the tokens out.

`ThemeCore.tokensStylesheet` registers the default tokens on their own.

[A site with its own look](../../../guide/custom-site.md) builds one.

## The shell

The pieces of a page every theme writes the same way.

| Function | What it renders |
|---|---|
| `Head.meta model` | Charset, viewport, title, description, canonical url, social cards, favicon |
| `Head.ofContext title description context` | The same, resolved for the page a layout renders |
| `Shell.stylesheetLinks site` | A link to every stylesheet plugins contributed |
| `Shell.scripts site` | Every script plugins contributed |
| `Shell.skipLink className target` | The link that jumps past the navigation |
| `Shell.untranslatedNotice page` | The note on a page standing in for a missing translation |
| `Shell.generatedBy page` | The attribute naming the plugin that generated the page |
| `Behaviours.bootstrapTag ()` | The inline script that settles the colour scheme before first paint |
| `Behaviours.scriptTag site` | The deferred script tag of the shared behaviours |

## Pages and scope

A `PageScope` is the page, the site, and every page of the site. `PageScope.ofContext` builds one
from the `PageContext` a layout is given.

| Function | What it returns |
|---|---|
| `Head.resolve title description favicon scope` | A `HeadModel`: the title, the description, the canonical url, the favicon. `OgType` is `website` on a home page and `article` elsewhere |
| `Head.withImage site path alt model` | The model with the image shown on social sites. `path` is relative to the site root, or an absolute url |
| `Translations.resolve site pages page` | A `TranslationModel` per locale: the page's version in it |
| `Pages.section page` | The first segment of the page's route |
| `Pages.ofSection scope` | The pages of the page's section, in front-matter order |
| `Pages.find pages path` | `PageIndex.find` of Nacara.Core |
| `Links.resolve site pages url` | `PageIndex.link` of Nacara.Core |

`Icons` holds inline SVG icons.

## Registration

`ThemeCore.configureBase` registers the code-block renderer and the behaviours script. A theme
calls it from its own plugin. A documentation theme calls
[`ThemeDocs.configure`](../docs/index.md#registration), which calls it.

`ThemeCore.contentStylesheet` registers the content styles as a stylesheet of its own, in the
`nacara.content` layer, linked by `Shell.stylesheetLinks`. Use it when the content styles are not
part of a bundle.

The code-block renderer is `NacaraCodeBlockRenderer`. Register your own `ICodeBlockRenderer` after
it to replace its markup. See [Code blocks](../../../guide/code-blocks.md).

## Reference

[`Nacara.Theme.Core`](../../../reference/nacara-theme-core/nacara-theme-core/index.md), module by module.
