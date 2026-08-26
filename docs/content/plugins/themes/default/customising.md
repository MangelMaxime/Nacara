---
title: Customising
---

Colours, spacing and fonts are custom properties. Override the ones you care about in a stylesheet
of your own - you have no build step to run and no configuration file to write.

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

The path is relative to your project root, and one that names nothing fails the build rather than
leaving you to wonder where your colours went. What you get is a stylesheet treated like any other
the build handles: bundled, minified, fingerprinted, and linked on every page after the theme's
own, so your rules win without having to out-specify anything.

Every stylesheet sitting beside it goes to the bundler too, so `custom.css` can be a table of
contents for the rest and still arrive as one file:

```css title="css/custom.css"
@import "colours.css";
@import "navbar.css";
```

## Your own script

The same, at the end of every page and with `defer`:

```fsharp
|> Site.script "js/custom.js"
```

Written as several files that `import` each other, it needs
[esbuild](../../assets/esbuild.md) registered. A stylesheet does not.

## The other ways

`Theme.headExtra` puts anything you like into every page's `<head>` - a font, a favicon variant, an
analytics snippet, a stylesheet you host somewhere else:

```fsharp
Theme.defaults
|> Theme.headExtra [ Html.link [ prop.rel "stylesheet"; prop.href "https://example.com/font.css" ] ]
```

And a file in the static directory is copied out untouched, bundled by nothing and fingerprinted by
nothing - which is what you want for something whose URL has to stay exactly as you wrote it, and
not what you want for a stylesheet.

For a rule or two, skip the file altogether:

```fsharp
Theme.defaults
|> Theme.css """[data-section="reference"] { --nacara-sidebar-width: 20rem; }"""
```

It lands after everything above, so your rule wins without having to out-specify anything, and the
theme stamps the section on `<body>` - so `[data-section="…"]` is how you treat one part of a site
differently. Call it more than once and the rules add up.

## The tokens

| Token | What it decides |
|---|---|
| `--nacara-primary`, `--nacara-primary-contrast`, `--nacara-primary-subtle` | Links, the active menu entry, focus rings |
| `--nacara-bg`, `--nacara-bg-subtle`, `--nacara-bg-raised` | The page, code blocks and tables, dialogs |
| `--nacara-text`, `--nacara-text-muted` | Body text, and everything secondary |
| `--nacara-border` | Every rule and outline |
| `--nacara-content-width` | The measure - `75ch`, and `88ch` on a screen wider than 1600px |
| `--nacara-sidebar-width`, `--nacara-toc-width`, `--nacara-navbar-height`, `--nacara-layout-gap` | The frame, and the gutters between its columns |
| `--nacara-space-1` … `--nacara-space-12` | The spacing scale everything is built from |
| `--nacara-radius`, `--nacara-radius-sm`, `--nacara-control-height`, `--nacara-control-radius` | Corners, and the height controls share |
| `--nacara-font-sans`, `--nacara-font-mono` | The two families |
| `--nacara-note`, `--nacara-tip`, `--nacara-warning`, `--nacara-danger` | Callouts |
| `--tok-keyword`, `--tok-string`, `--tok-type`, … | [Code colours](../../highlight/index.md#colours) - Atom One Light, and One Dark Pro in the dark |

Redefine a token under `:root[data-theme="dark"]` to change it in dark mode only, or under
`[data-section="…"]` to change it in one section only. The theme puts the section on the `<body>`,
so a reference full of long names can have a wider sidebar than the rest of your site:

```css
[data-section="reference"] {
  --nacara-sidebar-width: 21rem;
}
```

The scheme is applied before first paint, so a reader who chose dark never sees a white flash.

## Controls

Everything that sits in the navbar - the search box, the version switcher, a widget of your own -
uses `--nacara-control-height` and `--nacara-control-radius`, so they line up without you measuring
anything. Use them too when your plugin adds a control.

## When tuning is not enough

Your stylesheet is loaded after the theme's, so anything you write wins without `!important`. That
covers colours, spacing and type.

When you want different markup rather than different colours, compose the theme's own pieces -
[Components](components.md) builds a layout out of them.
