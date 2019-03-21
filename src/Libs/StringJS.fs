module rec StringJS

open System
open Fable.Core
open Fable.Import.JS

let [<Import("default","string")>] S: TypeLiteral_01 = jsNative

type [<AllowNullLiteral>] StringJS =
    abstract length: float with get, set
    abstract s: string with get, set
    abstract between: left: string * ?right: string -> StringJS
    abstract camelize: unit -> StringJS
    abstract capitalize: unit -> StringJS
    abstract chompLeft: prefix: string -> StringJS
    abstract chompRight: suffix: string -> StringJS
    abstract collapseWhitespace: unit -> StringJS
    abstract contains: ss: string -> bool
    abstract count: substring: string -> float
    abstract dasherize: unit -> StringJS
    abstract decodeHTMLEntities: unit -> StringJS
    abstract endsWith: ss: string -> bool
    abstract escapeHTML: unit -> StringJS
    abstract ensureLeft: prefix: string -> StringJS
    abstract ensureRight: suffix: string -> StringJS
    abstract humanize: unit -> StringJS
    abstract ``include``: ss: string -> bool
    abstract isAlpha: unit -> bool
    abstract isAlphaNumeric: unit -> bool
    abstract isEmpty: unit -> bool
    abstract isLower: unit -> bool
    abstract isNumeric: unit -> bool
    abstract isUpper: unit -> bool
    abstract latinise: unit -> StringJS
    abstract left: n: float -> StringJS
    abstract lines: unit -> string array
    abstract pad: len: float * ?char: string -> StringJS
    abstract pad: len: float * ?char: float -> StringJS
    abstract padLeft: len: float * ?char: string -> StringJS
    abstract padLeft: len: float * ?char: float -> StringJS
    abstract padRight: len: float * ?char: string -> StringJS
    abstract padRight: len: float * ?char: float -> StringJS
    abstract parseCSV: ?delimiter: string * ?qualifier: string * ?escape: string * ?lineDelimiter: string -> string array
    abstract repeat: n: float -> StringJS
    abstract replaceAll: ss: string * newStr: string -> StringJS
    abstract strip: [<ParamArray>] strings: string array -> StringJS
    abstract stripLeft: [<ParamArray>] strings: string array -> StringJS
    abstract stripRight: [<ParamArray>] strings: string array -> StringJS
    abstract right: n: float -> StringJS
    abstract setValue: string: obj option -> StringJS
    abstract slugify: unit -> StringJS
    abstract startsWith: prefix: string -> bool
    abstract stripPunctuation: unit -> StringJS
    abstract stripTags: [<ParamArray>] tags: string array -> StringJS
    abstract template: values: Object * ?``open``: string * ?close: string -> StringJS
    abstract times: n: float -> StringJS
    abstract titleCase: unit -> StringJS
    abstract toBoolean: unit -> bool
    abstract toCSV: ?delimiter: string * ?qualifier: string -> StringJS
    abstract toCSV: options: StringJSToCSVOptions -> StringJS
    abstract toFloat: ?precision: float -> float
    abstract toInt: unit -> float
    abstract toInteger: unit -> float
    abstract toString: unit -> string
    abstract trim: unit -> StringJS
    abstract trimLeft: unit -> StringJS
    abstract trimRight: unit -> StringJS
    abstract truncate: length: float * ?chars: string -> StringJS
    abstract underscore: unit -> StringJS
    abstract unescapeHTML: unit -> StringJS
    abstract wrapHTML: ?element: string * ?attributes: Object -> StringJS

type [<AllowNullLiteral>] StringJSToCSVOptions =
    abstract delimiter: string option with get, set
    abstract qualifier: string option with get, set
    abstract escape: string option with get, set
    abstract encloseNumbers: bool option with get, set
    abstract keys: bool option with get, set

type [<AllowNullLiteral>] TypeLiteral_01 =
    [<Emit "$0($1...)">] abstract Invoke: o: 'T -> StringJS
    abstract VERSION: string with get, set
    abstract TMPL_OPEN: string with get, set
    abstract TMPL_CLOSE: string with get, set
