namespace Nacara.Core

/// <summary>
/// A file the build must place in the output directory.
/// </summary>
type Asset =
    | CopyFile of source: AbsolutePath * destination: RelativePath
    | CopyDirectory of source: AbsolutePath * destination: RelativePath
    | WriteText of content: string * destination: RelativePath
    /// <summary>
    /// A file a plugin holds as bytes rather than as text or a path: a font, an image, a wasm
    /// grammar.
    /// </summary>
    | WriteBytes of content: byte array * destination: RelativePath
    /// <summary>
    /// A file that references others, with the files it references.
    /// </summary>
    /// <remarks>Written as one file when a bundler claims its extension, and as all of them when
    /// none does.</remarks>
    | Bundle of sources: (string * string) list * entry: string * destination: RelativePath

    member this.Destination =
        match this with
        | CopyFile(_, destination)
        | CopyDirectory(_, destination)
        | WriteText(_, destination)
        | WriteBytes(_, destination)
        | Bundle(_, _, destination) -> destination

/// <summary>
/// What a content transform can see while it works.
/// </summary>
[<ReferenceEquality>]
type TransformContext =
    {
        Site: SiteInfo
        /// Every page, already routed, which is how internal links can be resolved.
        Pages: Page list
        Diagnostics: DiagnosticSink
        ProjectRoot: AbsolutePath
        /// Everything plugins registered, so a transform can read extension points others filled.
        Registry: Registry
    }

    /// <summary>Find a page by the path an author would write in a link.</summary>
    /// <param name="collection">Which collection to look in.</param>
    /// <param name="path">The path as a link would write it, such as <c>guide/setup.md</c>.</param>
    member this.TryFindPage(collection: string, path: string) =
        let wanted = $"%s{collection}:%s{path}"
        this.Pages |> List.tryFind (fun page -> page.Id = wanted)

/// <summary>
/// Turns a page's raw body into HTML.
/// </summary>
/// <remarks>The extensions each transform claims decide which pages it sees.</remarks>
and ContentTransform =
    {
        Name: string
        /// Source extensions handled, lowercase and dotted. Empty means "every page".
        Extensions: string list
        Transform: TransformContext -> Page -> Page
    }

/// <summary>A text asset on its way to the output.</summary>
and CodeBlockContext =
    {
        /// The block as the engine parsed it, including the meta tokens it did not recognise.
        Block: CodeBlock
        /// The file it was written in, when it came from one rather than from a generated page.
        Source: AbsolutePath option
        /// The line the fence opens on, counted in the file rather than in the body.
        Line: int
        Diagnostics: DiagnosticSink
    }

/// <summary>
/// A rule about code blocks, run over every one the build renders.
/// </summary>
/// <remarks>
/// <para>A fence inside another fence is text and not a block, and the parser has already decided
/// that.</para>
/// <para>Checks run while a page is rendered, and their diagnostics are remembered with it, so a
/// page rebuilt from cache reports exactly what it reported when it was rendered.</para>
/// </remarks>
and CodeBlockCheck =
    {
        Name: string
        Check: CodeBlockContext -> unit
    }

and AssetTransformContext =
    {
        /// Where the asset will be written, relative to the output directory.
        Path: RelativePath
        Content: string
        Diagnostics: DiagnosticSink
        /// <summary>True while <c>watch</c> is rebuilding.</summary>
        /// <remarks>Minifiers skip themselves here unless the site asks otherwise.</remarks>
        IsWatch: bool
    }

/// <summary>
/// Changes a text asset on its way out - minifying it, for instance.
/// </summary>
/// <remarks>Runs before the asset is written rather than rewriting the file afterwards.</remarks>
and AssetTransform =
    {
        Name: string
        /// Extensions this applies to, lowercase and dotted. Empty means every text asset.
        Extensions: string list
        Transform: AssetTransformContext -> string
    }

and AssetBundleContext =
    {
        /// <summary>The entry file, laid out with the files it references beside it.</summary>
        Entry: AbsolutePath
        /// Where the result will be written, relative to the output directory.
        Path: RelativePath
        Diagnostics: DiagnosticSink
        /// True while <c>watch</c> is rebuilding.
        IsWatch: bool
    }

/// <summary>
/// Resolves what a file references into one file.
/// </summary>
and AssetBundler =
    {
        Name: string
        /// Extensions this claims, lowercase and dotted.
        Extensions: string list
        /// The bundled text, or why it could not be produced.
        Bundle: AssetBundleContext -> Result<string, string>
    }

/// <summary>One entry of a menu a plugin offers, and what hangs under it.</summary>
and MenuOutlineItem =
    {
        /// What a reader sees.
        Label: string
        /// <summary>The page it points at, relative to the collection's content root.</summary>
        /// <remarks><c>None</c> for an entry that is a heading over what is under it: a plugin
        /// may know how its pages group without having written a page for the group.</remarks>
        Page: string option
        /// What that page introduces, if anything.
        Children: MenuOutlineItem list
    }

