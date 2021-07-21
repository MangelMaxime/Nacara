module Log

open Fable.Core
open Glutinum.Chalk

let info (text : string) =
    JS.console.log(chalk.blueBright.Invoke text)

let warn (text : string) =
    JS.console.warn(chalk.yellow.Invoke text)

let success (text : string) =
    JS.console.log(chalk.green.Invoke text)

let error (text : string) =
    JS.console.error(chalk.red.Invoke text)

let log (text : string) =
    JS.console.log text
