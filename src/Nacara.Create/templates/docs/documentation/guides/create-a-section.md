---
title: Create a Section
layout: nacara-standard
---

A section in Nacara helps you organize your website. For example, you can have the following sections:

- **Documentation**: a place to host your project documentation
- **Blog**: a place to publish blog posts

## Create a new section

A section, is defined by creating a folder right under your source folder in our case `docsrc`.

Currently, you have this structure:

```
docsrc
└── documentation
```

This means that we only have a single section called `documentation`

<ul class="textual-steps">

<li>

Create a new folder `docsrc/blog`, to get

```
docsrc
├── blog
└── documentation
```

You now have 2 sections one for your documentation and another one for hosting some blog post.

</li>

<li>

Create a file `docsrc/blog/index.md`:

```
---
title: Blog
layout: nacara-standard
---

List of posts:

* [Welcome]({{REPLACE_WITH_BASE_URL}}blog/2021/welcome.html)
```

This file will serve as the index of your blog

Create a file `docsrc/blog/2021/welcome.md`:

```
---
title: Welcome
layout: nacara-standard
---

Welcome to my blog
```

</li>

<li>

We now have our blog pages created but no way to access them.

Let's add a new **section** to the navbar.

Add these lines to the `navbar.start` property of `nacara.config.json`:

```json
{
    "section": "blog",
    "url": "{{REPLACE_WITH_BASE_URL}}blog/index.html",
    "label": "Blog"
}
```

It should looks similar to that:

```json
"navbar": {
    "start": [
        {
            "section": "documentation",
            "url": "{{REPLACE_WITH_BASE_URL}}documentation/index.html",
            "label": "Documentation"
        },
        {
            "section": "blog",
            "url": "{{REPLACE_WITH_BASE_URL}}blog/index.html",
            "label": "Blog"
        }
    ],
```

Kill Nacara and re-start it to see the changes

:::info
Right now, Nacara doesn't restart itself yet when `nacara.config.json` is modified. It should come in future version.
:::

You should see a `Blog` button in the navbar, click on it and see your blog index page.

</li>

</ul>
