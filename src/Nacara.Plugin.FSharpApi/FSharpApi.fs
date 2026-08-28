namespace Nacara.Plugins

open System
open System.IO
open Nacara.Core
open Nacara.Plugins.Internal

/// <summary>An assembly to document, and where its dependencies are.</summary>
type FSharpApiSource =
    {
        /// Path of the assembly, relative to the project root.
        Path: string
        /// Extra directories to look in for its dependencies.
        SearchPaths: string list
    }

[<RequireQualifiedAccess>]
module FSharpApiSource =

    /// <summary>An assembly to document.</summary>
    /// <param name="path">Where it is, relative to the project root - or absolute, which is how a
    /// site points at the directory it is itself running from.</param>
    let create (path: string) =
        {
            Path = path
            SearchPaths = []
        }

    /// <summary>Where else to look for what the assembly references.</summary>
    /// <param name="paths">Directories holding its dependencies. Beside the assembly, the running
    /// runtime and the site's own directory are searched without being asked.</param>
    /// <param name="source">The assembly being described.</param>
    let searchPaths (paths: string list) (source: FSharpApiSource) =
        { source with
            SearchPaths = paths
        }

/// <summary>Options of the F# API reference plugin.</summary>
type FSharpApiOptions =
    {
        Sources: FSharpApiSource list
        /// Route prefix the pages are published under.
        Root: string
        /// Title of the page that lists the namespaces.
        Title: string
        /// <summary>Namespaces to leave out, with everything under them.</summary>
        /// <remarks>A namespace F# has to make public for the library to compile, but that no
        /// reader should be sent to - <c>My.Library.Internal</c> and its like.</remarks>
        Exclude: string list
        /// <summary>Report a member that takes parameters and documents none of them.</summary>
        /// <remarks>Off by default. On, it is a warning per member, which <c>check</c> turns
        /// into a failure.</remarks>
        WarnOnUndocumented: bool
    }

