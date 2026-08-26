namespace Nacara.Plugins.Internal

open System
open System.IO
open System.Runtime.InteropServices
open Nacara.Core

/// <summary>
/// Finds - or fetches - the two native libraries this plugin talks to.
/// </summary>
/// <remarks>
/// One package per platform, about 8 MB, fetched once per machine. They are looked for beside the
/// program first - see <c>TreeSitterOptions.RuntimePath</c> - and then in the cache.
/// </remarks>
[<RequireQualifiedAccess>]
module Runtime =

    /// <summary>The tree-sitter release these libraries are built from.</summary>
    [<Literal>]
    let Version = "0.26.12"

    /// <summary>Where they are published, with <c>{version}</c> and <c>{rid}</c> to fill in.</summary>
    [<Literal>]
    let Source =
        "https://registry.npmjs.org/@nacara/tree-sitter-runtime-{rid}/-/tree-sitter-runtime-{rid}-{version}.tgz"

    /// <summary>What this platform is called, and what a shared library is called on it.</summary>
    let private target () =
        let architecture =
            match RuntimeInformation.ProcessArchitecture with
            | Architecture.Arm64 -> Ok "arm64"
            | Architecture.X64 -> Ok "x64"
            | other -> Error $"The tree-sitter runtime has no build for %A{other}"

        architecture
        |> Result.bind (fun architecture ->
            if RuntimeInformation.IsOSPlatform OSPlatform.Linux then
                Ok($"linux-%s{architecture}", "libtree-sitter.so", "libwasmtime.so")
            elif RuntimeInformation.IsOSPlatform OSPlatform.OSX then
                Ok($"osx-%s{architecture}", "libtree-sitter.dylib", "libwasmtime.dylib")
            elif RuntimeInformation.IsOSPlatform OSPlatform.Windows then
                Ok($"win-%s{architecture}", "tree-sitter.dll", "wasmtime.dll")
            else
                Error "The tree-sitter runtime has no build for this operating system"
        )

    /// <summary>
    /// The directory holding both libraries, fetching them once if they are not there.
    /// </summary>
    /// <param name="source">Where the package is published. Point it at a mirror to use one.</param>
    /// <returns>The directory, or why it could not be had.</returns>
    let resolve (source: string) =
        match target () with
        | Error message -> Error message
        | Ok(rid, core, engine) ->
            let shipped = Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native")

            let holds (directory: string) =
                [
                    core
                    engine
                ]
                |> List.forall (fun name -> File.Exists(Path.Combine(directory, name)))

            if holds shipped then
                Ok shipped
            else
                Tool.resolve
                    {
                        Name = "tree-sitter-runtime"
                        Version = Version
                        Url = source.Replace("{version}", Version).Replace("{rid}", rid)
                        Archive = TarGzip
                        Files =
                            [
                                core
                                engine
                            ]
                        Executable = []
                        Checksum = None
                    }
