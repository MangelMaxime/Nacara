namespace Nacara.Core

/// <summary>
/// A site, described as a value.
/// </summary>
/// <remarks>
/// You build it with a plain pipeline - <c>Site.create … |> Site.baseUrl … |> Site.plugin …</c> -
/// so your configuration is an ordinary value: share it between builds, reuse it in tests, and let
/// the compiler tell you when it is wrong.
/// </remarks>
type Site =
    {
        Title: string
        Description: string option
        BaseUrl: string
        /// Absolute origin the site is published at, for example <c>https://example.com</c>.
        /// Needed by anything that must produce a full URL: canonical links, sitemaps, feeds.
        Origin: string option
        /// Set when this build is one version among several deployed side by side.
        VersionPrefix: string option
        Locales: Locale list
        /// Generate a page in the default locale's content for locales that have no translation
        /// of it yet, rather than leaving a hole in the site.
        FallBackToDefaultLocale: bool
        /// Directory receiving the built site, relative to the project root.
        OutputDirectory: string
        /// Directory copied verbatim into the output, relative to the project root.
        StaticDirectory: string option
        /// Stylesheets of your own, linked on every page. Paths are relative to the project root.
        Stylesheets: string list
        /// Scripts of your own, loaded on every page. Paths are relative to the project root.
        Scripts: string list
        Plugins: IPlugin list
        Collections: CollectionDefinition list
    }