/// <summary>
/// API reference pages for F# libraries.
/// </summary>
/// <remarks>
/// The pages are generated from the assemblies a library ships and go through the pipeline like
/// any other content: same theme, same highlighting, same search index, same link checking.
/// </remarks>
[<RequireQualifiedAccess>]
module FSharpApi =

    let defaults =
        {
            Sources = []
            Root = "api"
            Title = "API reference"
            Exclude = []
            WarnOnUndocumented = false
        }

    /// <summary>What the assemblies are right now, cheaply.</summary>
    let private stampOf (searchPaths: string list) (paths: AbsolutePath list) =
        let stamp (path: AbsolutePath) =
            let file = FileInfo(AbsolutePath.value path)
            AbsolutePath.value path, file.Length, file.LastWriteTimeUtc

        searchPaths, paths |> List.map stamp

    let private readGate = obj ()
    let mutable private lastReadKey = None
    let mutable private lastRead: Result<FSharpApiAssembly, string> list = []

    /// <summary>
    /// The assemblies, opened again only when one of them has changed.
    /// </summary>
    let private readCached (searchPaths: string list) (paths: AbsolutePath list) =
        lock
            readGate
            (fun () ->
                let key =
                    stampOf
                        searchPaths
                        (paths |> List.filter (fun path -> File.Exists(AbsolutePath.value path)))

                if lastReadKey = Some key then
                    lastRead
                else
                    let read = Reader.readAllWith searchPaths paths
                    lastReadKey <- Some key
                    lastRead <- read
                    read
            )

    /// <summary>What the assemblies declare.</summary>
    /// <param name="projectRoot">What the sources' relative paths are resolved against.</param>
    /// <param name="options">Which assemblies to read, and what to leave out.</param>
    /// <returns>One entry per assembly that could be read. One that could not is left out here and
    /// reported by the collection, which has somewhere to report to.</returns>
    let readFrom (projectRoot: AbsolutePath) (options: FSharpApiOptions) =
        let searchPaths = options.Sources |> List.collect _.SearchPaths |> List.distinct

        let paths =
            options.Sources
            |> List.map (fun source -> AbsolutePath.combine projectRoot [ source.Path ])

        readCached searchPaths paths
        |> List.choose (
            function
            | Ok assembly -> Some assembly
            | Error _ -> None
        )

    /// <summary>
    /// The namespaces of several assemblies, one entry per package that declares one.
    /// </summary>
    /// <param name="assemblies">What was read, in the order it was asked for.</param>
    /// <returns>One entry per namespace, its declarations sorted by name.</returns>
    let namespaces (assemblies: FSharpApiAssembly list) =
        assemblies
        |> List.collect _.Namespaces
        |> List.groupBy (fun ns -> ns.Assembly, ns.Name)
        |> List.map (fun (_, parts) ->
            { List.head parts with
                Entities = parts |> List.collect _.Entities |> List.sortBy _.Name
            }
        )
        |> List.sortBy (fun ns -> ns.Assembly, ns.Name)

    /// <summary>Whether a namespace is one the options said to leave out.</summary>
    let private published (options: FSharpApiOptions) (ns: FSharpApiNamespace) =
        options.Exclude
        |> List.exists (fun excluded -> ns.Name = excluded || ns.Name.StartsWith(excluded + "."))
        |> not

    /// <summary>What the assemblies declare, read from where the command line says.</summary>
    /// <param name="options">Which assemblies to read, and what to leave out.</param>
    let read (options: FSharpApiOptions) =
        readFrom (AbsolutePath.create (Nacara.defaultProjectRoot ())) options

    /// <summary>
    /// The namespaces and what each declares, for a site building its own menu.
    /// </summary>
    /// <remarks>
    /// <para>Names and page paths, nothing else - a menu belongs to the theme:</para>
    /// <code lang="fsharp">
    /// let rec entry (item: FSharpApiOutlineEntry) =
    ///     match item.Children with
    ///     | [] -> Menu.page item.Page
    ///     | children -> Menu.group item.Page [ for child in children -> entry child ]
    ///
    /// Api.outline options
    /// |> List.map (fun ns -> Menu.group ns.Page [ for item in ns.Entries -> entry item ])
    /// </code>
    /// <para>A namespace offers what it declares at the top level; what a module or type declares
    /// hangs under it.</para>
    /// </remarks>
    /// <param name="projectRoot">What the sources' relative paths are resolved against.</param>
    /// <param name="options">Which assemblies to read, and what to leave out.</param>
    let outlineFrom
        (projectRoot: AbsolutePath)
        (options: FSharpApiOptions)
        : FSharpApiOutlinePackage list
        =
        readFrom projectRoot options
        |> namespaces
        |> List.filter (published options)
        |> List.map (fun ns ->
            let rec entry (entity: FSharpApiEntity) =
                {
                    Name = entity.Name
                    Page = $"%s{entity.Slug}.md"
                    Children = entity.Nested |> List.map entry |> List.sortBy _.Name
                }

            {
                Name = ns.Name
                Assembly = ns.Assembly
                Page = $"%s{ns.Slug}/index.md"
                Entries = ns.Entities |> List.map entry |> List.sortBy _.Name
            }
        )
        |> List.groupBy _.Assembly
        |> List.map (fun (package, namespaces) ->
            {
                Name = package
                Page = $"%s{Slug.create package}/index.md"
                Namespaces = namespaces |> List.sortBy _.Name
            }
        )
        |> List.sortBy _.Name

    /// <summary>The same, read from where the command line says the project root is.</summary>
    /// <param name="options">Which assemblies to read, and what to leave out.</param>
    let outline (options: FSharpApiOptions) =
        outlineFrom (AbsolutePath.create (Nacara.defaultProjectRoot ())) options

    /// <summary>
    /// The reference, rebuilt only when something it is made of has changed.
    /// </summary>
    let mutable private lastKey = None
    let mutable private lastPages: GeneratedContent list = []
    let mutable private lastFound: Diagnostic list = []

    /// <summary>
    /// A collection whose pages are the API reference.
    /// </summary>
    /// <param name="name">What you call the collection, and what a menu refers to it by.</param>
    /// <param name="decoder">Reads the front matter the plugin writes - a title, a description -
    /// into the site's own type, so these pages carry what every other page carries.</param>
    /// <param name="options">Which assemblies to read, where to publish them, what to leave
    /// out.</param>
    let collection (name: string) (decoder: Decoder<'FrontMatter>) (options: FSharpApiOptions) =
        Collection.create name decoder
        |> Collection.routePrefix options.Root
        |> Collection.producer
            "fsharp-api"
            (fun context ->
                let key =
                    stampOf
                        (options.Sources |> List.collect _.SearchPaths |> List.distinct)
                        (options.Sources
                         |> List.map (fun source ->
                             AbsolutePath.combine context.ProjectRoot [ source.Path ]
                         )
                         |> List.filter (fun path -> File.Exists(AbsolutePath.value path))),
                    AbsolutePath.value context.ProjectRoot,
                    context.Site.Url

                if lastKey = Some key then
                    for diagnostic in lastFound do
                        context.Diagnostics.Add diagnostic

                    lastPages
                else

                    let found = ResizeArray<Diagnostic>()

                    let wanted =
                        options.Sources
                        |> List.map (fun source ->
                            source, AbsolutePath.combine context.ProjectRoot [ source.Path ]
                        )

                    for _, path in wanted do
                        if not (File.Exists(AbsolutePath.value path)) then
                            found.Add(
                                Diagnostic.error
                                    "assembly-missing"
                                    $"No assembly at %s{AbsolutePath.toLog path}"
                                |> Diagnostic.withHint
                                    "The path is resolved from the project root, and the library has to be built first"
                            )

                    let searchPaths =
                        options.Sources |> List.collect _.SearchPaths |> List.distinct

                    let present =
                        wanted
                        |> List.map snd
                        |> List.filter (fun path -> File.Exists(AbsolutePath.value path))

                    let assemblies =
                        List.zip present (readCached searchPaths present)
                        |> List.choose (fun (path, result) ->
                            match result with
                            | Error message ->
                                found.Add(
                                    Diagnostic.error "unreadable" message
                                    |> Diagnostic.withHint
                                        "The assembly's own dependencies have to be reachable; add SearchPaths where they are"
                                )

                                None
                            | Ok assembly -> Some(path, assembly)
                        )

                    let link (slug: string) =
                        Url.ofPath context.Site.Url $"%s{options.Root}/%s{slug}" + "/"

                    let declared =
                        namespaces (assemblies |> List.map snd) |> List.filter (published options)

                    let pageOf =
                        [
                            for ns in declared do
                                let rec walk (entity: FSharpApiEntity) =
                                    [
                                        entity.Name, link entity.Slug
                                        for nested in entity.Nested do
                                            yield! walk nested
                                    ]

                                for entity in ns.Entities do
                                    yield! walk entity
                        ]
                        |> List.groupBy fst
                        |> List.choose (fun (name, pages) ->
                            match pages with
                            | [ (_, url) ] -> Some(name, url)
                            | _ -> None
                        )
                        |> Map.ofList

                    let resolve name = Map.tryFind name pageOf

                    if options.WarnOnUndocumented then
                        for ns in declared do
                            let rec check (entity: FSharpApiEntity) =
                                for item in entity.Members do
                                    // The compiler rejects a <param> for a nameless parameter.
                                    if
                                        List.length item.Parameters > 1
                                        && item.Parameters |> List.forall (fun p -> p.Name.IsSome)
                                        && item.Doc.Parameters.IsEmpty
                                        && item.Doc.Summary.IsSome
                                    then
                                        found.Add(
                                            Diagnostic.warning
                                                "undocumented-parameter"
                                                $"%s{ns.Name}.%s{entity.Name}.%s{item.Name} takes parameters and documents none"
                                            |> Diagnostic.withHint
                                                "Add <param name=\"…\"> for each one; F# asks for all of them once one is documented"
                                        )

                                for nested in entity.Nested do
                                    check nested

                            for entity in ns.Entities do
                                check entity

                    let sources = assemblies |> List.map fst

                    let named =
                        assemblies
                        |> List.map (fun (_, assembly) -> assembly.Name)
                        |> List.distinct

                    let showsAssembly = List.length named > 1

                    let content =
                        [
                            GeneratedContent.create
                                "index.md"
                                (Render.index' options.Title link declared)
                            |> GeneratedContent.dependsOn sources

                            for package, namespaces in declared |> List.groupBy _.Assembly do
                                GeneratedContent.create
                                    $"%s{Slug.create package}/index.md"
                                    (Render.package package link namespaces)
                                |> GeneratedContent.dependsOn sources

                            for ns in declared do
                                GeneratedContent.create
                                    $"%s{ns.Slug}/index.md"
                                    (Render.``namespace`` link resolve showsAssembly ns)
                                |> GeneratedContent.dependsOn sources

                                let rec pages (entity: FSharpApiEntity) =
                                    [
                                        GeneratedContent.create
                                            $"%s{entity.Slug}.md"
                                            (Render.entity link resolve showsAssembly entity)
                                        |> GeneratedContent.dependsOn sources

                                        for nested in entity.Nested do
                                            yield! pages nested
                                    ]

                                for entity in ns.Entities do
                                    yield! pages entity
                        ]

                    lastKey <- Some key
                    lastPages <- content
                    lastFound <- List.ofSeq found

                    for diagnostic in found do
                        context.Diagnostics.Add diagnostic

                    content
            )

    let private readResource =
        Resource.text (Reflection.Assembly.GetExecutingAssembly())

    let private apiCss = lazy readResource "fsharp-api.css"
    let private apiJs = lazy readResource "fsharp-api.js"

    /// <summary>The menu of the reference, as the plugin would have it.</summary>
    /// <remarks>A package, then what it declares - the namespace level only where a package declares
    /// more than one. A theme renders this when the site has not written a menu for the section
    /// itself.</remarks>
    let menu (options: FSharpApiOptions) =
        let rec item (entry: FSharpApiOutlineEntry) =
            {
                Label = entry.Name
                Page = Some entry.Page
                Children = entry.Children |> List.map item
            }

        {
            Section = options.Root
            Items =
                [
                    for package in outline options ->
                        {
                            Label = package.Name
                            Page = Some package.Page
                            Children =
                                match package.Namespaces with
                                | [ only ] -> only.Entries |> List.map item
                                | several ->
                                    [
                                        for ns in several ->
                                            {
                                                Label = ns.Name
                                                Page = Some ns.Page
                                                Children = ns.Entries |> List.map item
                                            }
                                    ]
                        }
                ]
        }

    type private FSharpApiPlugin(options: FSharpApiOptions) =
        interface IPlugin with
            member _.Name = "fsharp-api"

            member _.Configure registry =
                registry
                |> Registry.asset (
                    WriteText(apiCss.Value, RelativePath.create "assets/fsharp-api.css")
                )
                |> Registry.extra (Stylesheet "assets/fsharp-api.css")
                |> Registry.asset (
                    WriteText(apiJs.Value, RelativePath.create "assets/fsharp-api.js")
                )
                |> Registry.extra (Script("assets/fsharp-api.js", true))
                |> Registry.extra (menu options)

    /// <summary>Set <c>Sources</c>.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let sources value (options: FSharpApiOptions) =
        { options with
            Sources = value
        }

    /// <summary>Route prefix the pages are published under.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let root value (options: FSharpApiOptions) =
        { options with
            Root = value
        }

    /// <summary>Title of the page that lists the namespaces.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let title value (options: FSharpApiOptions) =
        { options with
            Title = value
        }

    /// <summary>Namespaces to leave out, with everything under them.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let exclude value (options: FSharpApiOptions) =
        { options with
            Exclude = value
        }

    /// <summary>Report a member that takes parameters and documents none of them.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let warnOnUndocumented value (options: FSharpApiOptions) =
        { options with
            WarnOnUndocumented = value
        }

    /// <summary>The plugin itself. The collection produces the pages.</summary>
    /// <param name="options">The same options the collection was given.</param>
    let create (options: FSharpApiOptions) = FSharpApiPlugin(options) :> IPlugin

    /// <summary>Add the API reference to a site.</summary>
    /// <param name="options">The same options the collection was given.</param>
    /// <param name="site">The site being described.</param>
    let register (options: FSharpApiOptions) (site: Site) = Site.plugin (create options) site
