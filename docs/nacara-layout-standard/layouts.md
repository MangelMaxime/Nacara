---
title: Layouts
layout: standard
---

Here are all the layouts include in this package:

- `nacara-standard`: standard documentation page
- `nacara-navbar-only`: a navbar with a blank canvas
- `nacara-changelog`: for displaying your changelog on your website

## nacara-standard

This layout is mostly used for documentation page.

This layout include:

- An optional menu
- A table of content
- Navigation buttons

### Front matter

#### `toc`

Optional</br>
Type: `Object | Boolean`

Options:

- `from` - Default: `2`</br>
    The level of the heading to start the table of content

- `to` - Default: `2`</br>
    The level of the heading to end the table of content

**Example**

```yml
# Change the configuration of the table of content
toc:
    from: 2
    to: 3

# Disable the table of content
toc: false
```

## nacara-navbar-only

This layout will generate a navbar and your content underneath it.

In general, you will want to use this layout for your index page so you have total freedom to style it using HTML tags.

## nacara-changelog

This layout will parse your changelog file and generate a page based on it.

### Front matter

#### `changelog_path`

Required</br>
Type: `String`

Relative path to the changelog to display

**Example**

```yml
changelog_path: ./../../src/Nacara/CHANGELOG.md
```
