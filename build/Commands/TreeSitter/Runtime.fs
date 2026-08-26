/// <summary>
/// Builds the native side of Nacara.Plugin.Highlight.TreeSitter, for this machine's platform.
/// </summary>
/// <remarks>
/// <para>tree-sitter can load a grammar compiled to wasm, but only when its C library was built
/// with <c>TREE_SITTER_FEATURE_WASM</c> - and nobody publishes such a build. So this makes one:
/// it fetches tree-sitter's sources and a wasmtime release, compiles the core against it, and
/// puts the pair where the plugin looks.</para>
/// <para>It builds for the machine it runs on: a cross-built library that loads a wasm store is
/// one nobody has run.</para>
/// </remarks>
module EasyBuild.Commands.TreeSitter.Runtime

open System
open System.ComponentModel
open System.Diagnostics
open System.Formats.Tar
open System.IO
open System.IO.Compression
open System.Net.Http
open System.Runtime.InteropServices
open Spectre.Console.Cli
open Nacara.Core
open EasyBuild.Workspace

module Plugin = Nacara.Plugins.Internal.Runtime

/// <summary>What wasmtime the core is built against, which tree-sitter pins in its lock file.</summary>
[<Literal>]
let WasmtimeVersion = "36.0.12"

let private windows = RuntimeInformation.IsOSPlatform OSPlatform.Windows

/// <summary>What this machine is called, by everyone who names it differently.</summary>
let private target () =
    let architecture =
        match RuntimeInformation.ProcessArchitecture with
        | Architecture.Arm64 -> "aarch64", "arm64"
        | Architecture.X64 -> "x86_64", "x64"
        | other -> failwith $"No runtime is built for %A{other}"

    let wasmtime, dotnet = architecture

    if RuntimeInformation.IsOSPlatform OSPlatform.Linux then
        {|
            Rid = $"linux-%s{dotnet}"
            Wasmtime = $"%s{wasmtime}-linux"
            Archive = "tar.xz"
            Core = "libtree-sitter.so"
            Engine = "libwasmtime.so"
        |}
    elif RuntimeInformation.IsOSPlatform OSPlatform.OSX then
        {|
            Rid = $"osx-%s{dotnet}"
            Wasmtime = $"%s{wasmtime}-macos"
            Archive = "tar.xz"
            Core = "libtree-sitter.dylib"
            Engine = "libwasmtime.dylib"
        |}
    elif windows then
        {|
            Rid = $"win-%s{dotnet}"
            Wasmtime = $"%s{wasmtime}-windows"
            Archive = "zip"
            Core = "tree-sitter.dll"
            Engine = "wasmtime.dll"
        |}
    else
        failwith "No runtime is built for this operating system"

let private http = new HttpClient(Timeout = TimeSpan.FromMinutes 10.0)

let private fetch (url: string) (destination: string) =
    Log.info url
    use stream = http.GetStreamAsync(url).GetAwaiter().GetResult()
    use file = File.Create destination
    stream.CopyTo file

/// <summary>Runs a program, and says what it said when it fails.</summary>
let private run (program: string) (arguments: string list) =
    let start =
        ProcessStartInfo(program, RedirectStandardError = true, RedirectStandardOutput = true)

    arguments |> List.iter start.ArgumentList.Add
    use running = Process.Start start
    let complaint = running.StandardError.ReadToEnd()
    running.StandardOutput.ReadToEnd() |> ignore
    running.WaitForExit()

    if running.ExitCode <> 0 then
        failwith $"%s{program} failed: %s{complaint.Trim()}"

/// <summary>Unpacks what upstream publishes, in the format it publishes it in.</summary>
let private unpack (archive: string) (into: string) =
    Directory.CreateDirectory into |> ignore

    if archive.EndsWith ".zip" then
        ZipFile.ExtractToDirectory(archive, into, true)
    elif archive.EndsWith ".tar.xz" then
        run
            "tar"
            [
                "xJf"
                archive
                "-C"
                into
            ]
    else
        use file = File.OpenRead archive
        use gzip = new GZipStream(file, CompressionMode.Decompress)
        TarFile.ExtractToDirectory(gzip, into, true)

