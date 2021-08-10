---
title: Markdown features
layout: nacara-standard
---

## Code blocks

## nacara-layout-standard

The following feature are only available if you are using `nacara-layout-standard` layouts. If you are using another layout, please check the according documentation to know its specificities.

### Block containers

Block container allows you to easily colored message blocks, to emphasize part of your page.

To create a block container, you need to wrap your text with 3 colons, specify a type and optionally add a title.

```
::: <type> [optional title]
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
::: primary Information
Your text goes here
:::
```

</div>
    <div class="column is-6">   

::: primary Information
Your text goes here
:::

</div>
    <div class="column is-6">

```
::: warning Warning
Please read this notice
:::
```

</div>
    <div class="column is-6">   

::: warning Warning
Please read this notice
:::

</div>
<div class="column is-6">

```
::: info
Please read this notice
:::
```

</div>
    <div class="column is-6">   

::: info 
Please read this notice
:::

</div>
<div class="column is-6">

```
::: danger
Something went wrong
:::
```

</div>
    <div class="column is-6">   

::: danger
Something went wrong
:::

</div>
<div class="column is-6">

```
::: success
Everything looks good
:::
```

</div>
    <div class="column is-6">   

::: success
Everything looks good
:::

</div>
</div>
