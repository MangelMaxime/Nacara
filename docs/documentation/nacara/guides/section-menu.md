---
title: Section menu
layout: nacara-standard
---

A menu is specific to a [section](/Nacara/documentation/nacara/guides/create-a-section.html).

The menu is optional but it helps you:

* Group your content
* Display a menu when visiting a page on the related section
* Optionally: Provide navigation buttons

## Define a menu

The menu of a section is defined by creating a `menu.json` file at the top level of your section.

```
docsrc
├── blog
└── documentation
    └── menu.json
```

Here you have **2** sections but only `documentation` has a menu.

## `menu.json`

The `menu.json` consist in a list of `MenuItem` which can be a `string` or an `object`.

```fsharp
type MenuItem =
    | Page of MenuItemPage
    | List of MenuItemList
    | Link of MenuItemLink

type Menu = MenuItem list
```

## Mastering your menu.json

Your `menu.json` supports different items:

- [Page](#Page:-Link-to-a-page): link to a page hosted on your site
- [Link](#Link:-link-to-any-page): link to an arbitrary URL
- [Section](#Section:-organize-your-menu): group several `MenuItem`

### Page: Link to a page

**Page**, allows you to link to a page from your `source` folder.

You need to provide the relative path from the `source` folder.

**Example:**

```
docsrc
├── blog
└── documentation
    └── page1.md
    └── page2.md
    └── menu.json
```

File: `docs/documentation/menu.json`

```json
[
    "documentation/page1",
    "documentation/page2",
]
```

The `title` front-matter will be used has the text to display in the menu.

### Link: link to any page

**Link**, allows you to link to any page (internal or external) to your website.

**Properties**

<table class="table is-narrow is-bordered">
    <thead>
        <tr>
            <th class="has-text-centered">Name</th>
            <th class="has-text-centered">Required</th>
            <th class="has-text-centered">Description</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td class="has-text-centered" style="vertical-align: middle"><code>type</code></td>
            <td class="has-text-centered" style="vertical-align: middle">X</td>
            <td>Always set to <code>link</code></td>
        </tr>
        <tr>
            <td class="has-text-centered" style="vertical-align: middle"><code>label</code></td>
            <td class="has-text-centered" style="vertical-align: middle">X</td>
            <td>Text to display in the menu</td>
        </tr>
        <tr>
            <td class="has-text-centered" style="vertical-align: middle"><code>href</code></td>
            <td class="has-text-centered" style="vertical-align: middle">X</td>
            <td>URL to link to</td>
        </tr>
    </tbody>
</table>

**Example:**

```json
[
    {
        "type": "link",
        "label": "Fable",
        "href": "http://fable.io"
    }
]
```

### Section: organize your menu


**Section**, allows you to organize your menu in different sections.

**Properties**

<table class="table is-narrow is-bordered">
    <thead>
        <tr>
            <th class="has-text-centered">Name</th>
            <th class="has-text-centered">Required</th>
            <th class="has-text-centered">Description</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td class="has-text-centered" style="vertical-align: middle"><code>type</code></td>
            <td class="has-text-centered" style="vertical-align: middle">X</td>
            <td>Always set to <code>section</code></td>
        </tr>
        <tr>
            <td class="has-text-centered" style="vertical-align: middle"><code>label</code></td>
            <td class="has-text-centered" style="vertical-align: middle">X</td>
            <td>Text to display in the menu</td>
        </tr>
        <tr>
            <td class="has-text-centered" style="vertical-align: middle"><code>items</code></td>
            <td class="has-text-centered" style="vertical-align: middle">X</td>
            <td>List of <a href="#menu.json">MenuItems</a></td>
        </tr>
        <tr>
            <td class="has-text-centered" style="vertical-align: middle"><code>collapsible</code></td>
            <td class="has-text-centered" style="vertical-align: middle"></td>
            <td>
                True, if the section can be collapse
                <br/>
                Default: <code>true</code>
            </td>
        </tr>
        <tr>
            <td class="has-text-centered" style="vertical-align: middle"><code>collapsed</code></td>
            <td class="has-text-centered" style="vertical-align: middle"></td>
            <td>
                True, if the section is collapse by default
                <br/>
                Default: <code>false</code>
            </td>
        </tr>
    </tbody>
</table>

**Example:**

```json
[
    "documentation/page-1",
    {
        "type": "section",
        "label": "Guides",
        "items": [
            "documentation/guides/guide-1",
            {
                "type": "section",
                "label": "Advanced",
                "collapsible": true,
                "collapsed": true,
                "items": [
                    "documentation/guides/advanced/guide-1"
                ]
            }
        ]
    }
]
```
