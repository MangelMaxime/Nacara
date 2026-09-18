/// <summary>
/// This repository's own site: the commands a contributor runs while writing it.
/// </summary>
/// <remarks>
/// A site is a program, so these forward to it. Watching goes through `dotnet watch` rather than
/// the site's own watcher, because a change to a layout or a plugin is a change to that program:
/// the site's watcher re-renders content, and this restarts the thing that renders it.
/// </remarks>
module EasyBuild.Commands.Docs

open System.ComponentModel
open Spectre.Console.Cli
open SimpleExec
open BlackFox.CommandLine
open EasyBuild.Tools.Npm
open EasyBuild.Workspace

/// <summary>What every one of these takes, and what it hands to the site.</summary>
/// <remarks>Anything written after <c>--</c> is handed to the site as it stands.</remarks>
type DocsSettings() =
    inherit CommandSettings()

    [<CommandOption("-p|--port <PORT>")>]
    [<Description("The port to serve on.")>]
    member val Port = 0 with get, set

    [<CommandOption("--strict")>]
    [<Description("Treat the site's warnings as errors.")>]
    member val Strict = false with get, set

    [<CommandOption("--verbose")>]
    [<Description("Log what the build is doing.")>]
    member val Verbose = false with get, set

    /// <summary>What the site is given, beyond the name of its command.</summary>
    abstract Arguments: CmdLine -> CmdLine

    default this.Arguments line =
        line
        |> CmdLine.appendPrefixIf (this.Port > 0) "--port" (string this.Port)
        |> CmdLine.appendIf this.Strict "--strict"
        |> CmdLine.appendIf this.Verbose "--verbose"

/// <summary>A build that can be published under a version prefix.</summary>
type VersionedSettings() =
    inherit DocsSettings()

    [<CommandOption("--version <VERSION>")>]
    [<Description("Build under a version prefix, for a site that serves several.")>]
    member val Version = "" with get, set

    override this.Arguments line =
        base.Arguments line
        |> CmdLine.appendPrefixIfNotNullOrEmpty "--version" this.Version

type CleanSettings() =
    inherit DocsSettings()

    [<CommandOption("--global")>]
    [<Description("Empty the shared cache of downloaded tools too.")>]
    member val Global = false with get, set

    override this.Arguments line =
        base.Arguments line |> CmdLine.appendIf this.Global "--global"

type WatchSettings() =
    inherit DocsSettings()

    [<CommandOption("--host [HOST]")>]
    [<Description("Listen on an address other than localhost. On its own, every interface.")>]
    member val Host = FlagValue<string>() with get, set

    [<CommandOption("--no-restart")>]
    [<Description("Serve without rebuilding the site when its own code changes.")>]
    member val NoRestart = false with get, set

    override this.Arguments line =
        base.Arguments line
        |> CmdLine.appendIf this.Host.IsSet "--host"
        |> CmdLine.appendIf (this.Host.IsSet && not (isNull this.Host.Value)) this.Host.Value

/// <summary>Runs the site with a command of its own, and what the flags asked for.</summary>
let private site
    (command: string)
    (watch: bool)
    (settings: DocsSettings)
    (context: CommandContext)
    =
    Npm.install Workspace.``.``

    let program, before =
        if watch then
            // Without this, dotnet watch hot-reloads the running site in place instead of restarting it.
            "dotnet",
            [
                "watch"
                "--no-hot-reload"
            ]
        else
            "dotnet", [ "run" ]

    let arguments =
        before
        |> List.fold (fun line argument -> CmdLine.appendRaw argument line) CmdLine.empty
        |> CmdLine.appendPrefix "--project" Workspace.docs.``Docs.fsproj``
        |> CmdLine.appendRaw "--"
        |> CmdLine.appendRaw command
        |> settings.Arguments
        |> CmdLine.appendSeq context.Remaining.Raw
        |> CmdLine.toString

    Command.Run(program, arguments, workingDirectory = Workspace.``.``)
    0

type BuildCommand() =
    inherit Command<VersionedSettings>()
    interface ICommandLimiter<CommandSettings>

    override _.Execute(context, settings, _) = site "build" false settings context

type CheckCommand() =
    inherit Command<DocsSettings>()
    interface ICommandLimiter<CommandSettings>

    override _.Execute(context, settings, _) = site "check" false settings context

type CleanCommand() =
    inherit Command<CleanSettings>()
    interface ICommandLimiter<CommandSettings>

    override _.Execute(context, settings, _) = site "clean" false settings context

type DeployCommand() =
    inherit Command<VersionedSettings>()
    interface ICommandLimiter<CommandSettings>

    override _.Execute(context, settings, _) = site "gh-pages" false settings context

type WatchCommand() =
    inherit Command<WatchSettings>()
    interface ICommandLimiter<CommandSettings>

    override _.Execute(context, settings, _) =
        site "watch" (not settings.NoRestart) settings context
