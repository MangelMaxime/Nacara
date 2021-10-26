---
title: Create a Section
layout: standard
---

A section in Nacara helps you organize your website. For example, you can have the following sections:

- **Documentation**: a place to host your project documentation
- **Blog**: a place to publish blog posts

## Create a new section

A section, is defined by creating a folder right under your source folder.

Currently, you have this structure:

```
docsrc
└── documentation
```

This means that we only have a single section called `documentation`

<ul class="textual-steps">

<li>

Create a new folder `docs/blog`, to get

```
docsrc
├── blog
└── documentation
```

You now have 2 sections one for your documentation and another one for hosting some blog post.

</li>

<li>

Create a file `docs/blog/index.md`:

```
---
title: Blog
layout: standard
---

List of posts:

* [Welcome](/blog/2021/welcome.html)
```

This file will serve as the index of your blog

Create a file `docs/blog/2021/welcome.md`:

```
---
title: Welcome
layout: standard
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
    "url": "/blog/index.html",
    "label": "Blog"
}
```

It should looks similar to that:

```json
"navbar": {
    "start": [
        {
            "section": "documentation",
            "url": "/documentation/index.html",
            "label": "Documentation"
        },
        {
            "section": "blog",
            "url": "/blog/index.html",
            "label": "Blog"
        }
    ],
```

Nacara will re-start itself, to take your changes into consideration.

You should see a `Blog` button in the navbar, click on it and see your blog index page.

</li>

</ul>
