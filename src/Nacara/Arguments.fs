namespace Nacara

open Argu

[<CliPrefix(CliPrefix.None)>]
type CliArguments =
    | Build
    | Watch
    | Serve
    | Version
    | Clean

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Build -> "Build the website."
            | Watch -> "Start the development server."
            | Serve -> "Serve the website locally."
            | Version -> "Print the version."
            | Clean -> "Clean the generated files."
