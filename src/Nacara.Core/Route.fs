namespace Nacara.Core

open System

/// <summary>
/// A logical location in the site: a locale plus path segments.
/// </summary>
/// <remarks>
/// A route deliberately carries neither the base URL nor the version prefix. Those belong to the
/// build rather than to your content, so you can deploy the same content at <c>/</c>, under
/// <c>/Nacara/</c> or under <c>/Nacara/2.0/</c> without rewriting a single page. Turn a route into
/// a URL with <see cref="T:Nacara.Core.Url" />.
/// </remarks>
type Route =
    {
        Locale: Locale
        Segments: string list
    }

    override this.ToString() =
        "/" + String.Join("/", Locale.segments this.Locale @ this.Segments)

[<RequireQualifiedAccess>]
module Route =

    /// <summary>Create a route from already-slugified segments.</summary>
    /// <param name="locale">Which language this page is in. The root locale writes no prefix, the
    /// others write their code as the first segment.</param>
    /// <param name="segments">The path, one entry per segment, each one already a slug. Use
    /// <see cref="M:Nacara.Core.Route.ofPath"/> when they are not.</param>
    let create (locale: Locale) (segments: string list) =
        {
            Locale = locale
            Segments = segments |> List.filter (String.IsNullOrWhiteSpace >> not)
        }

    /// <summary>
    /// Create a route from a path, slugifying every segment.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// Route.ofPath locale "docs/Getting started" // /docs/getting-started
    /// </code>
    /// </example>
    /// <param name="locale">Which language this page is in.</param>
    /// <param name="path">A path with slashes. Every segment is slugged, and empty ones are
    /// dropped, so <c>"docs//Getting started/"</c> and <c>"docs/getting-started"</c> agree.</param>
    let ofPath (locale: Locale) (path: string) =
        path.Split('/', StringSplitOptions.RemoveEmptyEntries)
        |> Seq.map Slug.create
        |> Seq.filter (String.IsNullOrWhiteSpace >> not)
        |> List.ofSeq
        |> create locale

    /// <summary>
    /// A route that is a file rather than a directory, such as <c>404.html</c>.
    /// </summary>
    /// <remarks>
    /// Pages are written as <c>somewhere/index.html</c> so their URLs end in a slash, but some files
    /// have to land at a literal path: every static host looks for <c>404.html</c> at the root of
    /// the output, and will not use <c>404/index.html</c> instead.
    /// </remarks>
    /// <param name="locale">Which language this file belongs to.</param>
    /// <param name="path">The literal path to write, extension and all: <c>404.html</c>.</param>
    let file (locale: Locale) (path: string) =
        create locale (path.Split('/', StringSplitOptions.RemoveEmptyEntries) |> List.ofArray)

    /// <summary>True when this route names a file, rather than a page in a directory.</summary>
    /// <param name="route">The route to ask about.</param>
    let isFile (route: Route) =
        route.Segments
        |> List.tryLast
        |> Option.map (fun segment -> segment.Contains "." && not (segment.EndsWith "."))
        |> Option.defaultValue false

    /// <summary>The route of the locale's home page.</summary>
    /// <param name="locale">Whose home page: the root locale's is <c>/</c>, another's is
    /// <c>/fr/</c>.</param>
    let home (locale: Locale) = create locale []

    /// <summary>True when this route is a locale's home page.</summary>
    /// <param name="route">The route to ask about.</param>
    let isHome (route: Route) = List.isEmpty route.Segments

    /// <summary>Locale-independent key, used to link translations of the same page together.</summary>
    /// <param name="route">The route to take the key of. Two pages sharing a key are the same page
    /// in two languages, and that is what the locale picker follows.</param>
    let translationKey (route: Route) = String.Join("/", route.Segments)

/// <summary>
/// Where the current build will be deployed.
/// </summary>
/// <remarks>
/// A build knows it is <c>2.0</c> and emits every URL under that prefix, so versions are
/// independent deployments side by side rather than duplicated content inside one build.
/// </remarks>
type SiteUrl =
    {
        /// Path the site is served from, for example <c>/</c> or <c>/Nacara/</c>.
        BaseUrl: string
        /// Set when this build is deployed as one version among several.
        VersionPrefix: string option
    }

[<RequireQualifiedAccess>]
module SiteUrl =

    /// <summary>Where the site is served from, as every link will be resolved against it.</summary>
    /// <param name="baseUrl">The base path, <c>/</c> or <c>/Nacara/</c>. Slashes at either end are
    /// optional on the way in, and always there on the way out.</param>
    let create (baseUrl: string) =
        {
            BaseUrl = "/" + baseUrl.Trim('/')
            VersionPrefix = None
        }

    /// <summary>Put every URL of this build under a version's prefix.</summary>
    /// <param name="prefix">What this version is called in a URL: <c>v2</c>, <c>next</c>.</param>
    /// <param name="siteUrl">Where the site is served from.</param>
    let withVersionPrefix prefix (siteUrl: SiteUrl) =
        { siteUrl with
            VersionPrefix = Some(prefix: string)
        }

[<RequireQualifiedAccess>]
module Url =

    /// <summary>Absolute, root-relative URL of a route, always ending with a <c>/</c>.</summary>
    /// <param name="siteUrl">Where the site is served from, and under which version.</param>
    /// <param name="route">The route to address. A route that names a file keeps its extension
    /// and gets no trailing slash.</param>
    let ofRoute (siteUrl: SiteUrl) (route: Route) =
        let path =
            [
                yield! siteUrl.BaseUrl.Split('/', StringSplitOptions.RemoveEmptyEntries)
                yield! Option.toList siteUrl.VersionPrefix
                yield! Locale.segments route.Locale
                yield! route.Segments
            ]
            |> String.concat "/"

        if path = "" then
            "/"
        elif Route.isFile route then
            "/" + path
        else
            "/" + path + "/"

    /// <summary>Root-relative URL of a file in the output, such as an asset.</summary>
    /// <param name="siteUrl">Where the site is served from, and under which version.</param>
    /// <param name="path">The file's path inside the output - <c>assets/api.css</c>. What a
    /// plugin registered as an asset is addressed with exactly this.</param>
    let ofPath (siteUrl: SiteUrl) (path: string) =
        [
            yield! siteUrl.BaseUrl.Split('/', StringSplitOptions.RemoveEmptyEntries)
            yield! Option.toList siteUrl.VersionPrefix
            yield! path.Split('/', StringSplitOptions.RemoveEmptyEntries)
        ]
        |> String.concat "/"
        |> fun path -> "/" + path

    /// <summary>Path of the file to write for a route, relative to the output directory.</summary>
    /// <remarks>
    /// Routes map to directory-style URLs (<c>/docs/guide/</c> -> <c>docs/guide/index.html</c>),
    /// which keeps links stable whether or not the host hides the <c>.html</c> extension.
    /// </remarks>
    /// <param name="route">The route to write. Directory-style routes become
    /// <c>segments/index.html</c>; a file route is its own path.</param>
    let outputPath (route: Route) =
        let segments = Locale.segments route.Locale @ route.Segments

        (if Route.isFile route then
             segments
         else
             segments @ [ "index.html" ])
        |> String.concat "/"
        |> RelativePath.create
