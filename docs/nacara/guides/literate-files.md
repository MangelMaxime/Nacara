---
layout: standard
title: Literate files
---

A literate file is a file ending with `.fs` or `.fsx` extension and which start with a front-matter block like this one:

```fs
(**
---
layout: the-layout-name
---
**)
```

Literate files are converted into markdown files before being processed by the Nacara as a normal markdown file.

They are really handy because:

- You can use your code editor to get intellisense when writting the snippets
- Compile or run the files to check for errors

:::info
To keep, the implementation simple, Nacara **doesn't** test the literate files against the F# compiler.

So if there is a syntax error Nacara will style transform the file.
:::

## Commands

You can decide to hide a code portion by using `(*** hide ***)`

```fs
(*** hide ***)

// This code is hidden
let answer = 42

(**
From here the code is visible
*)
```

## Markdown blocks

Markdown blocks are defined by using `(** ... *)`. Anything because these tags is going to be treated as markdown.

```fs
(**
### This line will be converted to a header

This text is **strong** and this one is in *italic*
*)
```
