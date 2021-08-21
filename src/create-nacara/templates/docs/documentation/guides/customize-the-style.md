---
title: Style your application
layout: nacara-standard
---

Nacara allows you to customize your application style to match your design.

## Main file

The main file for styling your application is `docsrc/style.{{REPLACE_WITH_STYLE_EXTENSION}}`.

Modify the `$primary` color to `#44387a` and save the file.

You should see the site update and use the new color.

## Special folder

When you website become bigger you will want to split your `style` file into smaller files.

Nacara has specials folders to deal with that:

- `docsrc/scss`: use this folder is you are using SCSS to write your style
- `docsrc/sass`: use this folder is you are using SASS to write your style

When you a file in one of this folder changes, Nacara will recompile your `docsrc/style.{{REPLACE_WITH_STYLE_EXTENSION}}`.

Let's try it out

<ul class="textual-steps">

<li>

Add the following code to the page you created earlier `docsrc/tutorial/my-page`

```
<div class="my-text">

This text should be bold and red.

</div>
```

</li>

<li>

Create a file `docsrc/{{REPLACE_WITH_STYLE_EXTENSION}}/my-style.{{REPLACE_WITH_STYLE_EXTENSION}}`

```
.my-text {
    color: $danger;
    font-weight: $weight-bold;
}
```

Because default layout of Nacara use Bulma, you have access to [Bulma variables](https://bulma.io/documentation/customize/variables/).

</li>

<li>

Include your file in `style.{{REPLACE_WITH_REPLACE_WITH_STYLE_EXTENSION}}`

```
@import './scss/my-style.scss';
```

</li>

</ul>
