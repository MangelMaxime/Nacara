---
title: Contracts
description: The stylesheet, the tokens and the attributes every theme shares
---

Theme.Core ships two stylesheets, a set of tokens and a script that every theme can use. This page
lists the files, the tokens and the attributes they read.

## Stylesheets

A theme's stylesheet is an entry file that imports the two shared stylesheets and the theme's own
files:

```css
/* mine.css */
@import "nacara-core-tokens.css";
@import "layout.css";
@import "nacara-core-content.css";
```

`Styles.bundle` takes the name of the bundle and the theme's own files, as `(file name, content)`
pairs. It returns:

- the files to bundle: the two shared stylesheets and the theme's own files;
- the path of the bundle, `assets/css/<name>.<hash>.css`. The hash changes when a file changes.

Register them as a `Bundle` asset, with the name of the entry file, and link the path from the
page's `<head>`:

```fsharp
let parts, cssPath = Styles.bundle "mine" [ "mine.css", mineCss; "layout.css", layoutCss ]

registry |> Registry.asset (Bundle(parts, "mine.css", RelativePath.create cssPath))

Html.link [ prop.rel "stylesheet"; prop.href (context.Site.UrlOfAsset cssPath) ]
```

When the site uses a CSS bundler, such as [LightningCSS](../../assets/lightningcss.md), the bundle is
written as one file. Without one, the entry file is written at the path of the bundle, and the
other files next to it.

### `nacara-core-tokens.css`

The default [tokens](#tokens): colours, spacing, fonts, radii, for the light and the dark scheme.
To change a token, set it again after this import. To replace them all, define the same names in
a stylesheet of your own instead.

### `nacara-core-content.css`

The styles of what the plugins write into a page. Import it after your own layout rules. To change
one of them, write the rule again after this import.

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

`nacara-core-content.css` reads these tokens. A theme that does not import
`nacara-core-tokens.css` must define all of them.

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
