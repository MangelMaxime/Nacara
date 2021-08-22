---
title: Configuration
layout: nacara-standard
---

[[toc]]

::: warning my super title
We are working on improving the documentation in order to make it easier to read.
:::

::: warning
We are working on improving the documentation in order to make it easier to read.
:::

::: info
We are working on improving the documentation in order to make it easier to read.
:::

:::primary
We are working on improving the documentation in order to make it easier to read.
:::
::: danger
We are working on improving the documentation in order to make it easier to read.
:::
::: success
We are working on improving the documentation in order to make it easier to read.
:::
You can use the file `nacara.json` in order to configure most of Nacara behaviour.

## Mandatory fields

### `url` - [string]

This is the protocol and host part of your website URL.

For the website `https://mangelmaxime.github.io/Nacara/`, it is `https://mangelmaxime.github.io`

### `baseUrl` - [string]

This is the path after the host part of your website URL.

For the website `https://mangelmaxime.github.io/Nacara/`, it is `/Nacara/`. If you have no path you can set it to `/`.

### `version` - [string]

This is the version of the website. It's used in order to show the version in the navbar if `navbar.showVersion` is `true`. It will also be used in order to handle versionning of the documentation.

### `title` - [string]

Title for your website.

It's shown in the navbar and in the `title` tag of your page.

## Optional fields

### `source` - [string]

Relative path to the folder used as source.

The default value is `docsrc`.

### `output` - [string]

Relative path to the output folder.

The default value is `docs`.

### `changelog` - [string]

Relative path to the file used as a changelog. If you set this value, the changelog will be parsed and you will be able to accessible at the root your website.

### `navbar` - [object]

This property allow you to control the navbar component of your website.

* `showVersion` - [bool]

    If `true`, the `version` from the config file will be shown in the navbar.

    Later, this "navbar item" will be clickable in order to choose another version of the documentation.

* `doc` - [string]

    If set then a link `Documentation` will be added to the navbar. The value provided is the id of the page to link to.

* `links` - [list]

    * Required
        * `href` - [string]
    * Optional
        * `label` - [string]

            If provided this will be displayed as text in the link
        * `icon` - [string]

            If provided this will be displayed as an icon in the link. It needs to be a FontAwesome5 icon class. Ex: `fab fa-twitter`
        * `color` - [string]

            If provided it will be apply to the CSS property `color` for the link.
        * `isExternal` - [string]

            If `true`, then clicking on the link will open a new tab in the browser

### `menu` - [object]

This property is used to configure the menu.

Every property `"page-id"` means you need to specify the `id` of the page you want to link to. The title of the page will be used as the label for the menu item.

```json
{
    //...
    "menu": {
        "Category 1": [
            "page-id",
            "page-id"
        ],
        "Category 2": [
            "page-id",
        ],
        "Category 3": [
            "page-id",
            {
                "Sub category 3-1": [
                    "page-id"
                ],
                "Sub category 3-2": [
                    "page-id"
                ]
            }
        ]
    }
    //...
}
```

### `lightner` - [object]

This property is used to configure [Code-Lightner](https://github.com/MangelMaxime/Code-Lightner).

Code-Lightner is a project which use TextMate grammar in order to highlight the code. This is how highlights is done in VSCode for example so you can have the same result in your website.

* `backgroundColor` - [string]

    Set the background color of the pre element

* `textColor` - [string]

    Set the color of the text

* `themeFile` - [string]

    Relative path to the theme file

* `grammars` - [list]

    List of the grammar file to load
