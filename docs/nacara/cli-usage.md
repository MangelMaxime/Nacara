---
title: Command line usage
layout: standard
---

## Usage

Nacara has several commands available.

Here is a list of the commands and their usage:

- `build` : Build the website
- `clean` : Clean up the generated files
- `serve` : Serve the website locally
- `watch` : Start the development server

You can use `--help` to get more information about a specific command.

## Hooks

### afterClean

Both `build` and `watch` support the `--afterClean` hooks which takes a command and will spawn it after the initial clean is done.

This is useful if you are not using SASS/SCSS to generates your website style.

Indeed, if you use [TailwindCSS](https://tailwindcss.com/) you can do:

`nacara build --afterClean 'npx postcss style/main.css -o docs_deploy/style.css'`

This will spawn `postcss` once the initial clean is done and then generates your website.

:::warning
It is important that you place your command **between** quotes or singles quotes.
:::

## Watcher improvements

If you see an error like `EMFILE: too many open files`, you can try setting the `CHOKIDAR_USEPOLLING` environment variable to `true`.

Example: `CHOKIDAR_USEPOLLING=true nacara watch`

See [Chokidar documentation](https://github.com/paulmillr/chokidar#performance) to learn more about it.
