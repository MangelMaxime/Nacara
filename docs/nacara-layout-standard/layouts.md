---
title: Layouts
layout: nacara-standard
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

## nacara-navbar-only

This layout will generate a navbar and your content underneath it.

In general, you will want to use this layout for your index page so you have total freedom to style it using HTML tags.

## nacara-changelog

This layout will parse your changelog file and generate a page based on it.

**Front matter**

<table class="table is-narrow is-bordered">
    <thead>
        <tr>
            <th class="has-text-centered">Name</th>
            <th class="has-text-centered">Required</th>
            <th class="has-text-centered">Description</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td class="has-text-centered" style="vertical-align: middle">
                <code>changelog_path</code>
            </td>
            <td class="has-text-centered" style="vertical-align: middle">
                X
            </td>
            <td>Relative path to the changelog to display</td>
        </tr>
    </tbody>
</table>
