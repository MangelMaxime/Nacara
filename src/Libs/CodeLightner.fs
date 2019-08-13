module CodeLightner

open System
open Fable.Core

type [<AllowNullLiteral>] Config =
    abstract backgroundColor: string option with get, set
    abstract textColor: string option with get, set
    abstract grammarFiles: string array with get, set
    abstract scopeName: string with get, set
    abstract themeFile: string with get, set

let [<Import("lighten","code-lightner")>] lighten (config : Config) (sourceCode : string) : JS.Promise<string> = jsNative
