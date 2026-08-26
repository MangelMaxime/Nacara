/// <summary>Pushes every package to nuget.org, in the order a consumer can restore from.</summary>
module EasyBuild.Commands.Release

open System
open System.IO
open Spectre.Console.Cli
open EasyBuild.Tools.DotNet
open EasyBuild.Tools.Npm
open Nacara.Core
open EasyBuild.Workspace

let private packages =
    [
        Workspace.src.``Nacara.Core``.``.``
        Workspace.src.``Nacara.Plugin.Assets.Esbuild``.``.``
        Workspace.src.``Nacara.Plugin.Assets.LightningCss``.``.``
        Workspace.src.``Nacara.Plugin.Assets.Nuglify``.``.``
        Workspace.src.``Nacara.Plugin.Changelog``.``.``
        Workspace.src.``Nacara.Plugin.FSharpApi``.``.``
        Workspace.src.``Nacara.Plugin.Highlight.TextMate``.``.``
        Workspace.src.``Nacara.Plugin.Highlight.TreeSitter``.``.``
        Workspace.src.``Nacara.Plugin.LinkValidator``.``.``
        Workspace.src.``Nacara.Plugin.Linter.Rumdl``.``.``
        Workspace.src.``Nacara.Plugin.Literate``.``.``
        Workspace.src.``Nacara.Plugin.Markdown``.``.``
        Workspace.src.``Nacara.Plugin.Search``.``.``
        Workspace.src.``Nacara.Plugin.Sitemap``.``.``
        Workspace.src.``Nacara.Plugin.Versions``.``.``
        Workspace.src.``Nacara.Theme.Default``.``.``
        Workspace.src.``Nacara.Plugin.LiveExample``.``.``
        Workspace.templates.``.``
    ]

type ReleaseSettings() =
    inherit CommandSettings()

/// <summary>Packs and pushes each package, in order.</summary>
type ReleaseCommand() =
    inherit Command<ReleaseSettings>()
    interface ICommandLimiter<CommandSettings>

    override _.Execute(_, _, _) =
        let apiKey = Environment.GetEnvironmentVariable "NUGET_KEY"

        if String.IsNullOrWhiteSpace apiKey then
            Log.error "NUGET_KEY is not set"
            1
        else
            Npm.install Workspace.``.``

            packages
            |> List.iter (fun package ->
                let nupkg = DotNet.pack package

                DotNet.nugetPush (nupkg, apiKey = apiKey, skipDuplicate = true)

                Log.success $"pushed %s{Path.GetFileName package}"
            )

            0
