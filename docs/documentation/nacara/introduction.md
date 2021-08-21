---
title: Get up and running
layout: nacara-standard
---

# Quick start

<ul class="textual-steps">
<li>

Ensure you have the latest version of Node installed.

<br/>

:::warning
You have to be on Node >= 8.x.
:::
</li>

<li>

Add Nacara and the default layout package to your project by running

<br />

```
npm --save-dev nacara nacara nacara-layout-standard
```

</li>

<li>

Create a file `nacara.config.json` at the root of your project and copy this json in it:

<br />

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

Create a folder `docsrc` it will host the source of your documentation.

Create the following files under this folder:
- `index.md` - it will be the main page of your documentation website
- `style.scss` - you will use it to load Nacara styles and customize it
    - By default, you need to add this lines in it:
        ```
            @import './../node_modules/bulma/bulma.sass';
            @import './../node_modules/nacara/scss/nacara.scss';
        ```

        *Please check that the paths are correct for your repo setup*
</li>

<li>

Run Nacara: `yarn run nacara`
</li>

<li>

You can now make changes in your `index.md` and see the changes being applied directly in your browser.
</li>

</ul>
