namespace Nacara.Plugins

open System
open System.IO
open System.Net.Http
open System.Reflection
open System.Text.Json
open System.Text.RegularExpressions
open Nacara.Core
open Nacara.Plugins.Internal

/// <summary>Which build of the Fable compiler a site's snippets are compiled by.</summary>
/// <remarks>
/// <para>A pair: the packages declare no dependency on each other, but the compiler holds a list
/// of the assemblies it expects and the metadata package has to ship them.</para>
/// <para><c>Latest</c> asks npm at every build, so two builds of the same commit can differ. What
/// it picked is logged.</para>
/// </remarks>
type FableRelease =
    /// A pair that was released and tried together.
    | Pinned of standalone: string * metadata: string
    /// Whatever npm calls latest, asked for when the site is built.
    | Latest

/// <summary>Which Fable precompiles a library, and how it is started.</summary>
/// <remarks>
/// <para>A precompiled library is only readable by the Fable that wrote it, so the default fetches
/// the one the browser's compiler was built from rather than trusting whatever is installed.</para>
/// <para>A site that names its own is then the one keeping the two in step, and is told when they
/// have drifted.</para>
/// </remarks>
type FableCli =
    /// Fetched from NuGet by dnx, at the version the browser's compiler was built from.
    | FetchedFable of version: string
    /// A CLI the site named, and the version it ought to be.
    | NamedFable of tool: string * expected: string option

    /// <summary>What it is, for a log line.</summary>
    member this.Describe =
        match this with
        | FetchedFable version -> $"Fable %s{version}"
        | NamedFable(tool, _) -> tool

    /// <summary>What distinguishes one Fable's output from another's, for the cache.</summary>
    member this.Identity =
        match this with
        | FetchedFable version -> version
        | NamedFable(tool, expected) ->
            let expected = expected |> Option.defaultValue "unknown"
            $"%s{tool}@%s{expected}"

