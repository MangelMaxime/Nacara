module Log

open Fable.Import

let private chalk = Chalk.chalk

let private _log x = JS.console.log x
let private _warn x = JS.console.warn x
let private _error x = JS.console.error x

let newLine (msg : string) = msg + "\n"

let warn fmt =
    Printf.kprintf (chalk.yellow.Invoke >> _warn) fmt

let info fmt =
    Printf.kprintf (chalk.blue.Invoke >> _log) fmt

let log fmt =
    Printf.kprintf (chalk.Invoke >> _log) fmt

let success fmt =
    Printf.kprintf (chalk.green.Invoke >> _log) fmt

let error fmt =
    Printf.kprintf (chalk.red.Invoke >> _error) fmt

let warnFn fmt =
    Printf.kprintf (chalk.yellow.Invoke >> newLine >> _warn) fmt

let infoFn fmt =
    Printf.kprintf (chalk.blue.Invoke >> newLine >> _log) fmt

let logFn fmt =
    Printf.kprintf (chalk.Invoke >> newLine >> _log) fmt

let successFn fmt =
    Printf.kprintf (chalk.green.Invoke >> newLine >> _log) fmt

let errorFn fmt =
    Printf.kprintf (chalk.red.Invoke >> newLine >> _error) fmt