/// <summary>
/// A menu a plugin offers for the pages it generates.
/// </summary>
/// <remarks>
/// A theme that finds no menu of its own for the section renders this one. Declare a menu for the
/// section yourself and yours is used instead.
/// </remarks>
and MenuOutline =
    {
        /// The section this is the menu of - the first segment of the routes it covers.
        Section: string
        Items: MenuOutlineItem list
    }

and HookContext =
    {
        Site: SiteInfo
        /// True while serving: expensive whole-site work can be skipped or deferred.
        IsWatch: bool
        /// <summary>False under <c>check</c>, which renders everything and writes none of it.</summary>
        /// <remarks>A hook that reads the output rather than the pages has nothing to read then, so
        /// it should say nothing rather than report what it did not find.</remarks>
        Writes: bool
        ProjectRoot: AbsolutePath
        OutputDirectory: AbsolutePath
        Pages: Page list
        /// <summary>Every page as the layout rendered it, whole document.</summary>
        /// <remarks>Empty until the pages are rendered, so only a build-complete hook sees it.
        /// The text is what the layout produced, before any asset transform.</remarks>
        Rendered: (Page * string) list
        Diagnostics: DiagnosticSink
        /// <summary>
        /// Write a file into the output, relative to it. Returns true when the bytes changed.
        /// </summary>
        /// <remarks>
        /// Write through this rather than touching the file system directly: it leaves a file alone
        /// when the content is identical, so a watcher or a deploy tool has nothing to react to,
        /// and it tells the build the file exists so pruning does not treat it as an orphan.
        /// </remarks>
        Write: string -> string -> bool
    }

/// <summary>
/// Everything plugins have contributed to a build.
/// </summary>
/// <remarks>
/// <c>Extras</c> is how plugins extend <em>each other</em> with the core knowing about neither: the
/// markdown plugin reads every <c>MarkdigExtension</c> registered, a theme every <c>NavbarItem</c>.
/// </remarks>
/// <summary>What a plugin's command is given when it runs.</summary>
and CommandContext =
    {
        Site: SiteInfo
        ProjectRoot: AbsolutePath
        /// Where a build of this site writes, whether or not one has run.
        OutputDirectory: AbsolutePath
        /// Everything typed after the command's name.
        Arguments: string list
    }

/// <summary>A subcommand a plugin adds to the site's command line.</summary>
/// <remarks>
/// For work a build should not be doing: rewriting your sources, clearing a cache, printing what a
/// plugin knows.
/// </remarks>
and PluginCommand =
    {
        /// What a reader types, after the site's own commands.
        Name: string
        /// One line, shown in <c>--help</c> beside the name.
        Summary: string
        /// <summary>What it does, given the site it was run in.</summary>
        /// <returns>The process's exit code: <c>0</c> when it worked.</returns>
        Run: CommandContext -> int
        /// <summary>What <c>&lt;command&gt; --help</c> prints.</summary>
        /// <remarks>The summary is all a reader gets without this, which does not tell them what
        /// the command takes. Say what the arguments are and show a line of it being used.</remarks>
        Help: string option
        /// <summary>The plugin that added it, filled in when it is registered.</summary>
        /// <remarks>Shown in the help, so a reader knows which package a command came with - and
        /// which one to keep if they want it.</remarks>
        Source: string
    }

and Registry =
    {
        Collections: CollectionDefinition list
        Transforms: ContentTransform list
        /// Rules run over every code block, in registration order.
        CodeBlockChecks: CodeBlockCheck list
        AssetTransforms: AssetTransform list
        AssetBundlers: AssetBundler list
        /// How files of each kind carry their front matter. The engine ships none, so your site
        /// reads the formats your plugins bring - which is why markdown needs the markdown plugin.
        FrontMatterFormats: FrontMatterFormat list
        Assets: Asset list
        PagesRoutedHooks: (HookContext -> unit) list
        BuildCompleteHooks: (HookContext -> unit) list
        /// Output paths that pruning must leave alone, because something outside the build owns
        /// them - a search index, a legacy directory, files a hook writes.
        Preserved: string list
        /// Subcommands the plugins added, in the order they were registered.
        Commands: PluginCommand list
        Extras: Map<string, obj list>
        /// <summary>
        /// The plugin being configured right now, or <c>nacara</c> outside of one.
        /// </summary>
        /// <remarks>
        /// Everything registered while it is set reports under it, so a diagnostic code names its
        /// plugin without you writing that name anywhere.
        /// </remarks>
        Source: string
    }

