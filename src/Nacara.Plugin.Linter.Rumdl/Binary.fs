namespace Nacara.Plugins.Internal

open Nacara.Core

/// <summary>
/// Finds - or fetches - the rumdl binary.
/// </summary>
/// <remarks>
/// rumdl publishes a checksum beside every archive, so what was downloaded is checked before it is
/// ever run. The fetching and the cache are <see cref="T:Nacara.Core.Tool" />'s.
/// </remarks>
[<RequireQualifiedAccess>]
module RumdlBinary =

    /// <summary>The release this plugin was written against.</summary>
    [<Literal>]
    let Version = "0.2.58"

    /// <summary>Where those releases live, with <c>{version}</c> and <c>{target}</c> to fill in.</summary>
    [<Literal>]
    let Source =
        "https://github.com/rvben/rumdl/releases/download/v{version}/rumdl-v{version}-{target}"

    /// <summary>The release for this machine, as rumdl names them.</summary>
    let private request (source: string) =
        Tool.platform ()
        |> Result.bind (fun platform ->
            let architecture =
                if platform.Architecture = "arm64" then
                    "aarch64"
                else
                    "x86_64"

            let target =
                if platform.IsLinux then
                    // glibc is the common case; a musl machine says so with BinaryPath.
                    Ok $"%s{architecture}-unknown-linux-gnu.tar.gz"
                elif platform.IsMacOS then
                    Ok $"%s{architecture}-apple-darwin.tar.gz"
                elif platform.Architecture = "x64" then
                    Ok "x86_64-pc-windows-msvc.zip"
                else
                    // The one platform upstream does not build for.
                    Error
                        "rumdl publishes no build for Windows on Arm: point BinaryPath at one, or run the x64 build"

            target
            |> Result.map (fun target ->
                let url = source.Replace("{version}", Version).Replace("{target}", target)

                let binary =
                    if platform.IsWindows then
                        "rumdl.exe"
                    else
                        "rumdl"

                {
                    Name = "rumdl"
                    Version = Version
                    Url = url
                    Archive =
                        if target.EndsWith ".zip" then
                            Zip
                        else
                            TarGzip
                    Files = [ binary ]
                    Executable = [ binary ]
                    Checksum = Some(url + ".sha256")
                }
            )
        )

    /// <summary>The path of the binary, downloading it once if it is not already there.</summary>
    /// <param name="source">Where the release is published. Point it at a mirror to use one.</param>
    let resolve (source: string) =
        request source
        |> Result.bind (fun request -> Tool.file (List.head request.Files) request)
