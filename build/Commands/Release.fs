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
        "src/Nacara.Core"
        "src/Nacara.Plugin.Assets.Esbuild"
        "src/Nacara.Plugin.Assets.LightningCss"
        "src/Nacara.Plugin.Assets.Nuglify"
        "src/Nacara.Plugin.Changelog"
        "src/Nacara.Plugin.FSharpApi"
        "src/Nacara.Plugin.Highlight.TextMate"
        "src/Nacara.Plugin.Highlight.TreeSitter"
        "src/Nacara.Plugin.LinkValidator"
        "src/Nacara.Plugin.Linter.Rumdl"
        "src/Nacara.Plugin.Literate"
        "src/Nacara.Plugin.Markdown"
        "src/Nacara.Plugin.Search"
        "src/Nacara.Plugin.Sitemap"
        "src/Nacara.Plugin.Versions"
        "src/Nacara.Theme.Default"
        "src/Nacara.Plugin.LiveExample"
        "templates"
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
            // The theme and the live example bundle their JavaScript while they pack.
            Npm.install root

            packages
            |> List.iter (fun package ->
                let nupkg = DotNet.pack (Path.Combine(root, package))

                DotNet.nugetPush (nupkg, apiKey = apiKey, skipDuplicate = true)

                Log.success $"pushed %s{Path.GetFileName package}"
            )

            0
