// ts2fable 0.6.1
module Sass

open Fable.Core
open Fable.Import
open Fable.Import.JS

type ImporterReturnFile =
    { file : string }

type ImporterReturnContents =
    { contents : string }

type ImporterReturnType =
    U3<ImporterReturnFile, ImporterReturnContents, Error> option

type [<AllowNullLiteral>] Importer =
    [<Emit "$0($1...)">]
    abstract Invoke: url: string * prev: string * ``done``: (ImporterReturnType -> unit) -> ImporterReturnType
    [<Emit "$0($1...)">]
    abstract InvokeVoid: url: string * prev: string * ``done``: (ImporterReturnType -> unit) -> unit

[<StringEnum>]
[<RequireQualifiedAccess>]
type OutputStyle =
    | Compact
    | Compressed
    | Expanded
    | Nested

type [<AllowNullLiteral>] Options =
    abstract file: string with get, set
    abstract data: string with get, set
    abstract importer: U2<Importer, Importer array> with get, set
    abstract functions: obj with get, set
    abstract includePaths: string array with get, set
    abstract indentedSyntax: bool with get, set
    abstract indentType: string with get, set
    abstract indentWidth: float with get, set
    abstract linefeed: string with get, set
    abstract omitSourceMapUrl: bool with get, set
    abstract outFile: string with get, set
    abstract outputStyle: OutputStyle with get, set
    abstract precision: float with get, set
    abstract sourceComments: bool with get, set
    abstract sourceMap: U2<bool, string> with get, set
    abstract sourceMapContents: bool with get, set
    abstract sourceMapEmbed: bool with get, set
    abstract sourceMapRoot: string with get, set

type [<AllowNullLiteral>] SassError =
    inherit Error
    abstract message: string with get, set
    abstract line: float with get, set
    abstract column: float with get, set
    abstract status: float with get, set
    abstract file: string with get, set

type [<AllowNullLiteral>] Result =
    abstract css: Node.Buffer.Buffer with get, set
    abstract map: Node.Buffer.Buffer with get, set
    abstract stats: obj with get, set

type [<AllowNullLiteral>] IExports =
    abstract render: options: Options * callback: (SassError -> Result -> obj) -> unit
    abstract renderSync: options: Options -> Result

[<Import("*", "sass")>]
let sass : IExports = jsNative
