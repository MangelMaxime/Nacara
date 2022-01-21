---
title: Partials
layout: standard
---

Partials are small components which are used by the layout to give you control over a certain part of the page.

The most common usage for a partial is to add a footer to your website.

Look at **your layout documentation** to know which partials are **available**.

:::info
If you are a layout creator, learn more about how to access the partial by [clicking here](nacara/partials).
:::

## Structure

To create a partial you need to create a file under the `docs/_partials` folder.

For example, with the following structure there are two partials available:

- `footer`
- `dropdown-1`

```
docs
└── _partials
    ├── dropdown-1.jsx
    └── footer.jsx
```

## Writing a partial

When writing a partial you can use raw JavaScript or JSX. If you use JSX, you will need to [setup babel](https://babeljs.io/docs/en/config-files).

The easiest way to do it is by installing `@babel/preset-react` and creating a `babel.config.json` file near your `package.json` containing:

```json
{
    "presets": [
        "@babel/preset-react"
    ]
}
```
