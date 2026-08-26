namespace Nacara.Core

open System
open System.Formats.Tar
open System.IO
open System.IO.Compression
open System.Net.Http
open System.Runtime.InteropServices
open System.Security.Cryptography

/// <summary>What the machine a site is being built on is called.</summary>
/// <remarks>
/// Everyone names a platform differently - <c>x86_64-unknown-linux-musl</c>, <c>linux-x64</c>,
/// <c>darwin-arm64</c> - so this gives you the parts and your plugin writes the name it needs from
/// them.
/// </remarks>
type ToolPlatform =
    {
        /// The .NET runtime identifier: <c>linux-x64</c>, <c>osx-arm64</c>, <c>win-x64</c>.
        Rid: string
        /// <c>x64</c> or <c>arm64</c>.
        Architecture: string
        IsLinux: bool
        IsMacOS: bool
        IsWindows: bool
    }

/// <summary>How a download is packed.</summary>
type ToolArchive =
    /// A <c>.tar.gz</c> holding the files.
    | TarGzip
    /// A <c>.zip</c> holding the files.
    | Zip
    /// One file, gzipped: the download is the program.
    | Gzip
    /// The download is the file, as it is.
    | Raw

/// <summary>A program a plugin needs, and where to get it.</summary>
/// <remarks>
/// Half the plugins here drive something they did not write - pagefind, lightningcss, rumdl - and
/// every one of them was fetching it the same way. Say what to fetch and what it is called
/// afterwards, and <see cref="T:Nacara.Core.Tool" /> does the fetching.
/// </remarks>
type ToolRequest =
    {
        /// What it is called, in the cache path and in what the build says.
        Name: string
        /// Pinned, so a build is the same tomorrow. Part of the cache path.
        Version: string
        /// Where the download is.
        Url: string
        /// How that download is packed.
        Archive: ToolArchive
        /// <summary>The files that must be there afterwards, by name.</summary>
        /// <remarks>They are found wherever the archive keeps them - an npm tarball puts everything
        /// under <c>package/</c> - and left in one directory, so you ask for a name rather than a
        /// path.</remarks>
        Files: string list
        /// Which of them are programs, and so need the executable bit on Unix.
        Executable: string list
        /// <summary>Where the checksum of the download is published, when it is.</summary>
        /// <remarks>Verified before anything is unpacked. A tool that publishes none is fetched
        /// as it is.</remarks>
        Checksum: string option
    }

