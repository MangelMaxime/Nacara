---
title: Customising
toc:
  to: 3
---

The theme is built on CSS custom properties, so most changes are a value rather than a rule. Your
own stylesheet is linked after the theme's, so anything you do write wins without `!important`.

## Your own stylesheet

Write it wherever you keep it and name it on the site:

```css title="css/custom.css"
:root {
  --nacara-primary: #7c3aed;
  --nacara-content-width: 80ch;
}

:root[data-theme="dark"] {
  --nacara-primary: #a78bfa;
}
```

```fsharp ins={3}
Site.create "My library"
|> Theme.register theme
|> Site.stylesheet "css/custom.css"
```

The path is relative to your project root, and a path that matches no file fails the build. The
stylesheet is bundled, minified and fingerprinted like every other asset.

Every stylesheet beside it is given to the bundler too, so `custom.css` can `@import` its neighbours
and still ship as one file:

```css title="css/custom.css"
@import "colours.css";
@import "navbar.css";
```

For a rule or two, use `Theme.css` instead of a file. It is written into every page's `<head>` after
every stylesheet, so it wins over the theme's and over yours:

```fsharp
Theme.defaults
|> Theme.css """[data-section="reference"] { --nacara-sidebar-width: 20rem; }"""
```

Call it more than once and the rules add up.

## The tokens

Colours, spacing, radii, fonts, the widths of the frame and the colours code is painted in are all
custom properties.
[`tokens.css`](https://github.com/MangelMaxime/Nacara/blob/main/src/Nacara.Theme.Default/assets/css/tokens.css)
declares every one of them, for both colour schemes. They cover:

| | |
|---|---|
| Colour | The primary and its pair, the backgrounds, text, headings, borders, inline code, and the four callout hues |
| Layout | The measure, the splash measure, the sidebar and table of contents, the navbar's height, the gutters |
| Spacing | `--nacara-space-1` … `--nacara-space-12`, which everything else is built from |
| Type | The sans and mono families |
| Controls | The height and radius shared by everything in the navbar |
| Code | `--tok-*`, one per token kind - see [Code colours](../../highlight/index.md#colours) |

Redefine the ones you need; the rest keep the theme's values.

### Light and dark

Redefine a token under `:root` to change both schemes, or under `:root[data-theme="dark"]` for the
dark one only:

```css
:root {
  --nacara-primary: #7c3aed;
}

:root[data-theme="dark"] {
  --nacara-primary: #a78bfa;
}
```

The scheme is applied before first paint, so there is no flash of the wrong one.

### One section only

The theme sets `data-section` on `<body>`, so `[data-section="…"]` styles a single section:

```css
[data-section="reference"] {
  --nacara-sidebar-width: 21rem;
}
```

The section is the first segment of a route: `guide/getting-started.md` is in `guide`.

### Surfaces

`.nacara-navbar`, `.nacara-sidebar` and `.nacara-footer` read the same tokens as the page.
Restating them there gives that part its own colours:

```css title="css/custom.css"
.nacara-navbar {
  --nacara-bg: #2d3947;
  --nacara-bg-subtle: #354353;
  --nacara-bg-raised: #354353;
  --nacara-border: #46566a;
  --nacara-text: #ffffff;
  --nacara-text-muted: rgb(255 255 255 / 78%);
  --nacara-primary: #ffffff;
  --nacara-primary-subtle: #46566a;
}
```

Every component reads these tokens, including the controls a plugin owns such as the search box.

The navbar is painted at `--nacara-navbar-opacity`, `85%`, over `--nacara-navbar-blur`, `8px`, of
backdrop blur. Set the opacity to `100%` for a solid bar.

### Controls

The search box, the version switcher and any widget of your own use `--nacara-control-height` and
`--nacara-control-radius`. Use them when your plugin adds a control and it lines up with the rest.

## Your own script

`Site.script` does the same for JavaScript, loaded at the end of every page with `defer`:

```fsharp
|> Site.script "js/custom.js"
```

A script split across files that `import` each other needs [esbuild](../../assets/esbuild.md)
registered.

## Anything else in the head

`Theme.headExtra` adds markup to every page's `<head>` - a font, a favicon variant, an analytics
snippet, a stylesheet you host elsewhere:

```fsharp
Theme.defaults
|> Theme.headExtra [ Html.link [ prop.rel "stylesheet"; prop.href "https://example.com/font.css" ] ]
```

A file in the static directory is copied out untouched, with no bundling and no fingerprint, so its
URL stays exactly as you wrote it.

## Changing the markup

Tokens and your own stylesheet cover colours, spacing and type. For different markup, compose the
theme's pieces yourself - see [Components](components.md).
