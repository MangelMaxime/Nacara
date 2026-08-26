module EasyBuild.Commands.Format

open Spectre.Console.Cli
open SimpleExec
open BlackFox.CommandLine
open EasyBuild.Workspace

type FormatSettings() =
    inherit CommandSettings()

    [<CommandOption("-c|--check")>]
    member val Check = false with get, set

/// <summary>
/// Formats everything: Fantomas the F#, Biome the css and the javascript.
/// </summary>
/// <remarks>Biome is pinned in package.json, which is the only thing node is here for.</remarks>
type FormatCommand() =
    inherit Command<FormatSettings>()
    interface ICommandLimiter<FormatSettings>

    override _.Execute(_, settings, _) =
        Command.Run(
            "dotnet",
            CmdLine.empty
            |> CmdLine.appendRaw "fantomas"
            |> CmdLine.appendRaw "build"
            |> CmdLine.appendRaw "src"
            |> CmdLine.appendRaw "tests"
            |> CmdLine.appendRaw "docs"
            |> CmdLine.appendIf settings.Check "--check"
            |> CmdLine.toString,
            workingDirectory = root
        )

        Command.Run(
            "npx",
            CmdLine.empty
            |> CmdLine.appendRaw "biome"
            |> CmdLine.appendRaw "format"
            |> CmdLine.appendIf (not settings.Check) "--write"
            |> CmdLine.appendRaw "."
            |> CmdLine.toString,
            workingDirectory = root
        )

        0
