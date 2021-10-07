---
title: Markdown features
layout: standard
---

## Code blocks

By default, all the code blocks have a "Copy" button associated to them.

If needed, you can prevent the "Copy" button from being added by adding the attributes `data-disable-copy-button="true"` to any parent containing the code blocks.

````html

<div data-disable-copy-button="true">

```
This code will not have the copy button
```

```
This code will not have the copy button
```

</div>

```
This code have the copy button
```
````

## Block containers

Block container allows you to easily colored message blocks, to emphasize part of your page.

To create a block container, you need to wrap your text with 3 colons, specify a type.

```
:::<type>
Your text goes here
:::
```

You can also optionally specify a title

```
:::<type>{title="My title"}
Your text goes here
:::
```

Available types are:

- primary
- info
- success
- warning
- danger

<div class="columns is-multiline is-mobile">
<div class="column is-6 has-text-centered">

**Code**

</div>
    <div class="column is-6 has-text-centered">

**Preview**

</div>
    <div class="column is-6">

```
:::primary{title="Information"}
Your text goes here
:::
```

</div>
    <div class="column is-6">

:::primary{title="Information"}
Your text goes here
:::

</div>
    <div class="column is-6">

```
:::warning{title="Warning"}
Please read this notice
:::
```

</div>
    <div class="column is-6">

:::warning{title="Warning"}
Please read this notice
:::

</div>
<div class="column is-6">

```
:::info
Please read this notice
:::
```

</div>
    <div class="column is-6">

:::info
Please read this notice
:::

</div>
<div class="column is-6">

```
:::danger
Something went wrong
:::
```

</div>
    <div class="column is-6">

:::danger
Something went wrong
:::

</div>
<div class="column is-6">

```
:::success
Everything looks good
:::
```

</div>
    <div class="column is-6">

:::success
Everything looks good
:::

</div>
</div>

## Textual steps

Textual steps makes it easy to create a step by step guides. It will auto-generate the number of the steps for you.

To create a textual steps, you need to wrap your text withing a `<ul class="textual-steps></ul>` and put each step inside a `<li>...</li>`

````html
<ul class="textual-steps">

<li>

This is step one

</li>

<li>

This is step two.

And as you can see **Markdown** syntax can be used inside textual-steps.

```js
console.log("Hello")
```

</li>

</ul>
````

**Preview:**

<ul class="textual-steps">

<li>

This is step one

</li>

<li>

This is step two.

And as you can see **Markdown** syntax can be used inside textual-steps.

```js
console.log("Hello")
```

</li>

</ul>
