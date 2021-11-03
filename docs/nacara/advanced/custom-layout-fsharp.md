---
title: With FSharp
layout: standard
---

:::primary{title="Note"}
Creating a layout via F#, is the recommended way when working on complex layout or releasing it as an NPM package.
:::

<ul class="textual-steps">

<li>

Create a folder to host your code `Nacara.Layout.Custom`.

```
mkdir Nacara.Layout.Custom && cd Nacara.Layout.Custom
```

</li>

<li>

Create an FSharp project `Nacara.Layout.Custom.fsproj`

```
dotnet new library -lang f#
```

</li>

<li>

Add `Nacara.Core` to your project

```
dotnet add package Nacara.Core
```

By default you have access to `Fable.React`, if you prefer `Feliz` you can add it

```
dotnet add package Feliz
```

</li>

<li>

Change `Library.fs` content to:

```fsharp
module Layout

open Fable.Core.JsInterop
open Nacara.Core.Types
open Fable.React
open Fable.React.Props

// Your render function, it is responsible to transform a page into HTML
let render (rendererContext : RendererContext) (pageContext : PageContext) =
    promise {
        let! pageContent =
            rendererContext.MarkdownToHtml pageContext.Content

        return div [ ]
            [
                str "Hello, from your custom layout"
                br [ ]
                div [ DangerouslySetInnerHTML { __html = pageContent} ]
                    [ ]
            ]
    }

// This is how we expose layouts to Nacara
exportDefault
    {
        Renderers = [|
            {
                Name = "my-layout"
                Func = render
            }
        |]
        Dependencies = [| |]
    }
```

</li>

<li>

Compile you project to JavaScript using Fable

If you don't already have Fable installed in your project you need to install it

```
dotnet new tool-manifest
dotnet tool install fable
```

Launch the compilation

```sh
dotnet fable --watch
```

</li>

<li>

Register your layout in the `nacara.config.json`

```js
{
    // ...
    "layouts": [
        "nacara-layout-standard",
        "./Nacara.Layout.Custom/Library.js"
    ]
}
```

</li>

</ul>

:::info
You can find type documentation in the API section.

Here are the most important one when working with custom layouts:

- [RendererContext](/Nacara/reference/Nacara.Core/nacara-core-types-renderercontext.html) : Context accessible when rendering a page.
- [PageContext](/Nacara/reference/Nacara.Core/nacara-core-types-pagecontext.html) : Represents the context of a page within Nacara.
- [LayoutInfo](/Nacara/reference/Nacara.Core/nacara-core-types-layoutinfo.html) : Exposed contract of a layout
:::
