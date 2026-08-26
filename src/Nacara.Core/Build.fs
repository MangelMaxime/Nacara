namespace Nacara.Core

open System
open System.IO
open System.Diagnostics
open System.Collections.Concurrent
open System.Threading.Tasks
open System.Security.Cryptography
open System.Text
open Microsoft.Extensions.FileSystemGlobbing
open Feliz.ViewEngine

/// <summary>Outcome of one build.</summary>
type BuildResult =
    {
        Pages: Page list
        Diagnostics: Diagnostic list
        /// Files whose content actually changed on disk.
        WrittenFiles: int
        /// Files that were already up to date.
        UnchangedFiles: int
        /// Files removed because nothing produced them any more.
        PrunedFiles: int
        Elapsed: TimeSpan
    }

    member this.Succeeded =
        this.Diagnostics |> List.forall (fun item -> item.Severity <> Severity.Error)

/// <summary>
/// What survives between two builds of the same process.
/// </summary>
type BuildCache(writes: bool) =
    let transformed = ConcurrentDictionary<string, Page * Diagnostic list>()
    let loaded = ConcurrentDictionary<string, Result<Page, Diagnostic>>()
    let rendered = ConcurrentDictionary<string, string>()
    let written = ConcurrentDictionary<string, string>()

    /// <summary>A cache that writes what it is given, which is what a build needs.</summary>
    new() = BuildCache(true)

    /// <summary>Whether this build puts anything on disk.</summary>
    /// <remarks>False under <c>check</c>, which renders every page and resolves every link and
    /// leaves the output alone.</remarks>
    member _.Writes = writes

    static member Hash(content: string) =
        content |> Encoding.UTF8.GetBytes |> SHA256.HashData |> Convert.ToHexString

    static member HashBytes(content: byte array) =
        content |> SHA256.HashData |> Convert.ToHexString

    member _.Clear() =
        transformed.Clear()
        loaded.Clear()
        rendered.Clear()
        written.Clear()

    /// <summary>Reuse the decoding of a file that has not changed.</summary>
    /// <param name="key">The file, and what is in it.</param>
    /// <param name="compute">What to do when it has changed.</param>
    member _.Load(key: string, compute: unit -> Result<Page, Diagnostic>) =
        match loaded.TryGetValue key with
        | true, page -> page
        | _ ->
            let page = compute ()
            loaded[key] <- page
            page

    /// <summary>Reuse the rendering of a page nothing about which has changed.</summary>
    /// <param name="key">Everything the rendering depends on.</param>
    /// <param name="compute">What to do when any of it has changed.</param>
    member _.Render(key: string, compute: unit -> string) =
        match rendered.TryGetValue key with
        | true, html -> html
        | _ ->
            let html = compute ()
            rendered[key] <- html
            html

    /// <summary>Reuse the transform of a page whose body has not changed.</summary>
    /// <param name="key">What the transform is, so two of them never share a cached result.</param>
    /// <param name="page">The page being transformed.</param>
    /// <param name="sink">Where what the transform says ends up.</param>
    /// <param name="compute">What to do when the body has changed, given the sink to report to.</param>
    member _.Transform
        (key: string, page: Page, sink: DiagnosticSink, compute: DiagnosticSink -> Page)
        =
        let cacheKey = $"%s{key}:%s{BuildCache.Hash page.Body}"

        match transformed.TryGetValue cacheKey with
        | true, (cached, diagnostics) ->
            sink.AddRange diagnostics

            { page with
                Html = cached.Html
                Headings = cached.Headings
                Data = cached.Data
            }
        | _ ->
            let caught = DiagnosticBag()
            let result = compute (DiagnosticSink(caught, sink.Source))
            let diagnostics = caught.ToList() |> List.ofSeq
            transformed[cacheKey] <- (result, diagnostics)
            sink.AddRange diagnostics
            result

    /// <summary>Write a file only when its content differs. Returns true when it was written.</summary>
    /// <param name="path">Where the file goes.</param>
    /// <param name="content">What it should contain.</param>
    member _.WriteIfChanged(path: AbsolutePath, content: string) =
        let key = AbsolutePath.value path
        let hash = BuildCache.Hash content

        let upToDate =
            match written.TryGetValue key with
            | true, previous when previous = hash -> File.Exists key
            | _ -> File.Exists key && BuildCache.Hash(File.ReadAllText key) = hash

        written[key] <- hash

        if upToDate then
            false
        else
            if writes then
                Directory.CreateDirectory(Path.GetDirectoryName key) |> ignore
                File.WriteAllText(key, content)

            true

    /// <summary>Write bytes only when they differ. Returns true when they were written.</summary>
    /// <param name="path">Where the file goes.</param>
    /// <param name="content">What it should contain.</param>
    member _.WriteBytesIfChanged(path: AbsolutePath, content: byte array) =
        let key = AbsolutePath.value path
        let hash = BuildCache.HashBytes content

        let upToDate =
            match written.TryGetValue key with
            | true, previous when previous = hash -> File.Exists key
            | _ -> File.Exists key && BuildCache.HashBytes(File.ReadAllBytes key) = hash

        written[key] <- hash

        if upToDate then
            false
        else
            if writes then
                Directory.CreateDirectory(Path.GetDirectoryName key) |> ignore
                File.WriteAllBytes(key, content)

            true

    /// <summary>Copy a file only when its content differs. Returns true when it was copied.</summary>
    /// <param name="source">The file to copy.</param>
    /// <param name="destination">Where it goes.</param>
    member this.CopyIfChanged(source: AbsolutePath, destination: AbsolutePath) =
        let sourcePath = AbsolutePath.value source
        let destinationPath = AbsolutePath.value destination

        let upToDate =
            File.Exists destinationPath
            && FileInfo(sourcePath).Length = FileInfo(destinationPath).Length
            && File.GetLastWriteTimeUtc sourcePath <= File.GetLastWriteTimeUtc destinationPath

        if upToDate then
            false
        else
            if writes then
                Directory.CreateDirectory(Path.GetDirectoryName destinationPath) |> ignore
                File.Copy(sourcePath, destinationPath, true)

            true