/// <summary>
/// The compiler a live snippet runs on, and where a site gets it.
/// </summary>
/// <remarks>
/// <para>Three npm packages, fetched once per machine by <see cref="T:Nacara.Core.Tool" /> and
/// copied into the site: the Fable compiler built for the browser, the assemblies it type-checks
/// against, and - when snippets colour with tree-sitter - the browser build of tree-sitter.</para>
/// <para>Emitted into the output rather than loaded from a CDN, so a site works offline and
/// behind a proxy.</para>
/// </remarks>
[<RequireQualifiedAccess>]
module Vendor =

    /// Pinned, so a site built today builds the same tomorrow.
    let StandaloneVersion = "3.1.0"

    let MetadataVersion = "2.1.0"

    /// <summary>The pair this plugin was built and tried against.</summary>
    let Default = Pinned(StandaloneVersion, MetadataVersion)

    /// <summary>Matches the tree-sitter the highlighting plugin pins.</summary>
    /// <remarks>A grammar is compiled against a tree-sitter ABI, so the browser build has to be the
    /// same version or it cannot load the grammar the site already ships.</remarks>
    let TreeSitterVersion = Runtime.Version

    /// <summary>Where a site keeps all of this.</summary>
    let Directory = "assets/live-example"

    /// <summary>Where a build of the compiler, its references and a precompiled library are served
    /// from, each relative to <see cref="P:Nacara.Plugins.LiveExample.Vendor.Directory" />.</summary>
    type Layout =
        {
            /// The compiler. Changes with fable-standalone and nothing else.
            Compiler: string
            /// What the checker reads. Holds the precompiled assembly too, because that is where
            /// the worker looks for it, so a new library changes this as well.
            Refs: string
            /// The precompiled library's own JavaScript.
            Precompiled: string
        }

    /// <summary>Where each of those goes, for these versions and this library.</summary>
    /// <param name="standalone">The version of fable-standalone in hand.</param>
    /// <param name="metadata">The version of fable-metadata in hand.</param>
    /// <param name="tag">What the precompiled library is, when there is one.</param>
    let layout (standalone: string) (metadata: string) (tag: string option) =
        {
            Compiler = $"compiler/%s{standalone}"
            Refs =
                match tag with
                | Some tag -> $"refs/%s{metadata}-%s{tag}"
                | None -> $"refs/%s{metadata}"
            Precompiled =
                match tag with
                | Some tag -> $"precompiled/%s{tag}"
                | None -> "precompiled"
        }

    /// <summary>
    /// The suffix put on every assembly the type-checker downloads.
    /// </summary>
    /// <remarks>
    /// GitHub Pages compresses by content type and will not compress a <c>.dll</c> - 6.9 MB
    /// uncompressed to anyone who presses Run. Named <c>.dll.txt</c> it is compressed like text,
    /// and the worker takes the suffix through <c>refsExtraSuffix</c>.
    /// </remarks>
    let AssemblySuffix = ".txt"

    /// <summary>Where a precompiled library is kept, under the project's cache.</summary>
    let private PrecompiledGroup = "live-example"

    /// <summary>
    /// Compiling a library once, ahead of time, into something a snippet can reference.
    /// </summary>
    /// <param name="key">What the output is keyed under: change it and the work is done again.</param>
    let private slugOf (key: string) =
        use sha = Security.Cryptography.SHA256.Create()

        sha.ComputeHash(Text.Encoding.UTF8.GetBytes key)
        |> Array.take 8
        |> Array.map (fun byte -> byte.ToString "x2")
        |> String.concat ""

    /// <summary>Run <c>fable precompile</c> over a project, and say where it put things.</summary>
    /// <param name="cli">Which Fable to run it with.</param>
    /// <param name="project">The project to compile. Its own files come too, so a site can offer
    /// its readers helpers as well as packages.</param>
    /// <param name="directory">Where the output is kept.</param>
    let private runPrecompile (cli: FableCli) (project: string) (directory: string) =
        let fableModules = Path.Combine(directory, "out", "fable_modules")

        try
            IO.Directory.CreateDirectory directory |> ignore

            Log.info
                $"Precompiling %s{Path.GetFileName project} with %s{cli.Describe} - this happens once"

            let executable, before =
                match cli with
                // The separator matters: without it dnx reads Fable's flags as its own.
                | FetchedFable version ->
                    "dotnet",
                    [
                        "dnx"
                        $"fable@%s{version}"
                        "--"
                    ]
                | NamedFable(tool, _) -> tool, []

            let start =
                Diagnostics.ProcessStartInfo(
                    executable,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = directory
                )

            // Deterministic source paths rewrite the paths inside the assemblies and not the ones
            // precompiled_info.json records, and the browser matches a source file on those.
            start.EnvironmentVariables["DeterministicSourcePaths"] <- "false"

            before
            @ [
                "precompile"
                project
                "--outDir"
                "out"
            ]
            |> List.iter start.ArgumentList.Add

            use running = Diagnostics.Process.Start start
            let output = running.StandardOutput.ReadToEnd()
            let complaint = running.StandardError.ReadToEnd()
            running.WaitForExit()

            let precompiledInfo = Path.Combine(fableModules, "precompiled_info.json")

            if running.ExitCode <> 0 then
                let said =
                    if complaint = "" then
                        output
                    else
                        complaint

                Error $"fable precompile failed: %s{said}"
            elif not (File.Exists precompiledInfo) then
                Error "fable precompile wrote no precompiled_info.json"
            else
                Ok fableModules
        with
        | :? ComponentModel.Win32Exception ->
            match cli with
            | FetchedFable version ->
                Error
                    $"dnx could not run fable %s{version}. It comes with the .NET 10 SDK; name a Fable of your own with LiveExample.fableTool if you cannot reach NuGet"
            | NamedFable(tool, _) ->
                Error
                    $"'%s{tool}' could not be started. Check the path, or leave LiveExample.fableTool out and let the pinned Fable be fetched"
        | exn -> Error $"Could not precompile: %s{exn.Message}"

    /// <summary>Precompile a project a site wrote, with everything it references.</summary>
    /// <remarks>Rebuilt when anything in it changes, and otherwise read from the last time.</remarks>
    /// <param name="projectRoot">The root of the site being built, where the output is kept.</param>
    /// <param name="cli">Which Fable to run it with.</param>
    /// <param name="project">The <c>.fsproj</c> to compile.</param>
    let precompileProject (projectRoot: AbsolutePath) (cli: FableCli) (project: string) =
        if not (File.Exists project) then
            Error $"'%s{project}' does not exist"
        else

            let stamp =
                match ProjectInputs.read project with
                | Ok inputs -> inputs
                | Error message ->
                    Log.debug
                        $"Asking MSBuild what %s{Path.GetFileName project} compiles: %s{message}"

                    let directory = Path.GetDirectoryName project

                    IO.Directory.EnumerateFiles(directory, "*.*", IO.SearchOption.AllDirectories)
                    |> Seq.filter (fun file ->
                        let extension = Path.GetExtension file
                        extension = ".fs" || extension = ".fsx" || extension = ".fsproj"
                    )
                    |> Seq.sort
                    |> Seq.map (fun file -> $"%s{file}:%i{FileInfo(file).LastWriteTimeUtc.Ticks}")
                    |> String.concat ";"

            let slug = slugOf $"project:%s{project}:%s{stamp}:fable:%s{cli.Identity}"

            ProjectCache.forgetOthers projectRoot PrecompiledGroup [ slug ]

            let cache =
                ProjectCache.directory projectRoot PrecompiledGroup slug |> AbsolutePath.value

            let modules = Path.Combine(cache, "out", "fable_modules")

            if File.Exists(Path.Combine(modules, "precompiled_info.json")) then
                Ok modules
            else
                runPrecompile cli (Path.GetFullPath project) cache

    /// <summary>What the build knows about a precompiled library, for the browser to be told.</summary>
    type Precompiled =
        {
            /// The Fable that wrote it. The worker refuses one written by a different Fable.
            CompilerVersion: string
            /// For each source file: what Fable matches it on, its module, and where its code went.
            Files: (string * string * string) list
            /// The first member in each chunk below, which is how the compiler finds the chunk a
            /// name is in. Empty when the library has no inline members.
            InlineExprHeaders: string list
            /// The bodies of those members, in the order the headers name them. F# keeps an inline
            /// body out of the assembly, so without these a snippet calling one does not compile.
            InlineExprChunks: string list
        }

    /// <summary>Read what <c>fable precompile</c> recorded.</summary>
    /// <remarks><c>OutPath</c> is where the compiled file was written on this machine; it is kept
    /// relative to the output so the browser can be told where the site serves it. <c>Path</c> is
    /// left exactly as it is: Fable matches a source file on it rather than reading from it.</remarks>
    /// <param name="modules">The <c>fable_modules</c> directory a precompile produced.</param>
    let precompiledInfo (modules: string) =
        let out = Path.GetDirectoryName modules
        let text = File.ReadAllText(Path.Combine(modules, "precompiled_info.json"))
        use document = JsonDocument.Parse text
        let root = document.RootElement

        let files =
            root.GetProperty("Files").EnumerateObject()
            |> Seq.map (fun file ->
                let value = file.Value
                let outPath = value.GetProperty("OutPath").GetString()

                file.Name,
                value.GetProperty("RootModule").GetString(),
                Path.GetRelativePath(out, outPath).Replace("\\", "/")
            )
            |> List.ofSeq

        let headers =
            match root.TryGetProperty "InlineExprHeaders" with
            | true, value -> value.EnumerateArray() |> Seq.map _.GetString() |> List.ofSeq
            | _ -> []

        // The compiler pairs a chunk with the header of the same index.
        let chunks =
            headers
            |> List.mapi (fun index _ ->
                let chunk =
                    Path.Combine(modules, "inline_exprs", $"inline_exprs_%i{index}.browser.json")

                if not (File.Exists chunk) then
                    failwith
                        $"fable precompile recorded %i{List.length headers} chunks of inline members but wrote no %s{Path.GetFileName chunk}"

                Path.GetRelativePath(out, chunk).Replace("\\", "/")
            )

        {
            CompilerVersion = root.GetProperty("CompilerVersion").GetString()
            Files = files
            InlineExprHeaders = headers
            InlineExprChunks = chunks
        }

    /// <summary>What a precompile produced, as a short hash of it.</summary>
    /// <param name="modules">The <c>fable_modules</c> directory a precompile produced.</param>
    let precompileTag (modules: string) =
        let out = Path.GetDirectoryName modules

        let carried (file: string) =
            match Path.GetExtension file with
            | ".js"
            | ".json"
            | ".dll" -> true
            | _ -> false

        use sha = Security.Cryptography.SHA256.Create()

        use hashed =
            new Security.Cryptography.CryptoStream(
                IO.Stream.Null,
                sha,
                Security.Cryptography.CryptoStreamMode.Write
            )

        IO.Directory.EnumerateFiles(out, "*.*", IO.SearchOption.AllDirectories)
        |> Seq.filter carried
        |> Seq.map (fun file -> Path.GetRelativePath(out, file).Replace("\\", "/"), file)
        |> Seq.sortBy fst
        |> Seq.iter (fun (relative, file) ->
            let name = Text.Encoding.UTF8.GetBytes relative
            hashed.Write(name, 0, name.Length)
            use reading = File.OpenRead file
            reading.CopyTo hashed
        )

        hashed.FlushFinalBlock()

        sha.Hash
        |> Array.take 8
        |> Array.map (fun byte -> byte.ToString "x2")
        |> String.concat ""

    /// <summary>The precompiled library, as files to emit.</summary>
    /// <remarks>
    /// The assembly goes in with the ones the checker already reads, because that is where the
    /// worker looks for it. The JavaScript keeps the shape it was written in, so the relative
    /// imports inside it resolve on their own - except for the copy of fable-library it came with,
    /// which is dropped in favour of the compiler's: they are the same files, and the worker will
    /// not load a library built by a different Fable, so they cannot drift apart.
    /// </remarks>
    /// <param name="layout">Where this build of the compiler and library are served from.</param>
    /// <param name="modules">The <c>fable_modules</c> directory a precompile produced.</param>
    let precompiledAssets (layout: Layout) (modules: string) =
        let out = Path.GetDirectoryName modules

        let library = Regex(@"(?:\.{1,2}/)+(?:fable_modules/)?fable-library-js[^/""]*/")

        [
            CopyFile(
                AbsolutePath.create (Path.Combine(modules, "Fable.Precompiled.dll")),
                RelativePath.create
                    $"%s{Directory}/%s{layout.Refs}/Fable.Precompiled.dll%s{AssemblySuffix}"
            )

            for file in IO.Directory.EnumerateFiles(out, "*.js", IO.SearchOption.AllDirectories) do
                let relative = Path.GetRelativePath(out, file).Replace("\\", "/")

                if not (relative.Contains "fable-library-js") then
                    let text = library.Replace(File.ReadAllText file, "fable-library-js/")

                    if text.Contains "fable-library-js." then
                        failwith
                            $"%s{relative} still points at the precompile's own copy of fable-library"

                    WriteText(
                        text,
                        RelativePath.create $"%s{Directory}/%s{layout.Precompiled}/%s{relative}"
                    )

            let inlineExprs = Path.Combine(modules, "inline_exprs")

            if IO.Directory.Exists inlineExprs then
                for chunk in IO.Directory.EnumerateFiles(inlineExprs, "*.browser.json") do
                    let relative = Path.GetRelativePath(out, chunk).Replace("\\", "/")

                    CopyFile(
                        AbsolutePath.create chunk,
                        RelativePath.create $"%s{Directory}/%s{layout.Precompiled}/%s{relative}"
                    )
        ]

    /// <summary>What a tarball must hold for the download to count as good.</summary>
    let private sentinel = [ "package.json" ]

    let private npm (scope: string) (name: string) (version: string) =
        let path =
            if scope = "" then
                name
            else
                $"%s{scope}/%s{name}"

        $"https://registry.npmjs.org/%s{path}/-/%s{name}-%s{version}.tgz"

    let standaloneOf version =
        {
            Name = "fable-standalone"
            Version = version
            Url = npm "@fable-org" "fable-standalone" version
            Archive = TarGzip
            Files = sentinel
            Executable = []
            Checksum = None
        }

    let metadataOf version =
        {
            Name = "fable-metadata"
            Version = version
            Url = npm "@fable-org" "fable-metadata" version
            Archive = TarGzip
            Files = sentinel
            Executable = []
            Checksum = None
        }

    let treeSitter =
        {
            Name = "web-tree-sitter"
            Version = TreeSitterVersion
            Url = npm "" "web-tree-sitter" TreeSitterVersion
            Archive = TarGzip
            Files = sentinel
            Executable = []
            Checksum = None
        }

    /// <summary>The version npm calls latest, or nothing if it could not be asked.</summary>
    let private latestOf (package: string) =
        try
            use client = new HttpClient(Timeout = TimeSpan.FromSeconds 10.0)

            let json =
                client
                    .GetStringAsync($"https://registry.npmjs.org/-/package/%s{package}/dist-tags")
                    .GetAwaiter()
                    .GetResult()

            let found = Regex.Match(json, "\"latest\"\s*:\s*\"([^\"]+)\"")

            if found.Success then
                Some found.Groups[1].Value
            else
                None
        with _ ->
            None

    /// <summary>What npm calls latest, asked once however often a build wants to know.</summary>
    let private latestPair =
        lazy
            (let standalone =
                latestOf "@fable-org/fable-standalone" |> Option.defaultValue StandaloneVersion

             let metadata =
                 latestOf "@fable-org/fable-metadata" |> Option.defaultValue MetadataVersion

             Log.info
                 $"Live examples: fable-standalone %s{standalone}, fable-metadata %s{metadata} (latest)"

             standalone, metadata)

    /// <summary>The two versions to fetch, and how they were arrived at.</summary>
    /// <param name="release">What the site asked for.</param>
    let resolve (release: FableRelease) =
        match release with
        | Pinned(standalone, metadata) -> standalone, metadata
        | Latest -> latestPair.Value

    /// <summary>The Fable a build of the compiler was made with, as its own package records it.</summary>
    /// <param name="directory">The fetched fable-standalone package.</param>
    let private fableVersionOf (directory: string) =
        try
            let manifest = Path.Combine(directory, "package.json")

            if not (File.Exists manifest) then
                None
            else
                use document = JsonDocument.Parse(File.ReadAllText manifest)

                match document.RootElement.TryGetProperty "fableVersion" with
                | true, value -> value.GetString() |> Option.ofObj
                | _ -> None
        with _ ->
            None

    /// <summary>The Fable the compiler in the browser was built from.</summary>
    /// <remarks>Read before anything is compiled, which is what lets the same Fable be fetched to do
    /// the compiling. <c>None</c> when the package does not say, which is every build of it before
    /// fable-standalone 3.1.0.</remarks>
    /// <param name="release">Which build of the compiler the site asked for.</param>
    let fableVersion (release: FableRelease) =
        let standaloneVersion, _ = resolve release

        match Tool.resolve (standaloneOf standaloneVersion) with
        | Error _ -> None
        | Ok directory -> fableVersionOf directory

    /// <summary>Whether the Fable that precompiled a library is the one that will read it.</summary>
    /// <remarks>
    /// A precompiled library is only readable by the Fable that wrote it. The worker enforces that
    /// by answering <c>CreateChecker</c> with a bare <c>LoadFailed</c>, which reaches a reader as a
    /// snippet that never answers and says nothing about why - so it is worth knowing here, where
    /// there is still a build to say it in and a slower way to carry on.
    /// </remarks>
    /// <param name="browser">What the compiler in the browser is, when it says.</param>
    /// <param name="modules">The <c>fable_modules</c> directory a precompile produced.</param>
    let agrees (browser: string option) (modules: string) =
        match browser with
        | None -> Ok()
        | Some browser ->
            let written = (precompiledInfo modules).CompilerVersion

            if written = browser then
                Ok()
            else
                Error
                    $"it was precompiled by Fable %s{written}, and the compiler in the browser is Fable %s{browser}. Leave LiveExample.fableTool out and the matching one is fetched"

    let private under (directory: string) (parts: string list) =
        Path.Combine(directory :: parts |> Array.ofList)

    let private copy source destination =
        CopyFile(AbsolutePath.create source, RelativePath.create $"%s{Directory}/%s{destination}")

    /// <summary>What the highlighting plugin ships for a language, or why it does not.</summary>
    let private bundled (read: string -> 'T) (language: string) =
        try
            Ok(read language)
        with exn ->
            Error exn.Message

    let private bundledGrammar = bundled TreeSitter.bundledGrammar
    let private bundledQueries = bundled TreeSitter.bundledQueries

    /// <summary>
    /// Every capture the queries use, paired with the class the build would give it.
    /// </summary>
    /// <remarks>
    /// Read out of the queries and answered by the highlighting plugin's own
    /// <see cref="M:Nacara.Plugins.TreeSitter.className" />, so the browser colours a snippet the
    /// same way the build coloured the block above it. Deriving it beats copying the table into
    /// JavaScript, where the two would drift apart the first time a capture was added.
    /// </remarks>
    let classMap (queries: string) =
        Regex.Matches(queries, @"@([A-Za-z][A-Za-z0-9_.]*)")
        |> Seq.map (fun m -> m.Groups[1].Value.TrimEnd('.'))
        |> Seq.distinct
        |> Seq.sort
        |> Seq.choose (fun capture ->
            TreeSitter.className capture |> Option.map (fun name -> capture, name)
        )
        |> List.ofSeq

    let private json (pairs: (string * string) list) =
        pairs
        |> List.map (fun (key, value) -> $"\"%s{key}\":\"%s{value}\"")
        |> String.concat ","
        |> sprintf "{%s}"

    /// <summary>Everything the browser needs, as assets to emit.</summary>
    /// <param name="treeSitterHighlighting">Whether the tree-sitter grammar is wanted too.</param>
    /// <param name="named">Grammars the site named for the languages its targets produce.</param>
    /// <param name="targetLanguages">What the targets compile to, so the ones the highlighting
    /// plugin already ships a grammar for are coloured without the site saying anything.</param>
    /// <param name="release">Which build of the compiler to fetch.</param>
    /// <param name="tag">What the precompiled library is, when the site built one.</param>
    /// <returns>The assets, the languages a grammar was emitted for, where all of it went, and the
    /// Fable this build of the compiler was made with - or the first reason one could not be
    /// had.</returns>
    let assets
        (treeSitterHighlighting: bool)
        (named: TreeSitterGrammar list)
        (targetLanguages: string list)
        (release: FableRelease)
        (tag: string option)
        =
        let standaloneVersion, metadataVersion = resolve release
        let layout = layout standaloneVersion metadataVersion tag

        match
            Tool.resolve (standaloneOf standaloneVersion), Tool.resolve (metadataOf metadataVersion)
        with
        | Error message, _
        | _, Error message -> Error message
        | Ok standaloneDir, Ok metadataDir ->

            let fable = fableVersionOf standaloneDir

            // The worker does importScripts("bundle.min.js"), so those two share a directory.
            let compiler =
                [
                    copy
                        (under
                            standaloneDir
                            [
                                "package"
                                "dist"
                                "bundle.min.js"
                            ])
                        $"%s{layout.Compiler}/bundle.min.js"
                    copy
                        (under
                            standaloneDir
                            [
                                "package"
                                "dist"
                                "worker.min.js"
                            ])
                        $"%s{layout.Compiler}/worker.min.js"
                    CopyDirectory(
                        AbsolutePath.create (
                            under
                                standaloneDir
                                [
                                    "package"
                                    "dist"
                                    "fable-library-js"
                                ]
                        ),
                        RelativePath.create $"%s{Directory}/%s{layout.Compiler}/fable-library-js"
                    )
                ]

            let assemblies =
                System.IO.Directory.EnumerateFiles(
                    under
                        metadataDir
                        [
                            "package"
                            "lib"
                        ],
                    "*.dll"
                )
                |> Seq.sort
                |> Seq.map (fun path ->
                    copy path $"%s{layout.Refs}/%s{Path.GetFileName path}%s{AssemblySuffix}"
                )
                |> List.ofSeq

            if not treeSitterHighlighting then
                Ok(compiler @ assemblies, [], layout, fable)
            else

                match Tool.resolve treeSitter with
                | Error message -> Error message
                | Ok treeSitterDir ->
                    let filesOf language =
                        match
                            named |> List.tryFind (fun grammar -> grammar.Language = language)
                        with
                        | Some grammar ->
                            match grammar.Source with
                            | Bundled -> bundledGrammar language, bundledQueries language
                            | Files(wasm, queries) ->
                                let read path =
                                    if File.Exists path then
                                        Ok(File.ReadAllBytes path)
                                    else
                                        Error
                                            $"The grammar for %s{language} names '%s{path}', which does not exist"

                                read wasm, read queries
                            | Repository(repository, reference, subdirectory, queries) ->
                                match
                                    Toolchain.ensure
                                        {
                                            Language = language
                                            Repository = repository
                                            Reference = reference
                                            Subdirectory = subdirectory
                                            Queries = queries
                                        }
                                        true
                                        Toolchain.CliSource
                                        Toolchain.WasiSdkSource
                                with
                                | Error message -> Error message, Error message
                                | Ok(wasm, queries) ->
                                    Ok(File.ReadAllBytes wasm), Ok(File.ReadAllBytes queries)
                        | None -> bundledGrammar language, bundledQueries language

                    let ofLanguage language =
                        match filesOf language with
                        | Error message, _
                        | _, Error message -> Error message
                        | Ok grammar, Ok queries ->
                            Ok
                                [
                                    WriteBytes(
                                        grammar,
                                        RelativePath.create
                                            $"%s{Directory}/grammars/%s{language}/grammar.wasm.gz"
                                    )
                                    WriteBytes(
                                        queries,
                                        RelativePath.create
                                            $"%s{Directory}/grammars/%s{language}/highlights.scm"
                                    )
                                    WriteText(
                                        json (classMap (Text.Encoding.UTF8.GetString queries)),
                                        RelativePath.create
                                            $"%s{Directory}/grammars/%s{language}/captures.json"
                                    )
                                ]

                    let shipped = TreeSitter.bundledLanguages.Value

                    let languages =
                        "fsharp" :: targetLanguages
                        |> List.filter (fun language -> List.contains language shipped)
                        |> List.distinct

                    let grammars =
                        (Ok [], languages)
                        ||> List.fold (fun state language ->
                            match state, ofLanguage language with
                            | Error message, _
                            | _, Error message -> Error message
                            | Ok sofar, Ok assets -> Ok(sofar @ assets)
                        )

                    match grammars with
                    | Error message -> Error message
                    | Ok grammars ->
                        let extra =
                            named
                            |> List.filter (fun grammar ->
                                not (List.contains grammar.Language languages)
                            )
                            |> List.choose (fun grammar ->
                                match ofLanguage grammar.Language with
                                | Ok assets -> Some(grammar.Language, assets)
                                | Error message ->
                                    Log.warn
                                        $"Output in %s{grammar.Language} is shown without colour: %s{message}"

                                    None
                            )

                        let grammars = grammars @ (extra |> List.collect snd)
                        let emitted = languages @ (extra |> List.map fst)

                        let runtime =
                            [
                                copy
                                    (under
                                        treeSitterDir
                                        [
                                            "package"
                                            "web-tree-sitter.js"
                                        ])
                                    "tree-sitter/web-tree-sitter.js"
                                copy
                                    (under
                                        treeSitterDir
                                        [
                                            "package"
                                            "web-tree-sitter.wasm"
                                        ])
                                    "tree-sitter/web-tree-sitter.wasm"
                            ]

                        Ok(compiler @ assemblies @ runtime @ grammars, emitted, layout, fable)
