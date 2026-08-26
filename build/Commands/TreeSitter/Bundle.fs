/// <summary>
/// Builds the grammars that ship inside Nacara.Plugin.Highlight.TreeSitter.
/// </summary>
/// <remarks>
/// The package carries the languages a documentation site is written in, so nothing is compiled
/// the first time somebody builds one. This puts them there, through the same code a site uses to
/// build a grammar of its own - one way of building a grammar, not two. Run it when a grammar is
/// worth updating, and commit what it writes.
/// </remarks>
module EasyBuild.Commands.TreeSitter.Bundle

open System.ComponentModel
open System.IO
open Spectre.Console.Cli
open Nacara.Core
open Nacara.Plugins.Internal
open EasyBuild.Workspace

/// <summary>A language the package ships, and the commit it is built from.</summary>
type Bundled =
    {
        Language: string
        Repository: string
        Reference: string
        Subdirectory: string option
    }

let private from language repository reference =
    {
        Language = language
        Repository = repository
        Reference = reference
        Subdirectory = None
    }

let private inDirectory subdirectory bundled =
    { bundled with
        Subdirectory = Some subdirectory
    }

/// <summary>
/// What ships.
/// </summary>
let private grammars =
    [
        from
            "fsharp"
            "https://github.com/MangelMaxime/tree-sitter-fsharp"
            "dd0f511f2a5e33daa27c4a0f72e288c78f587f14"
        from
            "csharp"
            "https://github.com/tree-sitter/tree-sitter-c-sharp"
            "9150f7d56bb47f1a809fa23623f1ba1413e93fa9"
        from
            "bash"
            "https://github.com/tree-sitter/tree-sitter-bash"
            "a06c2e4415e9bc0346c6b86d401879ffb44058f7"
        from
            "json"
            "https://github.com/tree-sitter/tree-sitter-json"
            "254c42a6476413b776221e03982ac8ae159eeb72"
        from
            "yaml"
            "https://github.com/tree-sitter-grammars/tree-sitter-yaml"
            "a1c4812a73ec5e089de8e441fdea3a921e8d5079"
        from
            "toml"
            "https://github.com/tree-sitter-grammars/tree-sitter-toml"
            "64b56832c2cffe41758f28e05c756a3a98d16f41"
        from
            "xml"
            "https://github.com/tree-sitter-grammars/tree-sitter-xml"
            "5000ae8f22d11fbe93939b05c1e37cf21117162d"
        |> inDirectory "xml"
        from
            "html"
            "https://github.com/tree-sitter/tree-sitter-html"
            "73a3947324f6efddf9e17c0ea58d454843590cc0"
        from
            "css"
            "https://github.com/tree-sitter/tree-sitter-css"
            "dda5cfc5722c429eaba1c910ca32c2c0c5bb1a3f"
        from
            "javascript"
            "https://github.com/tree-sitter/tree-sitter-javascript"
            "58404d8cf191d69f2674a8fd507bd5776f46cb11"
        from
            "typescript"
            "https://github.com/tree-sitter/tree-sitter-typescript"
            "75b3874edb2dc714fb1fd77a32013d0f8699989f"
        |> inDirectory "typescript"
        from
            "markdown"
            "https://github.com/tree-sitter-grammars/tree-sitter-markdown"
            "a0a00f817d02412bd92c54d316f164d827b57b5c"
        |> inDirectory "tree-sitter-markdown"
        from
            "python"
            "https://github.com/tree-sitter/tree-sitter-python"
            "26855eabccb19c6abf499fbc5b8dc7cc9ab8bc64"
        from
            "rust"
            "https://github.com/tree-sitter/tree-sitter-rust"
            "77a3747266f4d621d0757825e6b11edcbf991ca5"
        from
            "dart"
            "https://github.com/UserNobody14/tree-sitter-dart"
            "be07cf7118d3dba06236a3f19541685a68209934"
        from
            "php"
            "https://github.com/tree-sitter/tree-sitter-php"
            "3fda2fb9577166c6399834917f9844f30370beea"
        |> inDirectory "php"
        from
            "erlang"
            "https://github.com/WhatsApp/tree-sitter-erlang"
            "6ba4c762eb3065495e3db85697ffeecdf364ce35"
    ]

type BundleSettings() =
    inherit CommandSettings()

    [<CommandOption("-o|--output")>]
    [<Description("Where to put the grammars. Defaults to where the package reads them from.")>]
    member val Output: string = null with get, set

/// <summary>Builds every grammar the package ships, and writes what they came from.</summary>
type BundleCommand() =
    inherit Command<BundleSettings>()
    interface ICommandLimiter<CommandSettings>

    override _.Execute(_, settings, _) =
        let destination =
            if isNull settings.Output then
                Workspace.src.``Nacara.Plugin.Highlight.TreeSitter``.grammars.``.``
            else
                settings.Output

        Directory.CreateDirectory destination |> ignore

        let built =
            grammars
            |> List.map (fun bundled ->
                let result =
                    Toolchain.ensure
                        {
                            Language = bundled.Language
                            Repository = bundled.Repository
                            Reference = bundled.Reference
                            Subdirectory = bundled.Subdirectory
                            Queries = None
                        }
                        true
                        Toolchain.CliSource
                        Toolchain.WasiSdkSource

                match result with
                | Error message ->
                    Log.error $"%s{bundled.Language}: %s{message}"
                    None
                | Ok(wasm, queries) ->
                    let directory = Path.Combine(destination, bundled.Language)
                    Directory.CreateDirectory directory |> ignore
                    File.Copy(wasm, Path.Combine(directory, "grammar.wasm.gz"), true)
                    File.Copy(queries, Path.Combine(directory, "highlights.scm"), true)

                    let licence = Path.Combine(Path.GetDirectoryName wasm, "LICENSE")

                    if File.Exists licence then
                        File.Copy(licence, Path.Combine(directory, "LICENSE"), true)

                    let size = FileInfo(Path.Combine(directory, "grammar.wasm.gz")).Length
                    Log.success $"%-12s{bundled.Language} %6i{size / 1024L} KB"
                    Some(bundled, size)
            )
            |> List.choose id

        // What ships has to say where it came from, and under what terms.
        let notices =
            [
                yield "<!-- Written by the bundle command. Do not edit by hand. -->"
                yield "# Grammars shipped with this package"
                yield ""
                yield "Each is built from the commit named here, and carries the licence beside it."
                yield ""
                yield "| Language | Repository | Commit |"
                yield "|---|---|---|"
                for bundled, _ in built ->
                    $"| `%s{bundled.Language}` | [%s{bundled.Repository}](%s{bundled.Repository}) | `%s{bundled.Reference.Substring(0, 10)}` |"
            ]
            |> String.concat "\n"

        File.WriteAllText(Path.Combine(destination, "NOTICES.md"), notices + "\n")

        let total = built |> List.sumBy snd
        Log.success $"%i{List.length built} grammars, %i{total / 1024L} KB"

        if List.length built = List.length grammars then
            0
        else
            1
