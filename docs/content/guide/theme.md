---
title: Theming
---

A theme is a package like any other: it contributes layouts and assets. The engine has no opinion
about what a page looks like, and no theme gets special treatment.

## How a page gets its look

A collection says which function renders its pages:

```fsharp
Collection.create "docs" decoder
|> Collection.layout (fun context -> (* markup *) )
```

That function receives a `PageContext<'FrontMatter>` - the page, its decoded front matter, the site,
every other page, and the rendered content - and returns markup. Everything else a theme does
(stylesheets, scripts, web components) it contributes as a plugin:

```fsharp
registry
|> Registry.asset (WriteText(css, RelativePath.create "assets/theme.css"))
|> Registry.extra (Stylesheet "assets/theme.css")
```

So "using a theme" is two lines: register the package, and use its layout.

```fsharp
Site.create "My library"
|> Theme.register theme                        // its assets
|> Site.collection (Theme.docs theme "content") // its layout and front matter
```

## The default theme

`Nacara.Theme.Default` is the one this site uses, and what `dotnet new nacara-docs` starts you with:
navbar, sidebar, table of contents, dark mode, and the components for tabs, callouts and code
frames. Everything about it - the tokens you override, the navbar and menu options, the front matter
it reads, the pieces you can compose - is documented with the other packages:

- [Default theme](../plugins/themes/default/index.md) - what you get, and how to add it
- [Customising](../plugins/themes/default/customising.md) - colours, spacing, fonts, your own CSS
- [Navbar](../plugins/themes/default/navbar.md) - sections, dropdowns, and the frame
- [Menu](../plugins/themes/default/menu.md) - where a sidebar comes from, and writing your own
- [Front matter](../plugins/themes/default/front-matter.md) - what a page says about itself
- [Components](../plugins/themes/default/components.md) - composing a layout of your own

## Three ways to make it yours

**Override the tokens.** Colours, spacing and the measure are custom properties, and your own
stylesheet is loaded after the theme's, so yours wins.

**Compose the pieces.** The navbar, sidebar, table of contents and page navigation are exported
functions. Your own layout can use them and arrange the middle differently - a landing page, a
reference page with two columns.

**Write a theme.** A layout function, a stylesheet, and `register`; nothing else is required. If you
publish it, it is an ordinary NuGet package other sites can use, exactly like the default one.

## What the engine owns

However far you go, some things stay with the engine rather than the theme, and are the same
whichever theme you use:

| The engine decides | The theme decides |
|---|---|
| Which pages exist, and their routes | What a page looks like |
| That front matter is typed, and fails the build when it is not | Which fields it reads |
| What a URL is - base path, locale, version prefix | Which links a page shows |
| Which locale a page belongs to, and which one stands in for it | How that is said to a reader |
| What a code block contains - its title, marked lines, folded ranges | The markup that draws it |
| That a page carries its headings, whichever plugin read them out of it | Whether a table of contents is shown, and where |
| That an asset is written once, and only when its bytes changed | Which stylesheets and scripts a page carries |

That division is why swapping themes does not change your content, and why a plugin can ship a
widget - a search box, a version switcher - without knowing which theme will draw it.