/// <summary>A unit of behaviour added to the engine.</summary>
type IPlugin =
    abstract Name: string
    abstract Configure: Registry -> Registry

[<RequireQualifiedAccess>]
module PluginCommand =

    /// <summary>A subcommand, ready to register.</summary>
    /// <param name="name">What a reader types.</param>
    /// <param name="summary">One line, for the help.</param>
    /// <param name="run">Given the site, where it writes and what was typed after the name, the
    /// exit code.</param>
    let create name summary run =
        {
            Name = name
            Summary = summary
            Run = run
            Help = None
            // Filled in by Registry.command, from the plugin doing the registering.
            Source = ""
        }

    /// <summary>What <c>&lt;command&gt; --help</c> prints.</summary>
    /// <remarks>Say what the arguments are, and show one being used.</remarks>
    /// <param name="value">The text, as it should appear.</param>
    /// <param name="command">The command being described.</param>
    let help value (command: PluginCommand) =
        { command with
            Help = Some(value: string)
        }

[<RequireQualifiedAccess>]
module Registry =

    /// <summary>What the engine's own diagnostics report under.</summary>
    let engineSource = "nacara"

    let empty =
        {
            Collections = []
            Transforms = []
            CodeBlockChecks = []
            AssetTransforms = []
            AssetBundlers = []
            FrontMatterFormats = []
            Assets = []
            PagesRoutedHooks = []
            BuildCompleteHooks = []
            Preserved = []
            Commands = []
            Extras = Map.empty
            Source = engineSource
        }

    let private scopeHook source (hook: HookContext -> unit) =
        fun (context: HookContext) ->
            hook
                { context with
                    Diagnostics = context.Diagnostics.For source
                }

    /// <summary>Add a collection the way a site would, from inside a plugin.</summary>
    /// <param name="definition">A built collection. Its front-matter type is already erased, so a
    /// plugin can carry one without the site naming that type.</param>
    /// <param name="registry">What the plugin is contributing to.</param>
    let collection (definition: CollectionDefinition) (registry: Registry) =
        { registry with
            Collections = registry.Collections @ [ definition ]
        }

    /// <summary>Change a page's content on its way through the pipeline.</summary>
    /// <param name="transform">Runs once per page, in registration order, and returns the page as
    /// it should continue. Markdown rendering is one of these.</param>
    /// <param name="registry">What the plugin is contributing to.</param>
    let transform (transform: ContentTransform) (registry: Registry) =
        let source = registry.Source

        let scoped =
            { transform with
                Transform =
                    fun context page ->
                        transform.Transform
                            { context with
                                Diagnostics = context.Diagnostics.For source
                            }
                            page
            }

        { registry with
            Transforms = registry.Transforms @ [ scoped ]
        }

    /// <summary>Add a rule run over every code block the build renders.</summary>
    /// <remarks>
    /// The block arrives already parsed, with the file and line it was written at, so a plugin
    /// reporting on a fence writes a diagnostic that points at it. Reading
    /// <c>Block.Meta.Unknown</c> is how a plugin sees the annotations it brought and the engine
    /// does not know about.
    /// </remarks>
    /// <param name="check">Runs once per code block, in registration order.</param>
    /// <param name="registry">What the plugin is contributing to.</param>
    let codeBlockCheck (check: CodeBlockCheck) (registry: Registry) =
        let source = registry.Source

        let scoped =
            { check with
                Check =
                    fun context ->
                        check.Check
                            { context with
                                Diagnostics = context.Diagnostics.For source
                            }
            }

        { registry with
            CodeBlockChecks = registry.CodeBlockChecks @ [ scoped ]
        }

    /// <summary>Teach the engine how a kind of file carries its front matter.</summary>
    /// <param name="format">Which extensions it claims, and how it splits a file into front matter
    /// and body. If two formats claim one extension you get
    /// <c>nacara/duplicate-front-matter-format</c>.</param>
    /// <param name="registry">What the plugin is contributing to.</param>
    let frontMatter (format: FrontMatterFormat) (registry: Registry) =
        { registry with
            FrontMatterFormats = registry.FrontMatterFormats @ [ format ]
        }

    /// <summary>Add a subcommand to the site's command line.</summary>
    /// <remarks>
    /// It appears in <c>--help</c> and runs instead of a build, so it is where a plugin puts the
    /// work a build should not do - rewriting sources, clearing a cache. A plugin's own name is a
    /// good prefix when it might collide.
    /// </remarks>
    /// <param name="command">What to add.</param>
    /// <param name="registry">What the plugin is contributing to.</param>
    let command (command: PluginCommand) (registry: Registry) =
        { registry with
            Commands =
                registry.Commands
                @ [
                    { command with
                        Source = registry.Source
                    }
                ]
        }

    /// <summary>Change text assets on their way to the output.</summary>
    /// <param name="transform">Given an asset's path and text, return what to write instead.
    /// Minifying CSS is one of these, and anything that is not text passes it by.</param>
    /// <param name="registry">What the plugin is contributing to.</param>
    let assetTransform (transform: AssetTransform) (registry: Registry) =
        let source = registry.Source

        let scoped =
            { transform with
                Transform =
                    fun context ->
                        transform.Transform
                            { context with
                                Diagnostics = context.Diagnostics.For source
                            }
            }

        { registry with
            AssetTransforms = registry.AssetTransforms @ [ scoped ]
        }

    /// <summary>Resolve what an asset references into one file.</summary>
    /// <param name="bundler">Given the entry file, the one file it stands for.</param>
    /// <param name="registry">What the plugin is contributing to.</param>
    let assetBundler (bundler: AssetBundler) (registry: Registry) =
        let source = registry.Source

        let scoped =
            { bundler with
                Bundle =
                    fun context ->
                        bundler.Bundle
                            { context with
                                Diagnostics = context.Diagnostics.For source
                            }
            }

        { registry with
            AssetBundlers = registry.AssetBundlers @ [ scoped ]
        }

    /// <summary>Ship a file with the site: a stylesheet, a script, anything copied or written.</summary>
    /// <param name="asset">What to put in the output, and where. If two assets want one path you
    /// get <c>nacara/duplicate-output</c>.</param>
    /// <param name="registry">What the plugin is contributing to.</param>
    let asset (asset: Asset) (registry: Registry) =
        { registry with
            Assets = registry.Assets @ [ asset ]
        }

    /// <summary>
    /// Keep an output path even though no build step produces it.
    /// </summary>
    /// <remarks>
    /// The build deletes output that nothing produces any more, which is what stops a page you
    /// deleted from staying online. Anything a build-complete hook writes - a search index, say -
    /// is invisible to that bookkeeping, so it has to say so here.
    /// </remarks>
    /// <param name="path">A path in the output, relative to its root, that this build did not
    /// write but that pruning must leave alone.</param>
    /// <param name="registry">What the plugin is contributing to.</param>
    let preserve (path: string) (registry: Registry) =
        { registry with
            Preserved = registry.Preserved @ [ path.Trim('/') ]
        }

    /// <summary>Run after every page has a route, before rendering.</summary>
    /// <param name="hook">Given every page and where it will be published. This is the moment to
    /// check links, build a menu, or report on the site as a whole.</param>
    /// <param name="registry">What the plugin is contributing to.</param>
    let onPagesRouted (hook: HookContext -> unit) (registry: Registry) =
        let scoped = scopeHook registry.Source hook

        { registry with
            PagesRoutedHooks = registry.PagesRoutedHooks @ [ scoped ]
        }

    /// <summary>Run after every file has been written: search indexes, sitemaps, manifests.</summary>
    /// <param name="hook">Given the finished site. Declare whatever it writes with
    /// <see cref="M:Nacara.Core.Registry.preserve"/>, or the next build prunes it.</param>
    /// <param name="registry">What the plugin is contributing to.</param>
    let onBuildComplete (hook: HookContext -> unit) (registry: Registry) =
        let scoped = scopeHook registry.Source hook

        { registry with
            BuildCompleteHooks = registry.BuildCompleteHooks @ [ scoped ]
        }

    let private key<'T> = typeof<'T>.FullName

    /// <summary>Contribute a typed value that other plugins can read back.</summary>
    /// <param name="value">Anything: a stylesheet to load, a highlighter, a code-block renderer.
    /// The type is the contract, so nobody has to agree on a name.</param>
    /// <param name="registry">What the plugin is contributing to.</param>
    let extra (value: 'T) (registry: Registry) =
        let existing = registry.Extras |> Map.tryFind key<'T> |> Option.defaultValue []

        { registry with
            Extras = registry.Extras |> Map.add key<'T> (existing @ [ box value ])
        }

    /// <summary>Every value of type <c>'T</c> contributed by any plugin, in registration order.</summary>
    let extras<'T> (registry: Registry) : 'T list =
        registry.Extras
        |> Map.tryFind key<'T>
        |> Option.defaultValue []
        |> List.map (fun value -> value :?> 'T)

    /// <summary>Apply every plugin, in order.</summary>
    /// <param name="plugins">The site's plugins. Each one's contributions are stamped with its
    /// name, and that is the name its diagnostics report under.</param>
    /// <returns>Everything the plugins contributed, as one registry.</returns>
    let ofPlugins (plugins: IPlugin list) =
        let configured =
            plugins
            |> List.fold
                (fun registry (plugin: IPlugin) ->
                    plugin.Configure
                        { registry with
                            Source = plugin.Name
                        }
                )
                empty

        { configured with
            Source = engineSource
        }
