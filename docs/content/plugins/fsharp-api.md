---
title: API reference
---

Turns the assemblies your library ships into reference pages: one per type or module, with the
signatures you wrote and links between them.

This site's own [reference](../reference/index.md) is built by it.

## Add it

```bash frame="terminal"
dotnet add package Nacara.Plugin.FSharpApi --prerelease
```

This one reads your assemblies with the F# compiler, which pins `FSharp.Core` to an exact version -
a newer one than the SDK carries. Name it in your project, or the build reports NU1605 and compiles
against the older one:

```xml title="docs.fsproj"
<PackageReference Update="FSharp.Core" Version="10.1.400" />
```

No other Nacara package asks for this.

```fsharp ins={1-10,15,17}
let apiOptions =
    { FSharpApi.defaults with
        Root = "reference"
        Sources = [ FSharpApiSource.create "../src/My.Library/bin/Release/net10.0/My.Library.dll" ]
    }

let reference =
    FSharpApi.collection "reference" DocFrontMatter.decoder apiOptions
    |> Collection.title _.Title
    |> Collection.layout (Theme.layout theme)

let site =
    Site.create "My library"
    |> Markdown.register
    |> FSharpApi.register apiOptions
    |> Theme.register theme
    |> Site.collection reference
```

The collection produces the pages, so their front matter, route and layout are yours as with any
content. `register` ships their styling and offers the menu.

:::warning Two things to get right
Point it at a **built** assembly - the plugin compiles nothing for you. And build with
`GenerateDocumentationFile`: the prose comes from the `.xml` file beside the assembly.
:::

## What you get

| | |
|---|---|
| `/reference/` | the packages |
| `/reference/<package>/` | its namespaces |
| `/reference/<package>/<namespace>/` | what it declares there, grouped by kind |
| `/reference/<package>/<namespace>/<name>/` | one type or module, and its members |

Each declaration is a collapsible: closed you see its signature, open you see everything its author
wrote.

<!-- rumdl-disable MD013 -->
<div class="nacara-api__entry">
<details class="nacara-api__member">
<summary><pre class="nacara-api__signature"><code><span class="tok-function">create</span> <span class="tok-punctuation">(</span><span class="tok-parameter">text</span><span class="tok-punctuation">:</span> <span class="tok-type">string</span><span class="tok-punctuation">)</span> <span class="tok-punctuation">:</span> <span class="tok-type">string</span></code></pre></summary>
<div class="nacara-api__body">
<p>Create a lowercase, dash-separated slug from <code>text</code>.</p>
</div>
</details>
<details class="nacara-api__member" open>
<summary><pre class="nacara-api__signature"><code><span class="tok-function">ofPath</span> <span class="tok-punctuation">(</span><span class="tok-parameter">path</span><span class="tok-punctuation">:</span> <span class="tok-type">string</span><span class="tok-punctuation">,</span> <span class="tok-punctuation">?</span><span class="tok-parameter">limit</span><span class="tok-punctuation">:</span> <span class="tok-type">int</span><span class="tok-punctuation">)</span> <span class="tok-punctuation">:</span> <span class="tok-type">string</span></code></pre></summary>
<div class="nacara-api__body">
<p>A slug for every segment of a path, joined back up.</p>
</div>
</details>
</div>
<!-- rumdl-enable MD013 -->

Those two are written by hand for this page. [The real thing](../reference/nacara-core/nacara-core/slug.md)
is one click away, and always current.

Signatures read as standard F# code, not as the compiler signature -
`create (text: string) : string` rather than `string -> string`, optional parameters as
`?limit: int`, a union case with its field names.

## Getting a good reference out of it

The pages are only as good as the XML docs in your source. Everything you write is used:
`summary`, `remarks`, `param`, `typeparam`, `returns`, `exception`, `example` and `seealso`.

A `<see cref="T:My.Library.Person"/>` becomes a link when this build published that type.

F# wants all the parameters documented or none of them - document one and the compiler asks for
the rest with `FS3390`.

Turn on `WarnOnUndocumented` when your library has caught up, and every member that takes parameters
and documents none is reported. `check` turns those into a failure, which makes a useful gate.

## Options

| Option | Default | Effect |
|---|---|---|
| `Sources` | `[]` | The assemblies to document |
| `Root` | `"api"` | Route prefix the pages are published under |
| `Title` | `"API reference"` | Title of the page listing the packages |
| `Exclude` | `[]` | Namespaces to leave out, with everything under them |
| `WarnOnUndocumented` | `false` | Report a member that takes parameters and documents none |

```fsharp
{ FSharpApi.defaults with
    Sources = 
        [ 
            "My.Library"
            "My.Library.Extras" 
        ]
        |> List.map (fun name -> FSharpApiSource.create $"bin/Release/net10.0/{name}.dll")
    Exclude = [ "My.Library.Internal" ]
}
```

`Exclude` is for what F# made you make public so the library would compile, not so a reader would be
sent to it.

Several assemblies are read in one pass, so a dozen costs about what one does. Each declaration then
says which package it ships in, since that is the one a reader has to add.

If an assembly's dependencies are not beside it, say where they are:

```fsharp
FSharpApiSource.create "../src/My.Library/bin/Release/net10.0/My.Library.dll"
|> FSharpApiSource.searchPaths [ "../src/My.App/bin/Release/net10.0" ]
```

Three places are already searched: beside the assembly, the running runtime, and the directory your
site runs from - which covers you when your site references the library it documents.

## The menu

You get one without asking. `register` builds a sidebar for the reference section - a group per
package, and under it what that package declares - and the menu on this site's own reference pages
is exactly that, unchanged.

If you declare a menu for the same section in your theme, yours is used instead. `FSharpApi.outline`
gives you the shape to build it from, so you do not have to list your types by hand:

```fsharp
// Read once. Every page asks the theme for its menu.
let outline = lazy (FSharpApi.outline apiOptions)

let rec entry (item: FSharpApiOutlineEntry) =
    match item.Children with
    | [] -> Menu.page item.Page
    | children -> Menu.group item.Page [ for child in children -> entry child ]

let theme =
    Theme.defaults
    |> Theme.menu
        "reference"
        [
            Menu.page "index.md"

            for package in outline.Value do
                Menu.group
                    package.Page
                    [
                        for ns in package.Namespaces do
                            for item in ns.Entries -> entry item
                    ]
        ]
```

Reach for this when you want the reference to sit inside a menu of your own - a "Reference" section
under your guide, say - or when you want to order or rename what the default lists.

`FSharpApi.read` gives you the whole model, signatures and documentation included, for anything
larger than a menu.

## Reference

Every function and option of it, signature by signature: [`FSharpApi`](../reference/nacara-plugin-fsharpapi/nacara-plugins/fsharpapi.md).
