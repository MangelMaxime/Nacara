module rec Semver

open Fable.Core
open Fable.Core.JsInterop

let semver : IExport = import "default" "semver"

type IExport =
    abstract gte : v1 : string * v2 : string -> bool
