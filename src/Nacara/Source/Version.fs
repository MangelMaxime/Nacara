module Version

open Fable.Core
open Fable.Core.JsInterop

let getVersion () : JS.Promise<string> =
    import "default" "./../js/version.js"

let version () =
    promise {
        let! version = getVersion ()

        Log.log $"Nacara version: {version}"
    }
