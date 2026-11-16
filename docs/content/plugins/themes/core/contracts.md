---
title: Contracts
description: The stylesheet, the tokens and the attributes every theme shares
---

What Theme.Core ships that a theme includes rather than writes: the shared stylesheets, the tokens
they read, and the attributes the shared script attaches to.

## Stylesheets

`Styles.bundle name own` returns the parts of a theme's stylesheet and the path the bundle is
served from. Register the result as a `Bundle` asset. The theme's entry imports the shared parts by
name, and the site's bundler resolves them.

```css
@import "nacara-core-tokens.css";
@import "mine.css";
@import "nacara-core-content.css";
```

### `nacara-core-tokens.css`

The default tokens: colours, spacing, fonts, radii, light and dark. Import it and override what
you want, or bring tokens of your own that define the same names.

### `nacara-core-content.css`

What plugins emit, styled against the tokens. Import it after your own layout rules.

| Class | Emitted by |
|---|---|
| `nacara-heading-anchor` | The markdown plugin, on every heading |
| `nacara-callout` | `:::note`, `:::tip`, `:::warning`, `:::caution`, `:::danger` |
| `nacara-tabs`, `nacara-tab` | `:::tabs` |
| `nacara-steps` | `:::steps` |
| `nacara-details` | `:::details` |
| `nacara-file-tree` | `:::filetree` |
| `nacara-preview` | `:::preview` |
| `nacara-code` | The code-block renderer |
| `nacara-visually-hidden` | The shared script, for what it announces |

A theme that wants one of them different overrides the rule after the import.

## Tokens

`nacara-core-content.css` reads these. A theme that brings its own tokens defines every one.

| Token | Used for |
|---|---|
| `--nacara-space-1`, `-2`, `-3`, `-4`, `-6`, `-8`, `-12` | Spacing |
| `--nacara-radius`, `--nacara-radius-sm` | Corners |
| `--nacara-font-sans`, `--nacara-font-mono` | Type |
| `--nacara-bg`, `--nacara-bg-subtle`, `--nacara-border` | Surfaces |
| `--nacara-text`, `--nacara-text-muted`, `--nacara-primary` | Colour |
| `--nacara-shadow` | What floats |
| `--nacara-note`, `--nacara-tip`, `--nacara-warning`, `--nacara-danger` | Callouts and badges |
| `--tok-*` | Syntax highlighting |

Plugins with a widget of their own read `--nacara-control-height`, `--nacara-control-radius`,
`--nacara-bg-raised` and `--nacara-primary-subtle` too.

## Behaviours

`nacara-behaviours.js` is one script, shipped by `ThemeCore.configureBase`. It reads nothing but
`data-nacara-*` attributes. A theme opts into a behaviour by writing the attribute and out of it
by leaving it off. The menu's attributes are read by
[the menu script](../docs/index.md#the-menu-script) of Theme.Docs.

| Attribute | On | Behaviour |
|---|---|---|
| `data-nacara-theme` | A `<select>` with `light`, `dark` and `system` | The colour scheme, remembered across visits |
| `data-nacara-navbar` | The navbar | Its height offsets the table of contents' scroll spy |
| `data-nacara-dropdown-root` | An element holding a dropdown | Opens on hover and on click, `data-open` and `data-pinned` say which |
| `data-nacara-dropdown` | The dropdown's button | Toggles it, and carries `aria-expanded` |
| `data-nacara-sidebar` | The sidebar | The drawer on a narrow screen, `data-open` says whether it shows |
| `data-nacara-menu-toggle` | A button | Opens and closes the drawer |
| `data-nacara-widgets-end` | The navbar's end `<ul>` | Its items move to the shelf on a narrow screen |
| `data-nacara-widgets-shelf` | A hidden `<ul>` in the sidebar | Where they go |
| `data-nacara-toc-link` | A link of the table of contents | `data-active` follows the reader's scroll |
| `data-nacara-code`, `data-nacara-code-body`, `data-nacara-code-copy` | The code frame, its body, its button | Copying a block |

The colour scheme sets `data-theme` on `<html>` to `light` or `dark`, and `data-theme-setting` to
what was picked. `Behaviours.bootstrapTag` sets both before the first paint.

The narrow breakpoint is read from `--nacara-narrow` on `:root`, `860px` when it is not set. Set
it beside the media query that folds your navbar.

`<nacara-tabs>` and `<nacara-tab>` are defined by the script, as the markdown plugin emits them.

The search plugin's trigger carries `data-nacara-search`, which the shelf leaves where it is.
