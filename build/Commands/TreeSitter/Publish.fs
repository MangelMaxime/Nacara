/// <summary>
/// Publishes the native runtimes to npm, one package per platform.
/// </summary>
/// <remarks>
/// <para>The plugin fetches the pair for the machine it runs on, so a site downloads one platform
/// rather than six. npm hosts them because it serves a tarball the moment it is published and
/// because these are versioned by tree-sitter rather than by any Nacara package - they are built
/// when tree-sitter moves, and never in step with a release.</para>
/// <para>Run it on the platform whose runtime is being published: the libraries have to exist
/// before they can be packed.</para>
/// </remarks>
module EasyBuild.Commands.TreeSitter.Publish

open System.ComponentModel
open System.IO
open System.Runtime.InteropServices
open Spectre.Console.Cli
open SimpleExec
open EasyBuild.Tools.Npm
open EasyBuild.Tools.PackageJson
open Nacara.Core
open EasyBuild.Workspace

module Plugin = Nacara.Plugins.Internal.Runtime

/// <summary>The scope these are published under.</summary>
[<Literal>]
let Scope = "@nacara"

/// <summary>What npm calls the platform a package is for, from what .NET calls it.</summary>
let private platformOf (rid: string) =
    let operatingSystem, architecture =
        match rid.Split '-' with
        | [| operatingSystem; architecture |] -> operatingSystem, architecture
        | _ -> failwith $"'%s{rid}' is not a runtime identifier"

    let npmOs =
        match operatingSystem with
        | "linux" -> "linux"
        | "osx" -> "darwin"
        | "win" -> "win32"
        | other -> failwith $"No npm platform is called '%s{other}'"

    npmOs, architecture

/// <summary>What npm needs to know about a package holding two libraries and nothing else.</summary>
let private manifest (rid: string) =
    let npmOs, architecture = platformOf rid

    $"""{{
    "name": "%s{Scope}/tree-sitter-runtime-%s{rid}",
    "version": "%s{Plugin.Version}",
    "description": "tree-sitter built with wasm support, and the wasmtime engine it uses, for %s{rid}. Fetched by Nacara.Plugin.Highlight.TreeSitter; there is nothing here for JavaScript to call.",
    "license": "MIT AND Apache-2.0",
    "repository": {{
        "type": "git",
        "url": "git+https://github.com/MangelMaxime/Nacara.git"
    }},
    "os": [ "%s{npmOs}" ],
    "cpu": [ "%s{architecture}" ]
}}
"""

type PublishSettings() =
    inherit CommandSettings()

    [<CommandOption("-r|--rid")>]
    [<Description("Which platform to publish. Defaults to the one this machine built.")>]
    member val Rid: string = null with get, set

    [<CommandOption("-d|--dry-run")>]
    [<Description("Pack it and say what would go, without publishing.")>]
    member val DryRun = false with get, set

/// <summary>Packs one platform's libraries into an npm package, and publishes it.</summary>
type PublishCommand() =
    inherit Command<PublishSettings>()
    interface ICommandLimiter<CommandSettings>

    override _.Execute(_, settings, _) =
        // The runtime directories are named after what .NET calls this machine.
        let rid =
            if isNull settings.Rid then
                RuntimeInformation.RuntimeIdentifier
            else
                settings.Rid

        let native =
            Path.Combine(
                VirtualWorkspace.src.``Nacara.Plugin.Highlight.TreeSitter``.runtimes.``.``,
                rid,
                "native"
            )

        if not (Directory.Exists native) then
            Log.error $"'%s{native}' is empty - build that platform's runtime first"
            1
        else

            let staging = Path.Combine(Path.GetTempPath(), $"tree-sitter-runtime-%s{rid}")

            if Directory.Exists staging then
                Directory.Delete(staging, true)

            Directory.CreateDirectory staging |> ignore

            for file in Directory.EnumerateFiles native do
                File.Copy(file, Path.Combine(staging, Path.GetFileName file), true)

            let packageJson = Path.Combine(staging, "package.json")
            File.WriteAllText(packageJson, manifest rid)

            let size =
                Directory.EnumerateFiles staging
                |> Seq.sumBy (fun file -> FileInfo(file).Length)

            Log.info
                $"%s{Scope}/tree-sitter-runtime-%s{rid} %s{Plugin.Version} (%i{size / 1024L} KB)"

            if settings.DryRun then
                Command.Run("npm", "pack --dry-run", workingDirectory = staging)
            elif PackageJson.needPublishing (FileInfo packageJson) then
                Npm.publish staging
            else
                // One platform failing is not a reason to republish the five that went out with it.
                Log.info "Already published, skipping"

            0
