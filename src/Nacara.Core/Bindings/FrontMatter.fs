module FrontMatter

open Fable.Core

type [<AllowNullLiteral>] FrontMatterResult<'T> =
    abstract attributes: 'T
    abstract body: string
    abstract frontmatter: string option

type [<AllowNullLiteral>] FM =
    [<Emit "$0($1...)">] abstract Invoke<'T> : file: string -> FrontMatterResult<'T>
    abstract test: file: string -> bool

let [<Import("default","front-matter")>] fm: FM = jsNative
