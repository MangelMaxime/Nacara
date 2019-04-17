---
title: Getting started
---

# Quick start

:::info
**Nacara** is a young project but it already make generating documentation easy.

Here are some example of documentation website created using Nacara:

- [Nacara documentation](https://mangelmaxime.github.io/Nacara)
:::

<ul class="textual-steps">
<li>

Ensure you have the latest version of Node installed. We also recommend you install Yarn as well.

:::warning
You have to be on Node >= 8.x and Yarn >= 1.5.
:::
</li>

<li>

Add Nacara to your project by running `yarn add -D nacara`
</li>

<li>

Create a file `doc.json` at the root of your project copy this json in it:

```json
{
    "url": "https://mangelmaxime.github.io",
    "baseUrl": "/Nacara/",
    "title": "Nacara",
    "version": "0.1.0"
}
```

*Don't forget to adapt the values*

</li>

<li>

Create a folder `docsrc` it will host the source of your documenation.

Create to file under this folder:
- `index.md` - it will be the main page of your documenation website
- `style.scss` - you will use it to load Nacara styles and customize it
    - Add this line `@import './node_modules/nacara/scss/nacara.scss';`
</li>

<li>

Run Nacara: `yarn run nacara`
</li>

<li>

You can now make changes in your `index.md` and see the changes being applied directly in your browser.
</li>

</ul>
