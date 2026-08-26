module EasyBuild.Commands.Test

open Spectre.Console.Cli
open SimpleExec
open BlackFox.CommandLine
open EasyBuild.Tools.Npm
open EasyBuild.Workspace

type TestSettings() =
    inherit CommandSettings()

    [<CommandOption("-u|--update-snapshots")>]
    member val UpdateSnapshots = false with get, set

/// <summary>Runs the test suite, which is a program rather than a test runner.</summary>
type TestCommand() =
    inherit Command<TestSettings>()
    interface ICommandLimiter<TestSettings>

    override _.Execute(_, settings, _) =
        // The theme and the live example bundle their JavaScript while they build.
        Npm.install root

        Command.Run(
            "dotnet",
            CmdLine.empty
            |> CmdLine.appendRaw "run"
            |> CmdLine.appendPrefix "--project" tests
            |> CmdLine.toString,
            workingDirectory = root,
            configureEnvironment =
                fun environment ->
                    if settings.UpdateSnapshots then
                        environment["UPDATE_SNAPSHOTS"] <- "1"
        )

        0
