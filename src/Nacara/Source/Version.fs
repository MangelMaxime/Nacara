module Version

open Fable.Core.JsInterop

let version () =
    let nacaraVersion : string = emitJsExpr () """require("../package.json").version"""

    Log.log $"Nacara version: {nacaraVersion}"