/// <summary>A file a collection's source turned up, before its front matter has been read.</summary>
type private DiscoveredFile =
    {
        Site: SiteInfo
        Locale: Locale
        RelativePath: RelativePath
        ProjectPath: RelativePath option
        Source: PageSource
        Text: string
        Dependencies: AbsolutePath list
    }

[<RequireQualifiedAccess>]
module Build =

    /// <summary>
    /// Map over items across cores, preserving input order.
    /// </summary>
    let private parallelMap (mapping: 'T -> 'U) (items: 'T array) =
        if items.Length < 2 then
            Array.map mapping items
        else
            let results = Array.zeroCreate items.Length

            Parallel.For(
                0,
                items.Length,
                ParallelOptions(MaxDegreeOfParallelism = Environment.ProcessorCount),
                fun index -> results[index] <- mapping items[index]
            )
            |> ignore

            results

    /// <summary>Files of a directory matching any of the patterns, in a stable order.</summary>
    let private walk
        (projectRoot: AbsolutePath)
        (site: Site)
        (diagnostics: DiagnosticBag)
        (name: string)
        (root: string)
        (patterns: string list)
        =
        let rootPath = AbsolutePath.combine projectRoot [ root ]

        if not (Directory.Exists(AbsolutePath.value rootPath)) then
            diagnostics.Add(
                Diagnostic.warning
                    "nacara/collection-source-missing"
                    $"Collection '%s{name}' has no content: %s{AbsolutePath.toLog rootPath} does not exist"
                |> Diagnostic.withHint
                    "Paths are resolved from the project root, which '--root <dir>' overrides"
            )

            []
        else

            let matcher = Matcher()
            patterns |> List.iter (fun pattern -> matcher.AddInclude pattern |> ignore)

            matcher.GetResultsInFullPath(AbsolutePath.value rootPath)
            // Sorted so the build is reproducible whatever the file system returns.
            |> Seq.sort
            |> Seq.map (fun file ->
                let path = AbsolutePath.create file

                {
                    Site = Site.toInfo site
                    Locale = Site.rootLocale site
                    RelativePath = RelativePath.fromRoot rootPath path
                    ProjectPath = Some(RelativePath.fromRoot projectRoot path)
                    Source = FromFile path
                    Text = File.ReadAllText file
                    Dependencies = []
                }
            )
            |> List.ofSeq

    /// <summary>
    /// The files of a collection, from disk or from whatever produces them.
    /// </summary>
    let private discover
        (projectRoot: AbsolutePath)
        (site: Site)
        (formats: string list)
        (diagnostics: DiagnosticBag)
        (definition: CollectionDefinition)
        =
        match definition.Source with
        | FileFormats _ when List.isEmpty formats ->
            diagnostics.Add(
                Diagnostic.warning
                    "nacara/no-front-matter-format"
                    $"Collection '%s{definition.Name}' reads nothing: no plugin says how a file carries its front matter"
                |> Diagnostic.withHint
                    "Register a format plugin - Markdown.register reads .md - or name the files to read with Collection.source"
            )

            []
        | FileFormats root ->
            walk
                projectRoot
                site
                diagnostics
                definition.Name
                root
                [ for extension in formats -> $"**/*%s{extension}" ]
        | FileGlob(root, patterns) ->
            walk projectRoot site diagnostics definition.Name root patterns
        | Producer(name, produce) ->
            let context =
                {
                    Site = Site.toInfo site
                    ProjectRoot = projectRoot
                    Diagnostics = DiagnosticSink(diagnostics, name)
                }

            produce context
            // Keep build reproducible
            |> List.sortBy (fun content -> RelativePath.value content.Path)
            |> List.map (fun content ->
                {
                    Site = context.Site
                    Locale = Site.rootLocale site
                    RelativePath = content.Path
                    ProjectPath = None
                    Source = Generated name
                    Text = content.Text
                    Dependencies = content.Dependencies
                }
            )

    /// <summary>
    /// Resolve which locale a page belongs to from its path.
    /// </summary>
    let private resolveLocale (site: Site) (file: DiscoveredFile) =
        if List.length site.Locales <= 1 then
            file
        else
            match RelativePath.segments file.RelativePath with
            | first :: rest ->
                let claiming =
                    site.Locales
                    |> List.tryFind (fun locale -> not locale.IsRoot && locale.Code = first)

                match claiming with
                | Some locale ->
                    { file with
                        Locale = locale
                        RelativePath = String.Join("/", rest) |> RelativePath.create
                    }
                | None -> file
            | [] -> file

    let private reportDuplicateRoutes (diagnostics: DiagnosticBag) (pages: Page list) =
        pages
        |> List.groupBy (fun page -> Url.outputPath page.Route |> RelativePath.value)
        |> List.filter (fun (_, pages) -> List.length pages > 1)
        |> List.iter (fun (output, pages) ->
            let sources = pages |> List.map _.Source.Describe |> String.concat ", "

            diagnostics.Add(
                Diagnostic.error
                    "nacara/duplicate-route"
                    $"Several pages want to be written to '%s{output}': %s{sources}"
                |> Diagnostic.withHint "Give them different routes with Collection.route"
            )
        )

    /// <summary>
    /// Run the transforms that claim this page, in order, until none is left.
    /// </summary>
    let private applyTransforms
        (registry: Registry)
        (context: TransformContext)
        (cache: BuildCache)
        (page: Page)
        =
        /// Whether this transform is one that runs over this page. A transform naming no
        /// extensions takes every page, which is how one that works on rendered html rather than
        /// on a source format asks for all of them.
        let claims (transform: ContentTransform) (page: Page) =
            List.isEmpty transform.Extensions
            || List.contains page.Format transform.Extensions

        let rec run (sink: DiagnosticSink) (applied: Set<string>) (page: Page) =
            let next =
                registry.Transforms
                |> List.tryFind (fun transform ->
                    not (applied.Contains transform.Name) && claims transform page
                )

            match next with
            | None -> page
            | Some transform ->
                let page =
                    transform.Transform
                        { context with
                            Diagnostics = sink
                        }
                        page

                run sink (applied.Add transform.Name) page

        if
            registry.Transforms
            |> List.forall (fun transform -> not (claims transform page))
        then
            { page with
                Html = page.Body
            }
        else
            let key = registry.Transforms |> List.map _.Name |> String.concat "+"

            cache.Transform(
                $"%s{key}:%s{page.Id}",
                page,
                context.Diagnostics,
                fun sink -> run sink Set.empty page
            )

    let private runCore
        (cache: BuildCache)
        (projectRoot: AbsolutePath)
        (isWatch: bool)
        (site: Site)
        =
        let stopwatch = Stopwatch.StartNew()
        let mutable phaseStart = 0L

        let phase (name: string) =
            let elapsed = stopwatch.ElapsedMilliseconds - phaseStart
            phaseStart <- stopwatch.ElapsedMilliseconds
            Log.debug (name + ": " + string elapsed + " ms")

        let diagnostics = DiagnosticBag()
        Site.validate site |> List.iter diagnostics.Add

        let registry =
            Registry.ofPlugins site.Plugins
            |> fun registry ->
                site.Collections
                |> List.fold (fun registry item -> Registry.collection item registry) registry

        let own (asPageAsset: string -> PageAsset) (path: string) (registry: Registry) =
            let entry = AbsolutePath.combine projectRoot [ path ]
            let extension = (AbsolutePath.extension entry).ToLowerInvariant()

            if not (File.Exists(AbsolutePath.value entry)) then
                diagnostics.Add(
                    Diagnostic.error "missing-asset" $"'%s{path}' does not exist"
                    |> Diagnostic.withHint "Paths are resolved from the project root."
                )

                registry
            else

                let sources =
                    Directory.EnumerateFiles(AbsolutePath.value (AbsolutePath.directory entry))
                    |> Seq.filter (fun file ->
                        Path.GetExtension(file).ToLowerInvariant() = extension
                    )
                    |> Seq.sort
                    |> Seq.map (fun file -> Path.GetFileName file, File.ReadAllText file)
                    |> List.ofSeq

                let hash =
                    sources
                    |> List.map snd
                    |> String.concat "\n"
                    |> BuildCache.Hash
                    |> fun value -> value.Substring(0, 8).ToLowerInvariant()

                let name = Path.GetFileNameWithoutExtension path
                let destination = $"assets/%s{name}.%s{hash}%s{extension}"

                registry
                |> Registry.asset (
                    Bundle(sources, AbsolutePath.fileName entry, RelativePath.create destination)
                )
                |> Registry.extra (asPageAsset destination)

        let registry =
            let withStyles =
                (registry, site.Stylesheets)
                ||> List.fold (fun registry path -> own Stylesheet path registry)

            (withStyles, site.Scripts)
            ||> List.fold (fun registry path -> own (fun path -> Script(path, true)) path registry)

        let siteInfo =
            { Site.toInfo site with
                PageAssets = Registry.extras<PageAsset> registry
            }

        let outputDirectory = AbsolutePath.combine projectRoot [ site.OutputDirectory ]

        // The last registered wins, so a site can override the format its plugin brought.
        registry.FrontMatterFormats
        |> List.collect (fun format ->
            format.Extensions |> List.map (fun extension -> extension, format.Name)
        )
        |> List.groupBy fst
        |> List.filter (fun (_, claims) -> List.length claims > 1)
        |> List.iter (fun (extension, claims) ->
            let names = claims |> List.map snd |> String.concat ", "

            diagnostics.Add(
                Diagnostic.warning
                    "nacara/duplicate-front-matter-format"
                    $"Several front matter formats claim '%s{extension}': %s{names}"
                |> Diagnostic.withHint "The last one registered is the one used"
            )
        )

        // Every transform claiming an extension runs; one naming none is a catch-all.
        registry.AssetTransforms
        |> List.collect (fun transform ->
            transform.Extensions |> List.map (fun extension -> extension, transform.Name)
        )
        |> List.groupBy fst
        |> List.filter (fun (_, claims) -> List.length claims > 1)
        |> List.iter (fun (extension, claims) ->
            let names = claims |> List.map snd |> String.concat ", "

            diagnostics.Add(
                Diagnostic.warning
                    "nacara/duplicate-asset-transform"
                    $"Several transforms claim '%s{extension}': %s{names}"
                |> Diagnostic.withHint
                    "They all run, one after another. Register the one you want."
            )
        )

        /// Split a discovered file into front matter and body, using whichever format claims it.
        let readFrontMatter (file: DiscoveredFile) =
            let extension = (RelativePath.extension file.RelativePath).ToLowerInvariant()

            let claiming =
                registry.FrontMatterFormats
                |> List.filter (fun format -> List.contains extension format.Extensions)

            let asDiagnostic message =
                let diagnostic = Diagnostic.error "nacara/unknown-front-matter-format" message

                match file.Source with
                | FromFile path -> diagnostic |> Diagnostic.inFile path
                | Generated origin ->
                    diagnostic |> Diagnostic.withHint $"The content was generated by '%s{origin}'"

            match List.tryLast claiming with
            | None ->
                Error(
                    asDiagnostic
                        $"Nothing knows how to read the front matter of a '%s{extension}' file"
                    |> Diagnostic.withHint
                        "A plugin brings that knowledge: markdown for .md, literate for .fs and .fsx"
                )
            | Some format ->
                match FrontMatter.extract format file.Text with
                | Error message -> Error(asDiagnostic message)
                | Ok block ->
                    Ok
                        {
                            Site = file.Site
                            Locale = file.Locale
                            RelativePath = file.RelativePath
                            ProjectPath = file.ProjectPath
                            Source = file.Source
                            Text = file.Text
                            Dependencies = file.Dependencies
                            FrontMatter = block
                        }

        let pages =
            registry.Collections
            |> List.collect (fun definition ->
                discover
                    projectRoot
                    site
                    (registry.FrontMatterFormats |> List.collect _.Extensions |> List.distinct)
                    diagnostics
                    definition
                |> List.map (resolveLocale site)
                |> Array.ofList
                |> parallelMap (fun file ->
                    let key =
                        $"%s{definition.Name}:%s{RelativePath.value file.RelativePath}:%s{string file.Locale}:%s{BuildCache.Hash file.Text}"

                    cache.Load(key, fun () -> readFrontMatter file |> Result.bind definition.Load)
                )
                |> List.ofArray
            )
            |> List.choose (fun result ->
                match result with
                | Ok page -> Some page
                | Error diagnostic ->
                    diagnostics.Add diagnostic
                    None
            )

        phase "load"

        let pages =
            if not site.FallBackToDefaultLocale || List.length site.Locales < 2 then
                pages
            else
                let root = Site.rootLocale site

                let translated =
                    pages
                    |> List.map (fun page ->
                        page.Locale.Code, page.Collection, Route.translationKey page.Route
                    )
                    |> Set.ofList

                let fallbacks =
                    [
                        for locale in site.Locales do
                            if locale.Code <> root.Code then
                                for page in pages do
                                    if
                                        page.Locale.Code = root.Code
                                        && not (
                                            translated.Contains(
                                                locale.Code,
                                                page.Collection,
                                                Route.translationKey page.Route
                                            )
                                        )
                                    then
                                        { page with
                                            Id = $"%s{page.Id}@%s{locale.Code}"
                                            Locale = locale
                                            Route =
                                                { page.Route with
                                                    Locale = locale
                                                }
                                        }
                                            .WithData(PageData.UntranslatedFrom, root.Code)
                    ]

                if not (List.isEmpty fallbacks) then
                    Log.debug $"%i{List.length fallbacks} pages fall back to '%s{root.Code}'"

                pages @ fallbacks

        reportDuplicateRoutes diagnostics pages

        let transformContext =
            {
                Site = siteInfo
                Pages = pages
                Diagnostics = DiagnosticSink(diagnostics, Registry.engineSource)
                ProjectRoot = projectRoot
                Registry = registry
            }

        let pages =
            pages
            |> Array.ofList
            |> parallelMap (applyTransforms registry transformContext cache)
            |> List.ofArray

        phase "transform"

        let mutable written = 0
        let mutable unchanged = 0
        let mutable pruned = 0
        let produced = System.Collections.Generic.HashSet<string>()

        let track changed =
            if changed then
                written <- written + 1
            else
                unchanged <- unchanged + 1

        let produce (destination: AbsolutePath) changed =
            if not (produced.Add(AbsolutePath.value destination)) then
                let relative = RelativePath.fromRoot outputDirectory destination

                diagnostics.Add(
                    Diagnostic.warning
                        "nacara/duplicate-output"
                        $"'%s{RelativePath.value relative}' is written more than once in one build"
                    |> Diagnostic.withHint
                        "Two things produce this file - a static file and a plugin, most likely. Remove one, or the file will change on every build."
                )

            track changed

        /// Copy a directory into the output, keeping its shape. Sorted, so a build produces its
        /// files in the same order every time.
        let copyDirectoryInto (destination: AbsolutePath) (source: AbsolutePath) =
            let sourceRoot = AbsolutePath.value source

            if Directory.Exists sourceRoot then
                let files =
                    Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories)
                    |> Seq.sort

                for file in files do
                    let file = AbsolutePath.create file
                    let relative = RelativePath.fromRoot source file
                    let target = AbsolutePath.combine destination [ RelativePath.value relative ]
                    produce target (cache.CopyIfChanged(file, target))

        let hookContext =
            {
                Site = siteInfo
                IsWatch = isWatch
                Writes = cache.Writes
                ProjectRoot = projectRoot
                OutputDirectory = outputDirectory
                Pages = pages
                Diagnostics = DiagnosticSink(diagnostics, Registry.engineSource)
                Write =
                    fun relative content ->
                        let destination = AbsolutePath.combine outputDirectory [ relative ]
                        let changed = cache.WriteIfChanged(destination, content)
                        produce destination changed
                        changed
            }

        registry.PagesRoutedHooks |> List.iter (fun hook -> hook hookContext)
        phase "hooks"

        if not diagnostics.HasErrors then
            let byName =
                registry.Collections |> List.map (fun item -> item.Name, item) |> Map.ofList

            /// <summary>Everything the build writes goes through the transforms claiming its
            /// extension - a page as much as a stylesheet, so minifying is one idea and not two.
            /// </summary>
            let transformed (path: RelativePath) (content: string) =
                let extension = (RelativePath.extension path).ToLowerInvariant()

                registry.AssetTransforms
                |> List.filter (fun transform ->
                    List.isEmpty transform.Extensions
                    || List.contains extension transform.Extensions
                )
                |> List.fold
                    (fun content transform ->
                        transform.Transform
                            {
                                Path = path
                                Content = content
                                Diagnostics = DiagnosticSink(diagnostics, Registry.engineSource)
                                IsWatch = isWatch
                            }
                    )
                    content

            // F# gives structural hashing a budget of about 32 elements and spends it on a list's tail, so this folds one page at a time.
            let shape =
                pages
                |> List.fold
                    (fun acc page ->
                        let page =
                            hash (
                                page.Id,
                                page.Collection,
                                page.Route,
                                page.Title,
                                page.Order,
                                page.Locale,
                                hash page.FrontMatter
                            )

                        acc * 31 + page
                    )
                    17

            pages
            |> Array.ofList
            |> parallelMap (fun page ->
                match Map.tryFind page.Collection byName with
                | None -> None
                | Some definition ->
                    let relative = Url.outputPath page.Route

                    let key =
                        $"%i{shape}:%s{page.Id}:%s{BuildCache.Hash page.Html}:%i{hash page.Headings}:%i{hash page.FrontMatter}:%i{hash page.Data}"

                    let html =
                        cache.Render(
                            key,
                            fun () ->
                                definition.Render
                                    {
                                        Site = siteInfo
                                        Pages = pages
                                        Page = page
                                        Content = page.Html
                                    }
                                |> Render.htmlDocument
                        )
                        |> transformed relative

                    let destination =
                        AbsolutePath.combine outputDirectory [ RelativePath.value relative ]

                    Some(destination, cache.WriteIfChanged(destination, html))
            )
            |> Array.iter (
                function
                | Some(destination, changed) -> produce destination changed
                | None -> ()
            )

            phase "render"

            let bundleGroup = "bundles"

            let bundleKey sources =
                let hash = sources |> List.map snd |> String.concat "\n" |> BuildCache.Hash
                hash.Substring(0, 8).ToLowerInvariant()

            registry.Assets
            |> List.choose (
                function
                | Bundle(sources, _, _) -> Some(bundleKey sources)
                | _ -> None
            )
            |> ProjectCache.forgetOthers projectRoot bundleGroup

            for asset in registry.Assets do
                let destination =
                    AbsolutePath.combine outputDirectory [ RelativePath.value asset.Destination ]

                match asset with
                | WriteText(content, _) ->
                    produce
                        destination
                        (cache.WriteIfChanged(destination, transformed asset.Destination content))
                | WriteBytes(content, _) ->
                    produce destination (cache.WriteBytesIfChanged(destination, content))
                | CopyFile(source, _) ->
                    produce destination (cache.CopyIfChanged(source, destination))
                | CopyDirectory(source, _) -> copyDirectoryInto destination source
                | Bundle(sources, entry, _) ->
                    let extension = (RelativePath.extension asset.Destination).ToLowerInvariant()

                    let staged =
                        let directory =
                            ProjectCache.directory projectRoot bundleGroup (bundleKey sources)

                        for name, content in sources do
                            let file = AbsolutePath.combine directory [ name ]
                            let path = AbsolutePath.value file

                            if not (File.Exists path) || File.ReadAllText path <> content then
                                File.WriteAllText(path, content)

                        directory

                    let emitSources () =
                        let beside = AbsolutePath.directory destination

                        for name, content in sources do
                            let target =
                                if name = entry then
                                    destination
                                else
                                    AbsolutePath.combine beside [ name ]

                            produce target (cache.WriteIfChanged(target, content))

                    let bundled =
                        registry.AssetBundlers
                        |> List.filter (fun bundler -> List.contains extension bundler.Extensions)
                        |> List.tryLast
                        |> Option.map (fun bundler ->
                            bundler.Bundle
                                {
                                    Entry = AbsolutePath.combine staged [ entry ]
                                    Path = asset.Destination
                                    Diagnostics =
                                        DiagnosticSink(diagnostics, Registry.engineSource)
                                    IsWatch = isWatch
                                }
                        )

                    match bundled with
                    | Some(Ok content) ->
                        produce
                            destination
                            (cache.WriteIfChanged(
                                destination,
                                transformed asset.Destination content
                            ))
                    | Some(Error message) ->
                        diagnostics.Add(
                            Diagnostic.warning
                                "asset-not-bundled"
                                $"%s{RelativePath.value asset.Destination} was not bundled: %s{message}"
                            |> Diagnostic.withHint
                                "Its sources are served as they are, which is correct unless the reason above is a reference that could not be resolved."
                        )

                        emitSources ()
                    | None -> emitSources ()

            match site.StaticDirectory with
            | Some directory ->
                copyDirectoryInto outputDirectory (AbsolutePath.combine projectRoot [ directory ])
            | None -> ()

            phase "assets"
            registry.BuildCompleteHooks |> List.iter (fun hook -> hook hookContext)
            phase "post-build"

            if cache.Writes && Directory.Exists(AbsolutePath.value outputDirectory) then
                let preserved =
                    registry.Preserved
                    |> List.map (fun path ->
                        AbsolutePath.value (AbsolutePath.combine outputDirectory [ path ])
                    )

                let files =
                    Directory.EnumerateFiles(
                        AbsolutePath.value outputDirectory,
                        "*",
                        SearchOption.AllDirectories
                    )

                for file in files do
                    let normalized = AbsolutePath.value (AbsolutePath.create file)

                    let isPreserved =
                        preserved
                        |> List.exists (fun path ->
                            normalized = path || normalized.StartsWith(path + "/")
                        )

                    if not (produced.Contains normalized) && not isPreserved then
                        File.Delete normalized
                        pruned <- pruned + 1

                let directories =
                    Directory.EnumerateDirectories(
                        AbsolutePath.value outputDirectory,
                        "*",
                        SearchOption.AllDirectories
                    )
                    |> Seq.sortByDescending _.Length

                for directory in directories do
                    if
                        Directory.Exists directory
                        && Seq.isEmpty (Directory.EnumerateFileSystemEntries directory)
                    then
                        Directory.Delete directory

        stopwatch.Stop()

        {
            Pages = pages
            Diagnostics = diagnostics.ToList() |> List.ofSeq
            WrittenFiles = written
            UnchangedFiles = unchanged
            PrunedFiles = pruned
            Elapsed = stopwatch.Elapsed
        }

    /// <summary>Run a complete build.</summary>
    /// <param name="cache">What an earlier build left behind, to skip the expensive parts.</param>
    /// <param name="projectRoot">Where the site's content and output live.</param>
    /// <param name="site">The site to build.</param>
    let runWith (cache: BuildCache) (projectRoot: AbsolutePath) (site: Site) =
        runCore cache projectRoot false site

    /// <summary>Run a build that is part of a watch session.</summary>
    /// <param name="cache">What the last build in this session left behind.</param>
    /// <param name="projectRoot">Where the site's content and output live.</param>
    /// <param name="site">The site to build.</param>
    let runWatch (cache: BuildCache) (projectRoot: AbsolutePath) (site: Site) =
        runCore cache projectRoot true site

    /// <summary>Run a build with a fresh cache.</summary>
    /// <param name="projectRoot">Where the site's content and output live.</param>
    /// <param name="site">The site to build.</param>
    let run (projectRoot: AbsolutePath) (site: Site) = runWith (BuildCache()) projectRoot site

    /// <summary>
    /// Build the site without writing any of it.
    /// </summary>
    /// <remarks>Everything a build does except the last step: pages are rendered, links and anchors
    /// are resolved, plugins run, and whatever would have been written is counted instead.</remarks>
    /// <param name="projectRoot">Where the site's content and output live.</param>
    /// <param name="site">The site to build.</param>
    let check (projectRoot: AbsolutePath) (site: Site) =
        runWith (BuildCache false) projectRoot site

    /// <summary>Delete the output directory and the cache beside it.</summary>
    /// <param name="projectRoot">Where the site's content and output live.</param>
    /// <param name="site">The site whose output to remove.</param>
    let clean (projectRoot: AbsolutePath) (site: Site) =
        let output = AbsolutePath.combine projectRoot [ site.OutputDirectory ]

        if Directory.Exists(AbsolutePath.value output) then
            Directory.Delete(AbsolutePath.value output, true)

        ProjectCache.clear projectRoot