/// <summary>
/// Fetches the programs plugins drive, once per machine.
/// </summary>
/// <remarks>
/// One pinned version, cached under the user's profile, fetched only when it is not already there,
/// unpacked whatever it is packed in, and marked executable. What differs between plugins is a URL
/// and a file name, which is what <see cref="T:Nacara.Core.ToolRequest" /> carries.
/// </remarks>
[<RequireQualifiedAccess>]
module Tool =

    /// Two builds may ask at once - a watch and the build it is serving.
    let private gate = obj ()

    let private http = new HttpClient(Timeout = TimeSpan.FromMinutes 15.0)

    /// <summary>What this machine is, or why nothing can be fetched for it.</summary>
    let platform () =
        let architecture =
            match RuntimeInformation.ProcessArchitecture with
            | Architecture.Arm64 -> Ok "arm64"
            | Architecture.X64 -> Ok "x64"
            | other -> Error $"There are no builds for %A{other}"

        architecture
        |> Result.bind (fun architecture ->
            let linux = RuntimeInformation.IsOSPlatform OSPlatform.Linux
            let macOS = RuntimeInformation.IsOSPlatform OSPlatform.OSX
            let windows = RuntimeInformation.IsOSPlatform OSPlatform.Windows

            let os =
                if linux then
                    Ok "linux"
                elif macOS then
                    Ok "osx"
                elif windows then
                    Ok "win"
                else
                    Error "There are no builds for this operating system"

            os
            |> Result.map (fun os ->
                {
                    Rid = $"%s{os}-%s{architecture}"
                    Architecture = architecture
                    IsLinux = linux
                    IsMacOS = macOS
                    IsWindows = windows
                }
            )
        )

    /// <summary>Where a tool of this version lives, whether or not it is there yet.</summary>
    /// <param name="request">The tool being asked about.</param>
    let directory (request: ToolRequest) =
        Path.Combine(
            Environment.GetFolderPath Environment.SpecialFolder.UserProfile,
            ".cache",
            "nacara",
            request.Name,
            request.Version
        )

    let private runnable (path: string) =
        if not (RuntimeInformation.IsOSPlatform OSPlatform.Windows) then
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

    /// <summary>What the publisher says the download should be, when it says.</summary>
    let private verify (url: string) (archive: byte array) =
        let expected =
            try
                let text = http.GetStringAsync(url).GetAwaiter().GetResult()
                let first = text.Trim().Split(' ')
                Some(first[0].ToLowerInvariant())
            with _ ->
                None

        match expected with
        | None -> Ok()
        | Some expected ->
            let actual = SHA256.HashData archive |> Convert.ToHexString |> _.ToLowerInvariant()

            if actual = expected then
                Ok()
            else
                Error "the download does not match its published checksum"

    /// <summary>Unpacks a download into a directory, whatever it is packed in.</summary>
    let private unpack (request: ToolRequest) (archive: byte array) (destination: string) =
        Directory.CreateDirectory destination |> ignore
        use memory = new MemoryStream(archive)

        match request.Archive with
        | Zip ->
            use zip = new ZipArchive(memory, ZipArchiveMode.Read)
            zip.ExtractToDirectory(destination, true)
        | TarGzip ->
            use gzip = new GZipStream(memory, CompressionMode.Decompress)
            TarFile.ExtractToDirectory(gzip, destination, true)
        | Gzip ->
            let name = request.Files |> List.tryHead |> Option.defaultValue request.Name
            use gzip = new GZipStream(memory, CompressionMode.Decompress)
            use file = File.Create(Path.Combine(destination, name))
            gzip.CopyTo file
        | Raw ->
            let name = request.Files |> List.tryHead |> Option.defaultValue request.Name
            File.WriteAllBytes(Path.Combine(destination, name), archive)

    /// <summary>Brings the wanted files up to the top, wherever the archive kept them.</summary>
    let private flatten (request: ToolRequest) (destination: string) =
        for name in request.Files do
            let wanted = Path.Combine(destination, name)

            if not (File.Exists wanted) then
                Directory.EnumerateFiles(destination, name, SearchOption.AllDirectories)
                |> Seq.tryHead
                |> Option.iter (fun found -> File.Move(found, wanted, true))

    /// <summary>
    /// The directory holding a tool, fetching it once if it is not already there.
    /// </summary>
    /// <remarks>What is already in the cache is used as it is, so this reaches the network once per
    /// machine and version rather than once per build. A site that would rather drive its own copy
    /// says where it is, and then none of this runs.</remarks>
    /// <param name="request">What to fetch, and what it is called afterwards.</param>
    /// <returns>The directory the files are in, or why they could not be had.</returns>
    let resolve (request: ToolRequest) =
        lock
            gate
            (fun () ->
                let directory = directory request

                let present () =
                    request.Files
                    |> List.forall (fun name -> File.Exists(Path.Combine(directory, name)))

                if present () then
                    Ok directory
                else

                    try
                        Log.info $"Downloading %s{request.Name} %s{request.Version}"
                        let archive = http.GetByteArrayAsync(request.Url).GetAwaiter().GetResult()

                        let verified =
                            match request.Checksum with
                            | Some url -> verify url archive
                            | None -> Ok()

                        match verified with
                        | Error message ->
                            Error $"Could not download %s{request.Name}: %s{message}"
                        | Ok() ->
                            // Moved into place, so nothing ever sees a half-written program.
                            let staging = directory + ".part"

                            if Directory.Exists staging then
                                Directory.Delete(staging, true)

                            Directory.CreateDirectory(Path.GetDirectoryName staging) |> ignore
                            unpack request archive staging
                            flatten request staging

                            let missing =
                                request.Files
                                |> List.filter (fun name ->
                                    not (File.Exists(Path.Combine(staging, name)))
                                )

                            if not (List.isEmpty missing) then
                                Directory.Delete(staging, true)

                                let names = String.Join(", ", missing)
                                Error $"'%s{request.Url}' did not contain %s{names}"
                            else

                                for name in request.Executable do
                                    runnable (Path.Combine(staging, name))

                                Directory.CreateDirectory(Path.GetDirectoryName directory)
                                |> ignore

                                if Directory.Exists directory then
                                    Directory.Delete(directory, true)

                                Directory.Move(staging, directory)
                                Ok directory
                    with exn ->
                        Error $"Could not download %s{request.Name}: %s{exn.Message}"
            )

    /// <summary>The path of one file of a tool, fetching it once if it is not already there.</summary>
    /// <param name="name">Which of its files - usually the program itself.</param>
    /// <param name="request">What to fetch.</param>
    let file (name: string) (request: ToolRequest) =
        resolve request |> Result.map (fun directory -> Path.Combine(directory, name))
