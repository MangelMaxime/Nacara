---
title: Contracts
description: The stylesheet, the tokens and the attributes every theme shares
---

Theme.Core ships two stylesheets, a set of tokens and a script that every theme can use. This page
lists the files, the tokens and the attributes they read.

## Stylesheets

A theme's stylesheet is a list of layers. Each layer is written into the cascade layer
`nacara.<name>`, and a later layer wins over an earlier one. The stylesheets of the plugins are in
the `nacara.plugins` layer, after every layer of the theme. A rule outside any layer, such as the
site's own stylesheet, wins over all of them.

`Styles.layer name css` creates a layer. `name` uses lowercase letters, digits and hyphens.
`plugins` is taken.

`Styles.bundle` takes the name of the bundle and the layers. It returns:

- the `Bundle` asset: one file per layer, and an entry file that orders the layers and imports
  them;
- the path of the bundle, `assets/css/<name>.<hash>.css`. The hash changes when a layer changes.

Register the asset, and link the path from the page's `<head>`:

```fsharp
let stylesheet, cssPath =
    Styles.bundle
        "mine"
        [
            Styles.tokens.Value
            Styles.layer "layout" layoutCss
            Styles.content.Value
        ]

registry |> Registry.asset stylesheet

Html.link [ prop.rel "stylesheet"; prop.href (context.Site.UrlOfAsset cssPath) ]
```

When the site uses a CSS bundler, such as [LightningCSS](../../assets/lightningcss.md), the bundle is
written as one file. Without one, the entry file is written at the path of the bundle, and the
other files next to it.

A plugin writes its stylesheet into a layer of its own under `nacara.plugins`:

```css
@layer nacara.plugins.search {
    .nacara-search__trigger {
        display: inline-flex;
    }
}
```

### `Styles.tokens`

The `tokens` layer. The default [tokens](#tokens): colours, spacing, fonts, radii, for the light
and the dark scheme. To change a token, set it again in a later layer or outside the layers.
To replace them all, leave this layer out and define the same names in a layer of your own.

### `Styles.content`

The `content` layer. The styles of what the plugins write into a page. Put it after your own
layout layers. To change one of them, write the rule again in a later layer or outside the layers.

| Selector | Written by |
|---|---|
| `.nacara-heading-anchor` | The markdown plugin, on every heading |
| `.nacara-callout` | `:::note`, `:::info`, `:::tip`, `:::warning`, `:::caution`, `:::danger` |
| `nacara-tabs`, `nacara-tab` | `:::tabs` |
| `.nacara-steps` | `:::steps` |
| `.nacara-details` | `:::details` |
| `.nacara-file-tree` | `:::filetree` |
| `.nacara-preview` | `:::preview` |
| `.nacara-code` | The code-block renderer |
| `.nacara-visually-hidden` | The shared script, for the text it announces to screen readers |

## Tokens

The `content` layer reads these tokens. A theme without the `tokens` layer must define all of
them.

| Token | Used for |
|---|---|
| `--nacara-space-1`, `-2`, `-3`, `-4`, `-6` | Spacing |
| `--nacara-radius`, `--nacara-radius-sm` | Corners |
| `--nacara-font-sans`, `--nacara-font-mono` | Fonts |
| `--nacara-bg`, `--nacara-bg-subtle`, `--nacara-border` | Backgrounds and borders |
| `--nacara-text`, `--nacara-text-muted`, `--nacara-primary` | Text and links |
| `--nacara-shadow` | Shadows |
| `--nacara-note`, `--nacara-tip`, `--nacara-warning`, `--nacara-danger` | Callouts |
| `--tok-*` | Syntax highlighting |

`--nacara-code-max-height` is optional. It sets the height above which a code block scrolls. When
it is not set, a code block grows with its code.

The plugins that add a control to the page, such as search and versions, also read
`--nacara-control-height`, `--nacara-control-radius`, `--nacara-bg-raised` and
`--nacara-primary-subtle`.

## Behaviours

`ThemeCore.configureBase` registers one script, `nacara-behaviours.js`. The script reads only
`data-nacara-*` attributes. To use a behaviour, write its attribute in your markup. To leave it out,
do not write the attribute.

| Attribute | On | Behaviour |
|---|---|---|
| `data-nacara-theme` | A `<select>` with the options `light`, `dark` and `system` | Sets the colour scheme, and keeps it for the next visit |
| `data-nacara-theme-pick="light"`, `"dark"` or `"system"` | A button | Sets that colour scheme. `aria-pressed` is `true` on the button of the current scheme |
| `data-nacara-navbar` | The navbar | When the navbar is `sticky` or `fixed`, the table of contents adds its height to the scroll offset |
| `data-nacara-dropdown-root` | The element that holds a dropdown | Opens the dropdown on hover and on click. `data-open` is `true` while it is open, `data-pinned` is `true` after a click |
| `data-nacara-dropdown` | The button of the dropdown | Opens and closes the dropdown, and sets `aria-expanded` |
| `data-nacara-sidebar` | The sidebar | Shows the sidebar as a drawer on a narrow screen. `data-open` is `true` while it shows |
| `data-nacara-menu-toggle` | A button | Opens and closes the drawer |
| `data-nacara-widgets-end` | The `<ul>` at the end of the navbar | Moves its items to the shelf on a narrow screen |
| `data-nacara-widgets-shelf` | A hidden `<ul>` in the sidebar | Receives the items of `data-nacara-widgets-end` |
| `data-nacara-toc-link` | A link of the table of contents | Sets `data-active="true"` on the link of the section being read |
| `data-nacara-code`, `data-nacara-code-body`, `data-nacara-code-copy` | A code frame, its body and its copy button | Copies the code of the block |

The script also defines the `<nacara-tabs>` and `<nacara-tab>` elements that the markdown plugin
writes for `:::tabs`.

The menu attributes, `data-nacara-menu-*`, are read by [the menu script](../docs/index.md#the-menu-script)
of Theme.Docs.

### Colour scheme

The script sets two attributes on `<html>`:

- `data-theme`: `light` or `dark`, the scheme in use;
- `data-theme-setting`: `light`, `dark` or `system`, the scheme the reader picked.

`Behaviours.bootstrapTag` sets both before the first paint. Put it in the `<head>`.

### Narrow screen

The script reads the narrow breakpoint from `--nacara-narrow` on `:root`. When it is not set, the
breakpoint is `860px`. Set it to the same width as the media query that changes your navbar.

The shelf does not move an item that holds the trigger of the search plugin,
`data-nacara-search`.