type RuntimeSettings() =
    inherit CommandSettings()

    [<CommandOption("-o|--output")>]
    [<Description("Where to put the two libraries. Defaults to where the plugin looks for them.")>]
    member val Output: string = null with get, set

/// <summary>Builds tree-sitter and fetches wasmtime, for this machine.</summary>
type RuntimeCommand() =
    inherit Command<RuntimeSettings>()
    interface ICommandLimiter<CommandSettings>

    override _.Execute(_, settings, _) =
        let target = target ()
        let version = Plugin.Version

        let destination =
            if isNull settings.Output then
                Path.Combine(TreeSitter.runtimes, target.Rid, "native")
            else
                settings.Output

        let work = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
        Directory.CreateDirectory work |> ignore

        try
            Log.info $"tree-sitter %s{version} and wasmtime %s{WasmtimeVersion} for %s{target.Rid}"

            let sourcesArchive = Path.Combine(work, "tree-sitter.tar.gz")

            fetch
                $"https://github.com/tree-sitter/tree-sitter/archive/refs/tags/v%s{version}.tar.gz"
                sourcesArchive

            unpack sourcesArchive work
            let sources = Path.Combine(work, $"tree-sitter-%s{version}")

            let engineName = $"wasmtime-v%s{WasmtimeVersion}-%s{target.Wasmtime}-c-api"
            let engineArchive = Path.Combine(work, $"%s{engineName}.%s{target.Archive}")

            fetch
                $"https://github.com/bytecodealliance/wasmtime/releases/download/v%s{WasmtimeVersion}/%s{engineName}.%s{target.Archive}"
                engineArchive

            unpack engineArchive work
            let engine = Path.Combine(work, engineName)

            // wasm_store.c includes this from beside itself, and it ships one directory below.
            File.Copy(
                Path.Combine(sources, "lib", "src", "wasm", "stdlib-symbols.txt"),
                Path.Combine(sources, "lib", "src", "stdlib-symbols.txt"),
                true
            )

            Directory.CreateDirectory destination |> ignore
            let core = Path.Combine(destination, target.Core)
            let include' = Path.Combine(sources, "lib", "include")
            let internals = Path.Combine(sources, "lib", "src")
            let headers = Path.Combine(engine, "include")
            let library = Path.Combine(engine, "lib")
            let unit = Path.Combine(sources, "lib", "src", "lib.c")

            if windows then
                run
                    "cl.exe"
                    [
                        "/nologo"
                        "/O2"
                        "/LD"
                        "/DTREE_SITTER_FEATURE_WASM"
                        $"/I%s{include'}"
                        $"/I%s{internals}"
                        $"/I%s{headers}"
                        unit
                        "/link"
                        "/DLL"
                        $"/OUT:%s{core}"
                        Path.Combine(library, "wasmtime.dll.lib")
                    ]
            else
                run
                    "cc"
                    [
                        "-O2"
                        "-shared"
                        "-fPIC"
                        "-DTREE_SITTER_FEATURE_WASM"
                        $"-I%s{include'}"
                        $"-I%s{internals}"
                        $"-I%s{headers}"
                        unit
                        $"-L%s{library}"
                        "-lwasmtime"
                        // The two loaders spell "beside me" differently.
                        if RuntimeInformation.IsOSPlatform OSPlatform.OSX then
                            "-Wl,-rpath,@loader_path"
                        else
                            "-Wl,-rpath,$ORIGIN"
                        "-o"
                        core
                    ]

            let engineLibrary = Path.Combine(library, target.Engine)

            File.Copy(engineLibrary, Path.Combine(destination, target.Engine), true)

            // What they are licensed under travels with them.
            File.Copy(
                Path.Combine(sources, "LICENSE"),
                Path.Combine(destination, "LICENSE-tree-sitter"),
                true
            )

            File.Copy(
                Path.Combine(engine, "LICENSE"),
                Path.Combine(destination, "LICENSE-wasmtime"),
                true
            )

            let size =
                FileInfo(core).Length
                + FileInfo(Path.Combine(destination, target.Engine)).Length

            Log.success $"%s{destination} (%i{size / 1024L} KB)"
            0
        with exn ->
            Log.error exn.Message
            1
        |> fun code ->
            Directory.Delete(work, true)
            code
