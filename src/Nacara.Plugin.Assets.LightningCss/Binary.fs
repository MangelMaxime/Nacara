namespace Nacara.Plugins.Internal

open Nacara.Core

/// <summary>
/// Finds - or fetches - the Lightning CSS binary.
/// </summary>
/// <remarks>
/// Lightning CSS publishes no standalone downloads, but the npm registry is an ordinary HTTP server
/// serving tarballs, so the platform package is fetched with no npm or Node in sight. It parses CSS
/// rather than mangling text, which matters when the theme uses <c>@property</c>,
/// <c>color-mix()</c> and nesting - a minifier that does not understand those fails silently.
/// </remarks>
[<RequireQualifiedAccess>]
module LightningCssBinary =

    [<Literal>]
    let Version = "1.33.0"

    /// <summary>The npm package holding the binary for this machine, and the name inside it.</summary>
    let private request () =
        Tool.platform ()
        |> Result.bind (fun platform ->
            let package =
                if platform.IsLinux then
                    // The musl builds are published separately; this is the glibc one.
                    $"lightningcss-cli-linux-%s{platform.Architecture}-gnu"
                elif platform.IsMacOS then
                    $"lightningcss-cli-darwin-%s{platform.Architecture}"
                else
                    $"lightningcss-cli-win32-%s{platform.Architecture}-msvc"

            let binary =
                if platform.IsWindows then
                    "lightningcss.exe"
                else
                    "lightningcss"

            Ok
                {
                    Name = "lightningcss"
                    Version = Version
                    Url = $"https://registry.npmjs.org/%s{package}/-/%s{package}-%s{Version}.tgz"
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
