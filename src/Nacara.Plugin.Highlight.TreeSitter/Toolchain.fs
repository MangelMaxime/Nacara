namespace Nacara.Plugins.Internal

open System
open System.Diagnostics
open System.Formats.Tar
open System.IO
open System.IO.Compression
open System.Net.Http
open System.Runtime.InteropServices
open Nacara.Core

/// <summary>
/// Turns a grammar's repository into the two files a highlighter reads.
/// </summary>
/// <remarks>
/// A site names a grammar - a repository and a commit - and this fetches the sources, compiles them
/// to wasm and keeps the result, fetching the tree-sitter CLI and the wasi-sdk it needs along the
/// way. Nothing is expected to be installed and nothing installed is used. Cached per repository
/// and commit, so it happens once.
/// </remarks>
[<RequireQualifiedAccess>]
module Toolchain =

    /// <summary>The tree-sitter CLI this drives, which is the version the runtime is built from.</summary>
    [<Literal>]
    let Version = Runtime.Version

    /// <summary>Where those releases live, with <c>{version}</c> and <c>{platform}</c> to fill in.</summary>
    [<Literal>]
    let CliSource =
        "https://github.com/tree-sitter/tree-sitter/releases/download/v{version}/tree-sitter-{platform}.gz"

    /// <summary>The wasi-sdk that CLI compiles with, which pins its own.</summary>
    [<Literal>]
    let WasiSdkVersion = "29.0"

    /// <summary>Where those releases live, with <c>{version}</c> and <c>{platform}</c> to fill in.</summary>
    [<Literal>]
    let WasiSdkSource =
        "https://github.com/WebAssembly/wasi-sdk/releases/download/wasi-sdk-{major}/wasi-sdk-{version}-{platform}.tar.gz"

    /// Building a grammar twice at once would have each write what the other is reading.
    let private gate = obj ()

    let private root =
        Path.Combine(
            Environment.GetFolderPath Environment.SpecialFolder.UserProfile,
            ".cache",
            "nacara",
            "tree-sitter"
        )

    let private windows = RuntimeInformation.IsOSPlatform OSPlatform.Windows

    /// <summary>What the CLI's releases call this platform.</summary>
    let private cliPlatform () =
        let architecture =
            match RuntimeInformation.ProcessArchitecture with
            | Architecture.Arm64 -> Ok "arm64"
            | Architecture.X64 -> Ok "x64"
            | other -> Error $"The tree-sitter CLI has no build for %A{other}"

        architecture
        |> Result.bind (fun architecture ->
            if RuntimeInformation.IsOSPlatform OSPlatform.Linux then
                Ok $"linux-%s{architecture}"
            elif RuntimeInformation.IsOSPlatform OSPlatform.OSX then
                Ok $"macos-%s{architecture}"
            elif windows then
                Ok $"windows-%s{architecture}"
            else
                Error "The tree-sitter CLI has no build for this operating system"
        )

    /// <summary>What the wasi-sdk's releases call this platform, which is not the same thing.</summary>
    let private sdkPlatform () =
        let architecture =
            match RuntimeInformation.ProcessArchitecture with
            | Architecture.Arm64 -> Ok "arm64"
            | Architecture.X64 -> Ok "x86_64"
            | other -> Error $"The wasi-sdk has no build for %A{other}"

        architecture
        |> Result.bind (fun architecture ->
            if RuntimeInformation.IsOSPlatform OSPlatform.Linux then
                Ok $"%s{architecture}-linux"
            elif RuntimeInformation.IsOSPlatform OSPlatform.OSX then
                Ok $"%s{architecture}-macos"
            elif windows then
                Ok $"%s{architecture}-windows"
            else
                Error "The wasi-sdk has no build for this operating system"
        )

    /// <summary>Everything a download does the same way: to one side, then into place.</summary>
    let private staged (directory: string) (write: string -> unit) =
        let staging = directory + ".part"

        if Directory.Exists staging then
            Directory.Delete(staging, true)

        Directory.CreateDirectory staging |> ignore

        try
            write staging
            Directory.CreateDirectory(Path.GetDirectoryName directory) |> ignore
            Directory.Move(staging, directory)
        with _ ->
            if Directory.Exists staging then
                Directory.Delete(staging, true)

            reraise ()

    let private http = new HttpClient(Timeout = TimeSpan.FromMinutes 30.0)

    let private read (url: string) =
        let response =
            http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult()

        response.EnsureSuccessStatusCode() |> ignore
        response.Content.ReadAsStream()

    /// <summary>Marks a file executable, which a tar carries and a copy does not.</summary>
    let private runnable (path: string) =
        if not windows then
            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead
                ||| UnixFileMode.UserWrite
                ||| UnixFileMode.UserExecute
                ||| UnixFileMode.GroupRead
                ||| UnixFileMode.GroupExecute
                ||| UnixFileMode.OtherRead
                ||| UnixFileMode.OtherExecute
            )

    /// <summary>
    /// Unpacks a gzipped tar, dropping the single directory the archive is wrapped in.
    /// </summary>
    let private unpack (stream: Stream) (destination: string) (strip: bool) =
        use gzip = new GZipStream(stream, CompressionMode.Decompress)
        use archive = new TarReader(gzip)
        let mutable entry = archive.GetNextEntry()

        while not (isNull entry) do
            let relative =
                let name = entry.Name.Replace('\\', '/').TrimStart('/')

                if strip then
                    match name.IndexOf '/' with
                    | -1 -> ""
                    | cut -> name.Substring(cut + 1)
                else
                    name

            if relative <> "" then
                let path = Path.Combine(destination, relative)

                match entry.EntryType with
                | TarEntryType.Directory -> Directory.CreateDirectory path |> ignore
                | TarEntryType.RegularFile
                | TarEntryType.V7RegularFile ->
                    Directory.CreateDirectory(Path.GetDirectoryName path) |> ignore
                    entry.ExtractToFile(path, true)

                    if not windows && entry.Mode.HasFlag UnixFileMode.UserExecute then
                        runnable path
                | TarEntryType.SymbolicLink when not windows ->
                    Directory.CreateDirectory(Path.GetDirectoryName path) |> ignore

                    if not (File.Exists path) then
                        File.CreateSymbolicLink(path, entry.LinkName) |> ignore
                | _ -> ()

            entry <- archive.GetNextEntry()

    /// <summary>The CLI, fetched once.</summary>
    let cli (source: string) =
        match cliPlatform () with
        | Error message -> Error message
        | Ok platform ->
            let binary =
                if windows then
                    "tree-sitter.exe"
                else
                    "tree-sitter"

            Tool.file
                binary
                {
                    Name = "tree-sitter/cli"
                    Version = Version
                    Url = source.Replace("{version}", Version).Replace("{platform}", platform)
                    Archive = Gzip
                    Files = [ binary ]
                    Executable = [ binary ]
                    Checksum = None
                }

    /// <summary>The compiler the CLI needs, fetched once.</summary>
    let wasiSdk (source: string) =
        match sdkPlatform () with
        | Error message -> Error message
        | Ok platform ->
            let directory = Path.Combine(root, "wasi-sdk", WasiSdkVersion)

            let clang =
                Path.Combine(
                    directory,
                    "bin",
                    if windows then
                        "clang.exe"
                    else
                        "clang"
                )

            if File.Exists clang then
                Ok directory
            else
                try
                    Log.info
                        $"Downloading the wasi-sdk %s{WasiSdkVersion} for %s{platform} - this is large, and happens once"

                    let major = WasiSdkVersion.Split('.')[0]

                    let url =
                        source
                            .Replace("{major}", major)
                            .Replace("{version}", WasiSdkVersion)
                            .Replace("{platform}", platform)

                    staged
                        directory
                        (fun staging ->
                            use stream = read url
                            unpack stream staging true
                        )

                    Ok directory
                with exn ->
                    Error $"Could not download the wasi-sdk: %s{exn.Message}"

    /// <summary>A grammar named rather than shipped: where it lives, and which commit of it.</summary>
    type Request =
        {
            Language: string
            /// The repository, as its web address.
            Repository: string
            /// A branch, a tag or a commit.
            Reference: string
            /// Which directory of it holds the grammar, when it is not the top one.
            Subdirectory: string option
            /// Which file says what its nodes mean, when it is not where they usually are.
            Queries: string option
        }

    /// <summary>A name for the pair of files this builds, that says what they were built from.</summary>
    let private slug (request: Request) =
        [
            request.Repository.TrimEnd('/').Split('/')
            |> Array.tryLast
            |> Option.defaultValue "grammar"
            request.Reference
            match request.Subdirectory with
            | Some subdirectory -> subdirectory
            | None -> ()
        ]
        |> String.concat "-"
        |> String.map (fun character ->
            if Char.IsLetterOrDigit character || character = '-' || character = '.' then
                character
            else
                '-'
        )

    /// <summary>Where a repository keeps its licence, which travels with what is built from it.</summary>
    let private licenceIn (directory: string) =
        let parent = Path.GetDirectoryName directory

        [
            for place in
                [
                    directory
                    parent
                ] do
                for name in
                    [
                        "LICENSE"
                        "LICENSE.md"
                        "LICENSE.txt"
                        "COPYING"
                    ] do
                    Path.Combine(place, name)
        ]
        |> List.tryFind File.Exists

    /// <summary>Where a repository keeps the queries, in the order they are usually kept.</summary>
    let private queriesIn (directory: string) (language: string) (said: string option) =
        match said with
        | Some path -> [ Path.Combine(directory, path) ]
        | None ->
            let parent = Path.GetDirectoryName directory

            [
                Path.Combine(directory, "queries", "highlights.scm")
                Path.Combine(directory, "queries", language, "highlights.scm")
                Path.Combine(parent, "queries", "highlights.scm")
                Path.Combine(parent, "queries", language, "highlights.scm")
            ]
        |> List.tryFind File.Exists

    /// <summary>Runs the CLI, with the compiler it needs said outright rather than downloaded again.</summary>
    let private compile (cli: string) (sdk: string) (sources: string) (output: string) =
        let arguments =
            ProcessStartInfo(cli, RedirectStandardError = true, RedirectStandardOutput = true)

        arguments.ArgumentList.Add "build"
        arguments.ArgumentList.Add "--wasm"
        arguments.ArgumentList.Add "--output"
        arguments.ArgumentList.Add output
        arguments.ArgumentList.Add sources
        // Without this the CLI reaches for curl and downloads its own copy of what we just fetched.
        arguments.Environment["TREE_SITTER_WASI_SDK_PATH"] <- sdk

        use running = Process.Start arguments
        let complaint = running.StandardError.ReadToEnd()
        running.WaitForExit()

        if running.ExitCode = 0 && File.Exists output then
            Ok()
        else
            Error(complaint.Trim())

    /// <summary>
    /// The two files of a grammar, built from its repository the first time and kept after.
    /// </summary>
    /// <param name="request">Which grammar, and which commit of it.</param>
    /// <param name="allowBuild">Whether a grammar not already built may be.</param>
    /// <param name="cliSource">Where the CLI is published.</param>
    /// <param name="sdkSource">Where the wasi-sdk is published.</param>
    /// <returns>The wasm and the queries, or why they could not be had.</returns>
    let ensure (request: Request) (allowBuild: bool) (cliSource: string) (sdkSource: string) =
        lock
            gate
            (fun () ->
                let directory = Path.Combine(root, "grammars", slug request)
                let wasm = Path.Combine(directory, "grammar.wasm.gz")
                let queries = Path.Combine(directory, "highlights.scm")

                if File.Exists wasm && File.Exists queries then
                    Ok(wasm, queries)
                elif not allowBuild then
                    Error
                        $"The grammar for '%s{request.Language}' is not in '%s{directory}' and building is disabled"
                else

                    try
                        Log.info
                            $"Building the %s{request.Language} grammar from %s{request.Repository}@%s{request.Reference}"

                        let sources = Path.Combine(root, "sources", slug request)

                        if not (Directory.Exists sources) then
                            // Branch, tag or commit: GitHub serves all three from the same address.
                            let url =
                                $"""%s{request.Repository.TrimEnd('/')}/archive/%s{request.Reference}.tar.gz"""

                            staged
                                sources
                                (fun staging ->
                                    use stream = read url
                                    unpack stream staging true
                                )

                        let grammar =
                            match request.Subdirectory with
                            | Some subdirectory -> Path.Combine(sources, subdirectory)
                            | None -> sources

                        match queriesIn grammar request.Language request.Queries with
                        | None ->
                            Error
                                $"No highlight queries in %s{request.Repository}: a grammar without them colours nothing"
                        | Some found ->
                            match cli cliSource with
                            | Error message -> Error message
                            | Ok cli ->
                                match wasiSdk sdkSource with
                                | Error message -> Error message
                                | Ok sdk ->
                                    let building =
                                        Path.Combine(root, "sources", slug request + ".wasm")

                                    match compile cli sdk grammar building with
                                    | Error complaint ->
                                        Error
                                            $"The %s{request.Language} grammar did not compile: %s{complaint}"
                                    | Ok() ->
                                        staged
                                            directory
                                            (fun staging ->
                                                use built = File.OpenRead building

                                                use file =
                                                    File.Create(
                                                        Path.Combine(staging, "grammar.wasm.gz")
                                                    )

                                                use gzip =
                                                    new GZipStream(
                                                        file,
                                                        CompressionLevel.SmallestSize
                                                    )

                                                built.CopyTo gzip
                                                gzip.Dispose()

                                                File.Copy(
                                                    found,
                                                    Path.Combine(staging, "highlights.scm")
                                                )

                                                match licenceIn grammar with
                                                | Some licence ->
                                                    File.Copy(
                                                        licence,
                                                        Path.Combine(staging, "LICENSE")
                                                    )
                                                | None -> ()
                                            )

                                        File.Delete building

                                        Directory.Delete(sources, true)
                                        Ok(wasm, queries)
                    with exn ->
                        Error $"Could not build the %s{request.Language} grammar: %s{exn.Message}"
            )
