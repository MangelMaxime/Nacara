---
title: Getting started
order: 1
---

## Install

Back to the [home page](../index.md), or straight to [the deep end](advanced.md#going-further).

```fsharp title="Program.fs" {2} ins={3} /Nacara/
let site =
    Site.create "Nacara"
    |> Site.baseUrl "/"
```

## Inline code

Inside the backticks: `Site.create "Nacara"{:fsharp}`. Outside them:
`Site.baseUrl "/"`{fsharp} and `Site.output "out"`{lang=fsharp}. Left as
written: `{:js}` and `plain`{disabled}.

## Next steps

Nothing else for now.
