---
title: Style your application
layout: standard
---

Nacara allows you to customize your application style to match your design.

## Main file

The main file for styling your application is `docs/style.scss`.

Modify the `$primary` color to `#44387a` and save the file.

You should see the site update and use the new color.

## Special folder

When you website become bigger you will want to split your `style.scss` file into smaller files.

Nacara has specials folders to deal with that:

- `docs/scss`: use this folder is you are using SCSS to write your style
- `docs/sass`: use this folder is you are using SASS to write your style

When one of the files of these folders changes, Nacara will recompile your `docs/style.scss`.

Let's try it out

:::info
The template is configured to use SCSS, if you prefer to use SASS you can adapt the template.
:::

<ul class="textual-steps">

<li>

Add the following code to the page you created earlier `docs/tutorial/my-page`

```md
<div class="my-text">

This text should be bold and red.

</div>
```

</li>

<li>

Create a file `docs/scss/my-style.scss`

```scss
.my-text {
    color: $danger;
    font-weight: $weight-bold;
}
```

Because default layout of Nacara use Bulma, you have access to [Bulma variables](https://bulma.io/documentation/customize/variables/).

</li>

<li>

Include your file in `style.scss`

```scss
@import './scss/my-style.scss';
```

</li>

</ul>
