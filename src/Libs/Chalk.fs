module rec Chalk

open System
open Fable.Core
open Fable.Core.JS


/// Main Chalk object that allows to chain styles together.
/// Call the last one as a method with a string argument.
/// Order doesn't matter, and later styles take precedent in case of a conflict.
/// This simply means that `chalk.red.yellow.green` is equivalent to `chalk.green`.
[<Import("default", "chalk")>]
let chalk : Chalk = jsNative


[<RequireQualifiedAccess>]
type Level =
    /// All colors disabled.
    | None = 0
    /// Basic 16 colors support.
    | Basic = 1
    /// ANSI 256 colors support.
    | Ansi256 = 2
    /// Truecolor 16 million colors support.
    | TrueColor = 3


[<AllowNullLiteral>]
type ChalkOptions =
    /// Enable or disable Chalk.
    /// Default: `true`
    abstract enabled: bool with get, set

    /// Specify the color support for Chalk.
    /// By default, color support is automatically detected based on the environment.
    abstract level: Level with get, set


/// Detect whether the terminal supports color.
[<AllowNullLiteral>]
type ColorSupport =
    /// The color level used by Chalk.
    abstract level: Level with get, set

    /// Return whether Chalk supports basic 16 colors.
    abstract hasBasic: bool with get, set

    /// Return whether Chalk supports ANSI 256 colors.
    abstract has256: bool with get, set

    /// Return whether Chalk supports Truecolor 16 million colors.
    abstract has16m: bool with get, set


[<AllowNullLiteral>]
type Chalk =
    [<Emit "$0($1...)">]
    abstract Invoke: [<ParamArray>] text: string array -> string

    [<Emit "$0($1...)">]
    abstract Invoke: text: string[] * [<ParamArray>] placeholders: string array -> string

    /// Return a new Chalk instance.
    abstract ``constructor``: ?options: ChalkOptions -> Chalk with get

    /// Enable or disable Chalk.
    /// - default: `true`
    abstract enabled: bool with get, set

    /// The color support for Chalk.
    /// By default, color support is automatically detected based on the environment.
    abstract level: Level with get, set

    /// Use RGB values to set text color.
    abstract rgb: r: float * g: float * b: float -> Chalk

    /// Use HSL values to set text color.
    abstract hsl: h: float * s: float * l: float -> Chalk

    /// Use HSV values to set text color.
    abstract hsv: h: float * s: float * v: float -> Chalk

    /// Use HWB values to set text color.
    abstract hwb: h: float * w: float * b: float -> Chalk

    /// **Description**
    ///
    /// Use HEX value to set background color.
    ///
    /// Example: `chalk.bgHex('#DEADED');`
    ///
    /// **Parameters**
    ///   * `color` - parameter of type `string` - Hexadecimal value representing the desired color.
    ///
    /// **Output Type**
    ///   * `Chalk`
    ///
    /// **Exceptions**
    ///
    abstract bgHex: color: string -> Chalk

    /// **Description**
    ///
    /// Use keyword color value to set background color.
    ///
    /// Example: `chalk.bgKeyword('orange');`
    ///
    /// **Parameters**
    ///   * `color` - parameter of type `string` - Keyword value representing the desired color.
    ///
    /// **Output Type**
    ///   * `Chalk`
    ///
    /// **Exceptions**
    ///
    abstract bgKeyword: color: string -> Chalk

    /// Use RGB values to set background color.
    abstract bgRgb: r: float * g: float * b: float -> Chalk

    /// Use HSL values to set background color.
    abstract bgHsl: h: float * s: float * l: float -> Chalk

    /// Use HSV values to set background color.
    abstract bgHsv: h: float * s: float * v: float -> Chalk

    /// Use HWB values to set background color.
    abstract bgHwb: h: float * w: float * b: float -> Chalk

    /// **Description**
    ///
    /// Use HEX value to set text color.
    ///
    /// Example: `chalk.hex('#DEADED');`
    ///
    /// **Parameters**
    ///   * `color` - parameter of type `string` - Hexadecimal value representing the desired color.
    ///
    /// **Output Type**
    ///   * `Chalk`
    ///
    /// **Exceptions**
    ///
    abstract hex: color: string -> Chalk

    /// **Description**
    ///
    /// Use keyword color value to set text color.
    ///
    /// Example: `chalk.keyword('orange');`
    ///
    /// **Parameters**
    ///   * `color` - parameter of type `string` - Keyword value representing the desired color.
    ///
    /// **Output Type**
    ///   * `Chalk`
    ///
    /// **Exceptions**
    ///
    abstract keyword: color: string -> Chalk

    /// Modifier: Resets the current color chain.
    abstract reset: Chalk

    /// Modifier: Make text bold.
    abstract bold: Chalk

    /// Modifier: Emitting only a small amount of light.
    abstract dim: Chalk

    /// Modifier: Make text italic. (Not widely supported)
    abstract italic: Chalk

    /// Modifier: Make text underline. (Not widely supported)
    abstract underline: Chalk

    /// Modifier: Inverse background and foreground colors.
    abstract inverse: Chalk

    /// Modifier: Prints the text, but makes it invisible.
    abstract hidden: Chalk

    /// Modifier: Puts a horizontal line through the center of the text. (Not widely supported)
    abstract strikethrough: Chalk

    /// Modifier: Prints the text only when Chalk is enabled.
    /// Can be useful for things that are purely cosmetic.
    abstract visible: Chalk

    // Foregrounds

    abstract black: Chalk
    abstract red: Chalk
    abstract green: Chalk
    abstract yellow: Chalk
    abstract blue: Chalk
    abstract magenta: Chalk
    abstract cyan: Chalk
    abstract white: Chalk
    abstract gray: Chalk
    abstract grey: Chalk
    abstract blackBright: Chalk
    abstract redBright: Chalk
    abstract greenBright: Chalk
    abstract yellowBright: Chalk
    abstract blueBright: Chalk
    abstract magentaBright: Chalk
    abstract cyanBright: Chalk
    abstract whiteBright: Chalk

    // Backgrounds

    abstract bgBlack: Chalk
    abstract bgRed: Chalk
    abstract bgGreen: Chalk
    abstract bgYellow: Chalk
    abstract bgBlue: Chalk
    abstract bgMagenta: Chalk
    abstract bgCyan: Chalk
    abstract bgWhite: Chalk
    abstract bgBlackBright: Chalk
    abstract bgRedBright: Chalk
    abstract bgGreenBright: Chalk
    abstract bgYellowBright: Chalk
    abstract bgBlueBright: Chalk
    abstract bgMagentaBright: Chalk
    abstract bgCyanBright: Chalk
    abstract bgWhiteBright: Chalk
