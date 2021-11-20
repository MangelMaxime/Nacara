---
title: Configuration
layout: standard
toc:
    to: 3
---

The file `nacara.config.json` defines your site's metadata, navbar and other general configuration. This file is located at the root of your Nacara site.

## Initial set up

If you used `npm init nacara` to create your site, there should already be a basic configuration file.

It not here is a minimal configuration file:

```json
{
    "url": "https://mangelmaxime.github.io",
    "baseUrl": "/Nacara/",
    "editUrl" : "https://github.com/MangelMaxime/Nacara/edit/master/docsrc",
    "title": "Nacara",
    "serverPort": 8081,
    "navbar": {
        "start": [
            {
                "section": "documentation",
                "url": "/Nacara/documentation/index.html",
                "label": "Documentation",
                "pinned": true
            }
        ],
        "end": [
            {
                "url": "https://github.com/MangelMaxime/Nacara",
                "icon": "fab fa-github",
                "label": "GitHub"
            }
        ]
    },
    "layouts": [
        "nacara-layout-standard"
    ]
}
```

## Configuration options

Here is a list of the main category of option your can set within your `nacara.config.json` file:

1. [siteMetadata](#sitemetadata) (object)
1. [navbar](#navbar) (object)
1. [remarkPlugins](#remarkplugins) (array)
1. [rehypePlugins](#rehypeplugins) (array)
1. [layouts](#layouts) (array)
1. [serverPort](#serverport) (int)
1. [source](#source) (string)
1. [output](#output) (string)

### siteMetadata

The `siteMetadata` contains configuration common data related to you site like (for example, your site title, favIcon, etc).

<table class="table is-narrow is-bordered is-vcentered">
    <thead>
        <tr>
            <th class="label-cell">Property</th>
            <th class="label-cell">Required</th>
            <th class="label-cell">Description</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td class="label-cell">
                <code>title</code>
            </td>
            <td class="label-cell">X</td>
            <td class="fullwidth-cell">
                Title of your website
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>url</code>
            </td>
            <td class="label-cell">X</td>
            <td class="fullwidth-cell">

URL for your website. This is the domain part of your URL.

For example, `https://mangelmaxime.github.io` is the URL of `https://mangelmaxime.github.io/Nacara/`
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>baseUrl</code>
            </td>
            <td class="label-cell">X</td>
            <td class="fullwidth-cell">
Base URL for your site. This is the path after the domain.

For example, `/Nacara/` is the baseUrl of `https://mangelmaxime.github.io/Nacara/`.

For URLs that have no path, the baseUrl should be set to `/`.
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>favIcon</code>
            </td>
            <td class="label-cell"></td>
            <td class="fullwidth-cell">
Path to your site favIcon

If this field is omitted, no `favIcon` tag will be added to your website and the browser will use it's default behavior.
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>editUrl</code>
            </td>
            <td class="label-cell"></td>
            <td class="fullwidth-cell">
URL for editing your documentation.

Example: `editUrl + 'docs/introduction.md'`. If this field is omitted, there will be no "Edit" button generated.
            </td>
        </tr>
    </tbody>
</table>

**Example**

```json
{
    "siteMetadata": {
        "title": "Nacara",
        "url": "https://mangelmaxime.github.io",
        "baseUrl": "/Nacara/",
        "editUrl" : "https://github.com/MangelMaxime/Nacara/edit/master/docsrc",
        "favIcon" : "/static/favIcon.ico"
    }
}
```

### navbar

All configuration related to the navbar goes here.

The navbar is split in 2 sections:

- start: the **left part** of the navbar which appears next to your site title and can contains **textual links** or **dropdowns**
- end: the **right part** of the navbar which appears at the end of it

#### start

The `start` of the navbar consist of a list of objects of 2 types:

- `LabelLink`: Render a link consisting in just a label
- `Dropdown`: Render a dropdown, where you place a list of link
<!-- or fully customize its content -->

##### LabelLink

<table class="table is-narrow is-bordered is-vcentered">
    <thead>
        <tr>
            <th class="label-cell">Property</th>
            <th class="label-cell">Required</th>
            <th class="label-cell">Description</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td class="label-cell">
                <code>section</code>
            </td>
            <td class="label-cell">X</td>
            <td class="fullwidth-cell">

Section of the website associated to this navbar item

*The default layout will highlight the navbar item if the visited page is inside of the associated section*
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>label</code>
            </td>
            <td class="label-cell">X</td>
            <td class="fullwidth-cell">
                Text to display
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>url</code>
            </td>
            <td class="label-cell">X</td>
            <td class="fullwidth-cell">
                URL to redirect to
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>pinned</code>
            </td>
            <td class="label-cell"></td>
            <td class="fullwidth-cell">
If `true`, the link will be displayed on mobile display too

Default: `false`
            </td>
        </tr>
    </tbody>
</table>

**Example**

```json
{
    "navbar": {
    "start": [
        {
            "section": "documentation",
            "url": "/Nacara/documentation/index.html",
            "label": "Docs",
            "pinned": true
        },
        {
            "section": "showcase",
            "url": "/Nacara/showcase/index.html",
            "label": "Showcase"
        }
    ]
}
```

##### Dropdown

<table class="table is-narrow is-bordered is-vcentered">
    <thead>
        <tr>
            <th class="label-cell">Property</th>
            <th class="label-cell">Required</th>
            <th class="label-cell">Description</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td class="label-cell">
                <code>label</code>
            </td>
            <td class="label-cell">X</td>
            <td class="fullwidth-cell">
                Text to display
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>items</code>
            </td>
            <td class="label-cell">X</td>
            <td class="fullwidth-cell">
List of items displayed in the dropdown

It can be a string of value `divider` or an object of type [DropdownLink](#DropdownLink)
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>partial</code>
            </td>
            <td class="label-cell"></td>
            <td class="fullwidth-cell">
Partial path to used for rendering the dropdown content.

The partial property take precedence over the `items` property but you still need to fill the `items`.

This is required for 2 reasons:

1. Make Nacara understand your dropdown content and be able to generate the correct navigation element
2. If the dropdown is not pinned, the `items` elements will be used to render the mobile menu
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>fullwidth</code>
            </td>
            <td class="label-cell"></td>
            <td class="fullwidth-cell">
If `true`, the dropdown will also take the full width of the screen

Default: `false`
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>pinned</code>
            </td>
            <td class="label-cell"></td>
            <td class="fullwidth-cell">
If `true`, the link will be displayed on mobile display too

Default: `false`
            </td>
        </tr>
    </tbody>
</table>

###### DropdownLink

<table class="table is-narrow is-bordered is-vcentered">
    <thead>
        <tr>
            <th class="label-cell">Property</th>
            <th class="label-cell">Required</th>
            <th class="label-cell">Description</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td class="label-cell">
                <code>label</code>
            </td>
            <td class="label-cell">X</td>
            <td class="fullwidth-cell">
                Text to display
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>url</code>
            </td>
            <td class="label-cell">X</td>
            <td class="fullwidth-cell">
                URL to redirect to
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>Description</code>
            </td>
            <td class="label-cell"></td>
            <td class="fullwidth-cell">
Optional description

You can use `\n` into the string in order to force a new line
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>section</code>
            </td>
            <td class="label-cell"></td>
            <td class="fullwidth-cell">
                Section of the website associated to this navbar item
            </td>
        </tr>
    </tbody>
</table>

**Example**

```json
{
    "navbar": {
        "start": [
            {
                "pinned": true,
                "label": "Docs",
                "items": [
                    {
                        "section": "nacara",
                        "label": "Nacara",
                        "description": "Description line1\nline2 start here",
                        "url": "/Nacara/documentation/introduction.html"
                    },
                    "divider",
                    {
                        "label": "Nacara.Layout.Standard",
                        "url": "/Nacara/documentation/layout/introduction.html"
                    }
                ]
            }
        ]
    }
}
```

#### end

List of `IconLink`

<table class="table is-narrow is-bordered is-vcentered">
    <thead>
        <tr>
            <th class="label-cell">Property</th>
            <th class="label-cell">Required</th>
            <th class="label-cell">Description</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td class="label-cell">
                <code>label</code>
            </td>
            <td class="label-cell">X</td>
            <td class="fullwidth-cell">

Label of the link to display

*The default layout use it when on mobile screen*
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>icon</code>
            </td>
            <td class="label-cell">X</td>
            <td class="fullwidth-cell">
FontAwesome class icons part of the [free pack](https://fontawesome.com/icons?d=gallery&p=2&m=free)

Example: `fab fa-twitter`

*The default layout use it on tablet and desktop screen*
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>url</code>
            </td>
            <td class="label-cell">X</td>
            <td class="fullwidth-cell">
                URL to link to
            </td>
        </tr>
    </tbody>
</table>

**Example**

```json
{
    "navbar": {
        "end": [
            {
                "url": "https://github.com/MangelMaxime/Nacara",
                "icon": "fab fa-github",
                "label": "GitHub"
            },
            {
                "url": "https://twitter.com/MangelMaxime",
                "icon": "fab fa-twitter",
                "label": "Twitter"
            }
        ]
    }
}
```

### remarkPlugins

This is where you configure the list of [remark](https://remark.js.org/) plugins to load.

<table class="table is-narrow is-bordered is-vcentered">
    <thead>
        <tr>
            <th class="label-cell">Property</th>
            <th class="label-cell">Required</th>
            <th class="label-cell">Description</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td class="label-cell">
                <code>resolve</code>
            </td>
            <td class="label-cell">X</td>
            <td class="fullwidth-cell">
                Name of the NPM package to load
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>property</code>
            </td>
            <td class="label-cell"></td>
            <td class="fullwidth-cell">

If set, Nacara will load your plugin using `pluginName.default[propertyName]`

*Note: The only plugin I found requiring the use of this feature is [gatsby-remark-vscode](https://github.com/andrewbranch/gatsby-remark-vscode)*

```json
{
    "resolve": "gatsby-remark-vscode",
    "property": "remarkPlugin",
}
```
</td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>options</code>
            </td>
            <td class="label-cell"></td>
            <td class="fullwidth-cell">
                Options to pass to the plugin
            </td>
        </tr>
    </tbody>
</table>

**Example**

```json
{
    "remarkPlugins": [
        {
            "resolve": "gatsby-remark-vscode",
            "property": "remarkPlugin",
            "options": {
                "theme": "Atom One Light",
                "extensions": [
                    "vscode-theme-onelight"
                ]
            }
        },
        {
            "resolve": "remark-directive"
        }
    ]
}
```

### rehypePlugins

This is where you configure the list of [rehype](https://github.com/rehypejs/rehype) plugins to load.

<table class="table is-narrow is-bordered is-vcentered">
    <thead>
        <tr>
            <th class="label-cell">Property</th>
            <th class="label-cell">Required</th>
            <th class="label-cell">Description</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td class="label-cell">
                <code>resolve</code>
            </td>
            <td class="label-cell">X</td>
            <td class="fullwidth-cell">
                Name of the NPM package to load
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>property</code>
            </td>
            <td class="label-cell"></td>
            <td class="fullwidth-cell">

If set, Nacara will load your plugin using `pluginName.default[propertyName]`

*Note: I don't think any rehype plugin needs this feature, but I added it to be on par with `remarkPlugins` feature.*
</td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>options</code>
            </td>
            <td class="label-cell"></td>
            <td class="fullwidth-cell">
                Options to pass to the plugin
            </td>
        </tr>
    </tbody>
</table>

**Example**

```json
{
    "rehypePlugins": [
        {
            "resolve": "rehype-slug",
        }
    ]
}
```

### layouts

This is where you register the different layouts supported by your website.

It consists on a list of `string` which can be:
- The name of an npm package

    You need to have it installed in your project <br/><br/>

- A relative path to a `.js` or `.jsx` file.

    Learn more about custom layout [here](/Nacara/documentation/advanced/layout-from-scratch.html)

**Example**

```json
{
    "layouts": [
        "nacara-layout-standard",
        "./scripts/blog-page.jsx"
    ]
}
```

### serverPort

**Optional**

Configure on which port Nacara should start the watch server.

Default is `8080`.

**Example**

```json
{
    "serverPort": 8000
}
```

### source

**Optional**

Configure where the source folder is.

Default is `docs`

**Example**

```json
{
    "source": "docsrc"
}
```

### output

**Optional**

Configure where Nacara will put the generated files.

Default is `docs_deploy`

**Example**

```json
{
    "output": "public"
}
```
