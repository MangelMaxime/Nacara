(**
---
title: Demo
order: 5
---
*)

(**
This page is an F# file. What you are reading lives in `docs/content/plugins/literate/demo.fsx`,
and the code below is the code that file contains - it cannot drift from the prose, because it is
the same file. [The plugin page](index.md) covers adding it and its options; this one is what a
literate file looks like.

## How a literate file is read

Prose goes in `(** … *)` comments, and everything else is code. The front matter goes in the
comment the file opens with, because a file that starts with `---` does not compile:

```fsharp
(**
---
title: Literate F#
---
*)
```

Here is a type, written as ordinary code in this file:
*)

type Person =
    {
        Name: string
        Age: int
    }

(**
## Commands

`(*** hide ***)` drops the block that follows from the page - useful for the scaffolding a sample
needs but a reader does not:
*)

(*** hide ***)
let private scaffolding = System.Random(1)

(**
Anything else in a `(*** … ***)` comment becomes the [fence meta](../../guide/code-blocks.md) of the block that
follows, so a literate block can be titled, marked or numbered like any other:

```fsharp
(*** title="Greeting.fs" {2} ***)
let greet (person: Person) =
    if person.Age < 18 then
        $"Hi {person.Name}"
    else
        $"Hello {person.Name}"
```

*)

(*** title="Greeting.fs" {2} ***)
let greet (person: Person) =
    if person.Age < 18 then
        $"Hi {person.Name}"
    else
        $"Hello {person.Name}"
