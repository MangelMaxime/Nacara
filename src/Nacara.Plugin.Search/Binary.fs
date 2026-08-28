namespace Nacara.Plugins.Internal

open Nacara.Core

/// <summary>
/// Finds - or fetches - the Pagefind binary.
/// </summary>
/// <remarks>
/// The fetching, the cache and the unpacking are <see cref="T:Nacara.Core.Tool" />'s. What is here
/// is the name pagefind gives its releases.
/// </remarks>
[<RequireQualifiedAccess>]
module Pagefind =

    [<Literal>]
    let Version = "1.5.2"

    /// <summary>The release for this machine, as pagefind names them.</summary>
    let private request () =
        Tool.platform ()
        |> Result.bind (fun platform ->
            let architecture =
                if platform.Architecture = "arm64" then
                    "aarch64"
                else
                    "x86_64"

            let archive, binary =
                if platform.IsLinux then
                    // musl, so a build does not depend on the glibc of the machine it runs on.
                    $"pagefind-v%s{Version}-%s{architecture}-unknown-linux-musl.tar.gz", "pagefind"
                elif platform.IsMacOS then
                    $"pagefind-v%s{Version}-%s{architecture}-apple-darwin.tar.gz", "pagefind"
                else
                    $"pagefind-v%s{Version}-%s{architecture}-pc-windows-msvc.zip", "pagefind.exe"

            Ok
                {
                    Name = "pagefind"
                    Version = Version
                    Url =
                        $"https://github.com/Pagefind/pagefind/releases/download/v%s{Version}/%s{archive}"
                    Archive =
                        if archive.EndsWith ".zip" then
                            Zip
                        else
                            TarGzip
                    Files = [ binary ]
                    Executable = [ binary ]
                    Checksum = None
                }
        )

    /// <summary>The path of the binary, downloading it once if needed.</summary>
    let resolve () =
        request ()
        |> Result.bind (fun request -> Tool.file (List.head request.Files) request)
