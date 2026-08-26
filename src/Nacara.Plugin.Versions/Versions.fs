namespace Nacara.Plugins

open System.Reflection
open System.IO
open Nacara.Core
open Thoth.Json.Core
open Thoth.Json.System.Text.Json

/// <summary>One version of the documentation, as deployed.</summary>
type SiteVersion =
    {
        /// Shown in the switcher, for example <c>2.0</c> or <c>next</c>.
        Label: string
        /// URL segment this version lives under. Empty means the deployment root.
        Prefix: string
        /// The version a reader lands on by default.
        IsLatest: bool
    }

[<RequireQualifiedAccess>]
module SiteVersion =

    /// <summary>A version deployed under a prefix of its own.</summary>
    /// <param name="label">What the switcher calls it - <c>2.0</c>, <c>next</c>.</param>
    /// <param name="prefix">The first segment of its URLs, and what
    /// <c>--version</c> wrote them under.</param>
    let create label prefix =
        {
            Label = label
            Prefix = prefix
            IsLatest = false
        }

    /// <summary>The version served from the site root, under no prefix.</summary>
    /// <param name="label">What the switcher calls it.</param>
    let root label =
        {
            Label = label
            Prefix = ""
            IsLatest = true
        }

    /// <summary>The version a reader arriving without one should be reading.</summary>
    /// <param name="version">The version being described. Any other is shown with a notice saying
    /// it is not the current one.</param>
    let latest (version: SiteVersion) =
        { version with
            IsLatest = true
        }

/// <summary>Options of the versions plugin.</summary>
type VersionsOptions =
    {
        Versions: SiteVersion list
        /// Where the manifest is written, relative to the output directory.
        ManifestPath: string
        /// Show a notice at the top of every page of an older version.
        ShowOutdatedNotice: bool
    }

/// <summary>
/// Publishing several versions of a site side by side.
/// </summary>
/// <remarks>
/// <para>A version is a <em>build</em>, not a content dimension: each is built with its own prefix
/// into its own directory. An old version is never rebuilt, so it cannot break, and building the
/// current docs costs the same whether there is one version or twenty.</para>
/// <para>What those independent builds share is small - a manifest of what exists and a switcher
/// that rewrites the path - and that is all this plugin is.</para>
/// </remarks>
[<RequireQualifiedAccess>]
module Versions =

    let defaults =
        {
            Versions = []
            ManifestPath = "versions.json"
            ShowOutdatedNotice = true
        }

    let private readResource = Resource.text (Assembly.GetExecutingAssembly())

    let private switcherJs = lazy readResource "versions.js"
    let private switcherCss = lazy readResource "versions.css"

    let private escapeAttribute (value: string) =
        value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;")

    /// <summary>The file the switcher reads, listing every version of the site.</summary>
    /// <param name="currentPrefix">The prefix this build is deployed under, so the manifest can
    /// say which entry is the one being read.</param>
    /// <param name="options">The versions declared, and where the manifest is written.</param>
    let manifest (currentPrefix: string) (options: VersionsOptions) =
        Encode.list
            [
                for version in options.Versions ->
                    Encode.object
                        [
                            "label", Encode.string version.Label
                            "prefix", Encode.string version.Prefix
                            "latest", Encode.bool version.IsLatest
                            "current", Encode.bool (version.Prefix = currentPrefix)
                        ]
            ]
        |> Encode.toString 0

    /// <example>
    /// <code lang="fsharp">
    /// NavbarEnd = [ NavbarDynamicWidget(Versions.switcher options) ]
    /// </code>
    /// </example>
    /// <summary>The control that moves a reader between versions.</summary>
    /// <param name="options">The versions declared, and where the manifest is written.</param>
    /// <param name="site">The site being rendered, for the URLs the switcher points at.</param>
    let switcher (options: VersionsOptions) (site: SiteInfo) =
        let current = site.Url.VersionPrefix |> Option.defaultValue ""

        let baseUrl =
            "/" + site.Url.BaseUrl.Trim('/') + "/" |> fun path -> path.Replace("//", "/")

        $"""<nacara-version-switcher data-base="%s{escapeAttribute baseUrl}"
                                     data-versions="%s{escapeAttribute (manifest current options)}"></nacara-version-switcher>"""

    type private VersionsPlugin(options: VersionsOptions) =
        interface IPlugin with
            member _.Name = "versions"

            member _.Configure registry =
                registry
                |> Registry.asset (
                    WriteText(switcherCss.Value, RelativePath.create "assets/versions.css")
                )
                |> Registry.asset (
                    WriteText(switcherJs.Value, RelativePath.create "assets/versions.js")
                )
                |> Registry.extra (Stylesheet "assets/versions.css")
                |> Registry.extra (Script("assets/versions.js", true))
                |> Registry.onBuildComplete (fun context ->
                    let current = context.Site.Url.VersionPrefix |> Option.defaultValue ""
                    context.Write options.ManifestPath (manifest current options) |> ignore
                )

    /// <summary>Set <c>Versions</c>.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let versions value (options: VersionsOptions) =
        { options with
            Versions = value
        }

    /// <summary>Where the manifest is written, relative to the output directory.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let manifestPath value (options: VersionsOptions) =
        { options with
            ManifestPath = value
        }

    /// <summary>Show a notice at the top of every page of an older version.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let showOutdatedNotice value (options: VersionsOptions) =
        { options with
            ShowOutdatedNotice = value
        }

    /// <summary>Version switching, with the default options.</summary>
    /// <param name="versions">Every version of the site, newest first. The one marked latest is
    /// where a reader arriving without a version lands.</param>
    let create (versions: SiteVersion list) =
        VersionsPlugin(
            { defaults with
                Versions = versions
            }
        )
        :> IPlugin

    /// <summary>Version switching, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use. Anything it leaves alone keeps its default.</param>
    let createWith (configure: VersionsOptions -> VersionsOptions) =
        VersionsPlugin(configure defaults) :> IPlugin

    /// <summary>Add version switching to a site.</summary>
    /// <param name="versions">Every version of the site, newest first.</param>
    /// <param name="site">The site being described.</param>
    let register (versions: SiteVersion list) (site: Site) = Site.plugin (create versions) site

    /// <summary>Add version switching to a site, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use. Anything it leaves alone keeps its default.</param>
    /// <param name="site">The site being described.</param>
    let registerWith (configure: VersionsOptions -> VersionsOptions) (site: Site) =
        Site.plugin (createWith configure) site
