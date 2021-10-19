/// Module contains logging functionality
module Log

open Fable.Core
open Glutinum.Chalk


/// <summary>
/// Log a message in the console using a bright blue color
/// </summary>
/// <param name="text"></param>
/// <returns></returns>
let info (text : string) =
    JS.console.log(chalk.blueBright.Invoke text)


/// <summary>
/// Log a warning in the console using a yellow color
/// </summary>
/// <param name="text">Message to log</param>
/// <returns></returns>
let warn (text : string) =
    JS.console.warn(chalk.yellow.Invoke text)

/// <summary>
/// Log a message in the console using a green color
/// </summary>
/// <param name="text">Message to log</param>
/// <returns></returns>
let success (text : string) =
    JS.console.log(chalk.green.Invoke text)

/// <summary>
/// Log an error in the console using a red color
/// </summary>
/// <param name="text">Message to log</param>
/// <returns></returns>
let error (text : string) =
    JS.console.error(chalk.red.Invoke text)


/// <summary>
/// Alias to <c>JS.console.log</c>
/// </summary>
/// <param name="text">Message to log</param>
/// <returns></returns>
let log (text : string) =
    JS.console.log text
