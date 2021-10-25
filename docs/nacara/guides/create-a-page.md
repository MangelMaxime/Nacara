---
title: Create a page
layout: standard
---

A page in Nacara is a **Markdown** file composed of two things:

- **Front Matter**: configure how the page should rendered, for example it is here that you specify which layout to applied
- **Content**: The markdown content to include in the page

## Create your first page

Create a file `docsrc/documentation/tutorial/my-page.md`:

```
---
title: My page
layout: standard
---

This is a new page created for the tutorial.
```

The new page is available at [http://localhost:8080/documentation/tutorial/my-page.html](http://localhost:8080//documentation/tutorial/my-page.html)

## Add your page to the menu

If you look on the menu in the left, you will see that your page is missing from it.

This is because you need to provide some information to Nacara via the `menu.json` file.

Edit the file `docsrc/documentation/menu.json` to add `"documentation/tutorial/my-page"` to it.

The file should looks like:

```json
[
    "documentation/introduction",
    {
        "type": "category",
        "label": "API",
        "items": [
            "documentation/guides/create-a-page",
            "documentation/guides/create-a-section",
            "documentation/guides/customize-the-style"
        ]
    },
    "documentation/tutorial/my-page"
]
```

You should now see `My page` in the menu.

## Front matter

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
            <td class="has-text-centered" style="vertical-align: middle">
                <code>layout</code>
            </td>
            <td class="has-text-centered" style="vertical-align: middle">
                X
            </td>
            <td>Name of the layout used to render the page</td>
        </tr>
        <tr>
            <td class="has-text-centered" style="vertical-align: middle">
                <code>title</code>
            </td>
            <td class="has-text-centered" style="vertical-align: middle">
            </td>
            <td>Optional, title of the page</td>
        </tr>
    </tbody>
</table>