[<RequireQualifiedAccess>]
module Site =

    /// <summary>A site with the defaults of every field, ready to be described.</summary>
    /// <param name="title">What your site is called. It becomes the <c>&lt;title&gt;</c>, the brand
    /// in the navbar, and the site a search result says a page belongs to.</param>
    let create (title: string) =
        {
            Title = title
            Description = None
            BaseUrl = "/"
            Origin = None
            VersionPrefix = None
            Locales = [ Locale.root "en" ]
            FallBackToDefaultLocale = true
            OutputDirectory = "docs_deploy"
            StaticDirectory = Some "static"
            Stylesheets = []
            Scripts = []
            Plugins = []
            Collections = []
        }

    /// <summary>A stylesheet of your own, linked on every page after the theme's.</summary>
    /// <remarks>
    /// Bundled and minified like any other, so it may <c>@import</c> its neighbours - every
    /// stylesheet beside it is given to the bundler. Files under
    /// <see cref="P:Nacara.Core.Site.StaticDirectory" /> are copied untouched instead.
    /// </remarks>
    /// <param name="path">The stylesheet, relative to the project root.</param>
    /// <param name="site">The site being described.</param>
    let stylesheet (path: string) (site: Site) =
        { site with
            Stylesheets = site.Stylesheets @ [ path ]
        }

    /// <summary>A script of your own, loaded on every page.</summary>
    /// <remarks>
    /// Loaded as a classic script, so an <c>import</c> in it needs a bundler registered - unlike a
    /// stylesheet, whose imports a browser would resolve on its own.
    /// </remarks>
    /// <param name="path">The script, relative to the project root.</param>
    /// <param name="site">The site being described.</param>
    let script (path: string) (site: Site) =
        { site with
            Scripts = site.Scripts @ [ path ]
        }

    /// <summary>What your site is about, for pages that have nothing better to say.</summary>
    /// <param name="value">One sentence. It becomes the meta description of any page whose front
    /// matter carries none of its own.</param>
    /// <param name="site">The site being described.</param>
    let description value (site: Site) =
        { site with
            Description = Some(value: string)
        }

    /// <summary>The path the site is served from.</summary>
    /// <param name="value">A path with slashes at both ends: <c>/</c> at a domain root,
    /// <c>/MyLibrary/</c> under a project page. Every link the build emits goes through it, so you
    /// can move a site without editing a single page.</param>
    /// <param name="site">The site being described.</param>
    let baseUrl value (site: Site) =
        { site with
            BaseUrl = value
        }

    /// <summary>
    /// Where the site is published, for the URLs that have to be absolute.
    /// </summary>
    /// <remarks>
    /// Canonical links, sitemaps and social cards all need a full URL, and only you know where the
    /// site is deployed. Leave this out and they are left out too, rather than guessed at.
    /// </remarks>
    /// <param name="value">The scheme and host you deploy under, with no trailing slash:
    /// <c>https://example.github.io</c>.</param>
    /// <param name="site">The site being described.</param>
    let origin value (site: Site) =
        { site with
            Origin = Some((value: string).TrimEnd '/')
        }

    /// <summary>Deploy this build under a version prefix, alongside other versions.</summary>
    /// <param name="prefix">What this build is called in a URL, like <c>v2</c> or <c>next</c>. It
    /// becomes the first segment of every route the build writes.</param>
    /// <param name="site">The site being described.</param>
    let version prefix (site: Site) =
        { site with
            VersionPrefix = Some(prefix: string)
        }

    /// <summary>
    /// Locales of the site. Exactly one must be the root locale.
    /// </summary>
    /// <param name="values">The languages the site is written in. The root one is served from the
    /// base URL, the rest from a prefix of their code - see <see cref="M:Nacara.Core.Locale.root"/>
    /// and <see cref="M:Nacara.Core.Locale.other"/>.</param>
    /// <param name="site">The site being described.</param>
    let locales values (site: Site) =
        { site with
            Locales = values
        }

    /// <summary>
    /// Whether a locale with no translation of a page still gets that page.
    /// </summary>
    /// <remarks>
    /// On by default. A half-translated site should still be usable in either language, so an
    /// untranslated page shows its original content and says so, rather than 404ing or vanishing
    /// from the menu.
    /// </remarks>
    /// <param name="value"><c>true</c> to publish every page in every locale, translated or not;
    /// <c>false</c> to publish a page only in the locales that have it.</param>
    /// <param name="site">The site being described.</param>
    let fallBackToDefaultLocale value (site: Site) =
        { site with
            FallBackToDefaultLocale = value
        }

    /// <summary>Where the built site is written.</summary>
    /// <param name="value">A directory, relative to the project root. The build removes whatever it
    /// no longer produces, so do not keep anything of your own in there.</param>
    /// <param name="site">The site being described.</param>
    let output value (site: Site) =
        { site with
            OutputDirectory = value
        }

    /// <summary>A directory copied into the output as it is.</summary>
    /// <param name="value">Relative to the project root. Its contents land at the root of your
    /// site, so <c>static/logo.svg</c> is served as <c>/logo.svg</c>.</param>
    /// <param name="site">The site being described.</param>
    let staticFiles value (site: Site) =
        { site with
            StaticDirectory = Some(value: string)
        }

    /// <summary>Copy no directory as it is: every file your site publishes is produced.</summary>
    let noStaticFiles (site: Site) =
        { site with
            StaticDirectory = None
        }

    /// <summary>Add a plugin, which registers what it contributes when the build starts.</summary>
    /// <param name="value">The plugin. Plugins register in the order you add them, and that order
    /// decides the order of anything running in a sequence - transforms, hooks, assets.</param>
    /// <param name="site">The site being described.</param>
    let plugin (value: IPlugin) (site: Site) =
        { site with
            Plugins = site.Plugins @ [ value ]
        }

    /// <summary>Add a typed collection. Its front-matter type is erased here and nowhere else.</summary>
    /// <param name="value">A collection: where its content comes from, how its front matter is
    /// decoded, where its pages are routed, and what renders them.</param>
    /// <param name="site">The site being described.</param>
    let collection (value: Collection<'FrontMatter>) (site: Site) =
        { site with
            Collections = site.Collections @ [ Collection.build value ]
        }

    /// <summary>The root locale, or the first one when none was marked as root.</summary>
    /// <param name="site">The site being described.</param>
    let rootLocale (site: Site) =
        site.Locales
        |> List.tryFind _.IsRoot
        |> Option.defaultWith (fun () -> List.head site.Locales)

    /// <summary>The read-only view handed to layouts and plugins.</summary>
    let toInfo (site: Site) =
        {
            Title = site.Title
            Description = site.Description
            Url =
                {
                    BaseUrl = "/" + site.BaseUrl.Trim('/')
                    VersionPrefix = site.VersionPrefix
                }
            Origin = site.Origin
            Locales = site.Locales
            RootLocale = rootLocale site
            PageAssets = []
        }

    /// <summary>Configuration problems worth refusing to build over.</summary>
    let validate (site: Site) =
        [
            if List.isEmpty site.Locales then
                Diagnostic.error "nacara/no-locale" "A site must declare at least one locale"

            match site.Locales |> List.filter _.IsRoot with
            | []
            | [ _ ] -> ()
            | roots ->
                let codes = roots |> List.map _.Code |> String.concat ", "

                Diagnostic.error
                    "nacara/several-root-locales"
                    $"Only one locale can be the root locale, but %s{codes} all are"
                |> Diagnostic.withHint "Use Locale.other for every locale but one"

            match
                site.Locales |> List.countBy _.Code |> List.filter (fun (_, count) -> count > 1)
            with
            | [] -> ()
            | duplicates ->
                let codes = duplicates |> List.map fst |> String.concat ", "
                Diagnostic.error "nacara/duplicate-locale" $"Duplicated locale codes: %s{codes}"

            if List.isEmpty site.Collections then
                Diagnostic.warning
                    "nacara/no-collection"
                    "This site declares no collection, so it has no content"
                |> Diagnostic.withHint "Add one with Site.collection"
        ]
