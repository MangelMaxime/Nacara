namespace Nacara.Plugins.Internal

open Nacara.Core

/// <summary>
/// Finds - or fetches - the esbuild binary.
/// </summary>
/// <remarks>
/// esbuild publishes no standalone downloads, but the npm registry is an ordinary HTTP server
/// serving tarballs, so the platform package is fetched with no npm or Node in sight. The binary
/// inside is statically linked, so there is no glibc build to tell from a musl one.
/// </remarks>
[<RequireQualifiedAccess>]
module EsbuildBinary =

    [<Literal>]
    let Version = "0.28.2"

    /// <summary>The npm package holding the binary for this machine, and the name inside it.</summary>
    let private request () =
        Tool.platform ()
        |> Result.bind (fun platform ->
            let name =
                if platform.IsLinux then
                    $"linux-%s{platform.Architecture}"
                elif platform.IsMacOS then
                    $"darwin-%s{platform.Architecture}"
                else
                    $"win32-%s{platform.Architecture}"

            let binary =
                if platform.IsWindows then
                    "esbuild.exe"
                else
                    "esbuild"

            Ok
                {
                    Name = "esbuild"
                    Version = Version
                    Url =
                        $"https://registry.npmjs.org/@esbuild/%s{name}/-/%s{name}-%s{Version}.tgz"
                    Archive = TarGzip
                    Files = [ binary ]
                    Executable = [ binary ]
                    Checksum = None
                }
        )

    /// <summary>The path of the binary, downloading it once if needed.</summary>
    let resolve () =
        request ()
        |> Result.bind (fun request -> Tool.file (List.head request.Files) request)
