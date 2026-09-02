namespace Nacara.Plugins

open System
open System.IO
open System.Collections.Concurrent
open System.Net.Http
open System.Text.RegularExpressions
open System.Text.Json
open Nacara.Core

/// <summary>What a link check found.</summary>
type LinkOutcome =
    | Reachable
    | Missing of reason: string

/// <summary>Options of the links plugin.</summary>
type LinkValidatorOptions =
    {
        /// <summary>Check links that leave the site.</summary>
        /// <remarks>
        /// On by default. Answers are cached for a week, so most builds send no requests. Turn
        /// it off for a build that has no network, and see <c>FailOnExternal</c> for what a
        /// failure should cost.
        /// </remarks>
        CheckExternal: bool
        /// Check while watching too. Off by default: it is slow and rarely what you are working on.
        CheckWhileWatching: bool
        /// A link that leaves the site and cannot be reached fails the build, rather than warning.
        FailOnExternal: bool
        /// Seconds to wait for a server before giving up on a link.
        Timeout: int
        /// How many external links to check at once.
        Concurrency: int
        /// Links to leave alone, as regular expressions matched against the whole url.
        Ignore: string list
        /// Status codes to accept beyond the usual 2xx and 3xx - 403 and 429 are common for bots.
        AllowStatusCodes: int list
        /// How long an answer stays good, in hours. The cache lives under ~/.cache/nacara.
        CacheHours: int
    }

/// <summary>
/// Checks the links of the site that was built.
/// </summary>
/// <remarks>
/// <para>The markdown plugin resolves the links an author writes. This checks the site as
/// published: every <c>href</c> and <c>src</c> in every rendered page, wherever it came from,
/// against the files actually written.</para>
/// <para>Which catches an asset a theme names but never ships, a link written in html that no
/// markdown pass saw, and an anchor that moved when a heading was reworded.</para>
/// </remarks>
[<RequireQualifiedAccess>]
module LinkValidator =

    let defaults =
        {
            CheckExternal = true
            CheckWhileWatching = false
            FailOnExternal = false
            Timeout = 10
            Concurrency = 8
            Ignore = []
            AllowStatusCodes = []
            CacheHours = 168
        }

    /// Every href and src of a rendered page, in source order.
    let private attribute =
        Regex("(?:href|src)\\s*=\\s*\"([^\"]*)\"", RegexOptions.Compiled)

    /// Ids something on the page can be linked to.
    let private identifier = Regex("\\bid\\s*=\\s*\"([^\"]*)\"", RegexOptions.Compiled)

    let internal linksOf (html: string) =
        attribute.Matches html
        |> Seq.map (fun matched -> matched.Groups[1].Value)
        |> Seq.filter (fun url -> url <> "")
        |> Seq.distinct
        |> List.ofSeq

    let internal anchorsOf (html: string) =
        identifier.Matches html
        |> Seq.map (fun matched -> matched.Groups[1].Value)
        |> Set.ofSeq

    /// <summary>Links that are not ours to check: other protocols, and the page itself.</summary>
    let internal isCheckable (url: string) =
        let url = url.Trim()

        not (
            url = ""
            || url.StartsWith "#"
            || url.StartsWith "data:"
            || url.StartsWith "mailto:"
            || url.StartsWith "tel:"
            || url.StartsWith "javascript:"
            || url.StartsWith "//"
        )

    let internal isExternal (url: string) =
        url.StartsWith "http://" || url.StartsWith "https://"

    /// <summary>The prefix every url of this build carries: base url, then version.</summary>
    let internal sitePrefix (siteUrl: SiteUrl) =
        [
            yield! siteUrl.BaseUrl.Split('/', StringSplitOptions.RemoveEmptyEntries)
            yield! Option.toList siteUrl.VersionPrefix
        ]
        |> String.concat "/"
        |> fun prefix ->
            if prefix = "" then
                "/"
            else
                "/" + prefix + "/"

    /// <summary>
    /// The file a url points at, relative to the output directory.
    /// </summary>
    /// <remarks>
    /// A url ending in a slash is a directory, and a static host serves <c>index.html</c> from it -
    /// which is the file the build wrote. Anything else is the file itself.
    /// </remarks>
    let internal fileOf (prefix: string) (path: string) =
        let path = (path.Split('?')[0]).Split('#')[0]

        if not (path.StartsWith prefix) then
            None
        else
            let relative = path.Substring(prefix.Length).Trim('/')

            if relative = "" then
                Some "index.html"
            elif path.EndsWith "/" || not ((Path.GetFileName relative).Contains ".") then
                Some(relative + "/index.html")
            else
                Some relative

    type private Cache(directory: string, hours: int) =
        let file = Path.Combine(directory, "links.json")
        let entries = ConcurrentDictionary<string, DateTimeOffset * bool>()

        do
            if File.Exists file then
                try
                    JsonSerializer.Deserialize<Collections.Generic.Dictionary<string, string>>(
                        File.ReadAllText file
                    )
                    |> Seq.iter (fun pair ->
                        match pair.Value.Split '|' with
                        | [| stamp; ok |] ->
                            match DateTimeOffset.TryParse stamp with
                            | true, stamp -> entries[pair.Key] <- (stamp, ok = "1")
                            | _ -> ()
                        | _ -> ()
                    )
                with _ ->
                    ()

        member _.TryGet(url: string) =
            match entries.TryGetValue url with
            | true, (stamp, ok) when DateTimeOffset.UtcNow - stamp < TimeSpan.FromHours(float hours) ->
                Some ok
            | _ -> None

        member _.Set(url: string, ok: bool) =
            entries[url] <- (DateTimeOffset.UtcNow, ok)

        member _.Save() =
            try
                Directory.CreateDirectory directory |> ignore

                entries
                |> Seq.map (fun pair ->
                    let stamp, ok = pair.Value

                    pair.Key,
                    $"""%s{stamp.ToString "o"}|%s{if ok then
                                                      "1"
                                                  else
                                                      "0"}"""
                )
                |> dict
                |> JsonSerializer.Serialize
                |> fun json -> File.WriteAllText(file, json)
            with _ ->
                ()

    let private cacheDirectory =
        Path.Combine(
            Environment.GetFolderPath Environment.SpecialFolder.UserProfile,
            ".cache",
            "nacara",
            "links"
        )

    /// <summary>Ask a server whether a url is there, cheaply first, then properly.</summary>
    let private reach (client: HttpClient) (allowed: int list) (url: string) =
        async {
            let attempt (method: HttpMethod) =
                async {
                    use request = new HttpRequestMessage(method, url)

                    let! response =
                        client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                        |> Async.AwaitTask

                    return int response.StatusCode
                }

            try
                let! status = attempt HttpMethod.Head

                let! status =
                    if status = 404 || status = 405 || status = 403 then
                        attempt HttpMethod.Get
                    else
                        async { return status }

                if status < 400 || List.contains status allowed then
                    return Reachable
                else
                    return Missing $"answered %i{status}"
            with
            | :? OperationCanceledException -> return Missing "timed out"
            | exn ->
                let rec root (exn: exn) =
                    if isNull exn.InnerException then
                        exn.Message
                    else
                        root exn.InnerException

                return Missing(root exn)
        }

    let private checkExternalLinks (options: LinkValidatorOptions) (urls: string list) =
        let ignored = options.Ignore |> List.map (fun pattern -> Regex pattern)

        let wanted =
            urls
            |> List.filter (fun url ->
                ignored |> List.forall (fun pattern -> not (pattern.IsMatch url))
            )

        let cache = Cache(cacheDirectory, options.CacheHours)

        use client =
            let handler = new HttpClientHandler(AllowAutoRedirect = true)

            let client =
                new HttpClient(handler, Timeout = TimeSpan.FromSeconds(float options.Timeout))

            // Servers answer a browser and refuse an unnamed client.
            client.DefaultRequestHeaders.UserAgent.ParseAdd
                "Mozilla/5.0 (compatible; Nacara link check)"

            client

        let gate = new Threading.SemaphoreSlim(max 1 options.Concurrency)

        let results =
            wanted
            |> List.map (fun url ->
                async {
                    match cache.TryGet url with
                    | Some true -> return url, Reachable
                    | Some false -> return url, Missing "unreachable when last checked"
                    | None ->
                        do! gate.WaitAsync() |> Async.AwaitTask

                        try
                            let! outcome = reach client options.AllowStatusCodes url
                            cache.Set(url, (outcome = Reachable))
                            return url, outcome
                        finally
                            gate.Release() |> ignore
                }
            )
            |> Async.Parallel
            |> Async.RunSynchronously
            |> List.ofArray

        cache.Save()
        results

    /// <summary>Where a link was found, so a report names a page rather than a url.</summary>
    type private Found =
        {
            Url: string
            Page: Page
            /// False for a link the layout put there, which is on every page and in no source file.
            InBody: bool
        }

    let private check (options: LinkValidatorOptions) (context: HookContext) =
        if context.IsWatch && not options.CheckWhileWatching then
            Log.debug "Skipping the link check while watching"
        else

            let prefix = sitePrefix context.Site.Url
            let output = AbsolutePath.value context.OutputDirectory

            // One report per url: the navbar and the footer put the same link on every page.
            let found =
                context.Rendered
                |> List.collect (fun (page, document) ->
                    let body = linksOf page.Html |> Set.ofList

                    linksOf document
                    |> List.filter isCheckable
                    |> List.map (fun url ->
                        {
                            Url = url
                            Page = page
                            InBody = body.Contains url
                        }
                    )
                )
                |> List.groupBy _.Url
                |> List.map (fun (_, occurrences) ->
                    occurrences
                    |> List.tryFind _.InBody
                    |> Option.defaultValue (List.head occurrences)
                )

            let report (link: Found) code message hint =
                let diagnostic = Diagnostic.error code message |> Diagnostic.withHint (hint: string)

                match link.Page.Source with
                | FromFile file when link.InBody ->
                    context.Diagnostics.Add(diagnostic |> Diagnostic.inFile file)
                | _ -> context.Diagnostics.Add diagnostic

            let written =
                context.Pages
                |> List.map (fun page -> RelativePath.value (Url.outputPath page.Route), page)
                |> dict

            let anchors = ConcurrentDictionary<string, Set<string>>()

            let anchorsIn (relative: string) (file: string) =
                anchors.GetOrAdd(
                    relative,
                    fun _ ->
                        match written.TryGetValue relative with
                        | true, page -> anchorsOf page.Html
                        | _ ->
                            if File.Exists file then
                                anchorsOf (File.ReadAllText file)
                            else
                                Set.empty
                )

            for link in found |> List.filter (fun link -> not (isExternal link.Url)) do
                if not (link.Url.StartsWith "/") then
                    report
                        link
                        "relative-link"
                        $"'%s{link.Url}' is relative, so where it points depends on the page it is read from"
                        "Link to the file with markdown, and the engine writes the url"
                else
                    match fileOf prefix link.Url with
                    | None ->
                        report
                            link
                            "outside-site"
                            $"'%s{link.Url}' points outside this site, which is served from '%s{prefix}'"
                            "A link within the site should start with the base url, which markdown links do"
                    | Some relative ->
                        let target =
                            Path.Combine(output, relative.Replace('/', Path.DirectorySeparatorChar))

                        // An asset is a file rather than a page, and under `check` there is none.
                        if not (written.ContainsKey relative || context.Writes) then
                            ()
                        elif not (written.ContainsKey relative || File.Exists target) then
                            report
                                link
                                "target-missing"
                                $"'%s{link.Url}' points at nothing this build wrote"
                                $"The build would have to write '%s{relative}'"
                        else
                            let anchor = (link.Url.Split('#') |> Array.skip 1 |> Array.tryHead)

                            match anchor with
                            | Some anchor when
                                anchor <> "" && not ((anchorsIn relative target).Contains anchor)
                                ->
                                report
                                    link
                                    "anchor-missing"
                                    $"'%s{link.Url}' points at an anchor that is not on that page"
                                    "Anchors come from headings, or from an id written in the markup"
                            | _ -> ()

            if options.CheckExternal then
                let external =
                    found
                    |> List.filter (fun link -> isExternal link.Url)
                    |> List.map _.Url
                    |> List.distinct

                if not (List.isEmpty external) then
                    Log.debug $"Checking %i{List.length external} external links"

                    let pageOf (url: string) =
                        found |> List.find (fun link -> link.Url = url) |> _.Page

                    for url, outcome in checkExternalLinks options external do
                        match outcome with
                        | Reachable -> ()
                        | Missing reason ->
                            let diagnostic =
                                (if options.FailOnExternal then
                                     Diagnostic.error
                                 else
                                     Diagnostic.warning)
                                    "external-unreachable"
                                    $"'%s{url}' could not be reached: %s{reason}"
                                |> Diagnostic.withHint
                                    "Add it to Ignore if the site refuses automated requests"

                            match (pageOf url).Source with
                            | FromFile file ->
                                context.Diagnostics.Add(diagnostic |> Diagnostic.inFile file)
                            | Generated _ -> context.Diagnostics.Add diagnostic

    type private LinksPlugin(options: LinkValidatorOptions) =
        interface IPlugin with
            member _.Name = "link-validator"

            member _.Configure registry =
                registry |> Registry.onBuildComplete (check options)

    /// <summary>Check links that leave the site.</summary>
    /// <param name="value">Whether to ask the network about them.</param>
    /// <param name="options">The options so far.</param>
    let checkExternal value (options: LinkValidatorOptions) =
        { options with
            CheckExternal = value
        }

    /// <summary>Check while watching too.</summary>
    /// <param name="value">Whether a watch build checks links.</param>
    /// <param name="options">The options so far.</param>
    let checkWhileWatching value (options: LinkValidatorOptions) =
        { options with
            CheckWhileWatching = value
        }

    /// <summary>Fail the build on a link that leaves the site and cannot be reached.</summary>
    /// <param name="value">Whether an unreachable external link is an error rather than a warning.</param>
    /// <param name="options">The options so far.</param>
    let failOnExternal value (options: LinkValidatorOptions) =
        { options with
            FailOnExternal = value
        }

    /// <summary>Seconds to wait for a server before giving up on a link.</summary>
    /// <param name="value">The timeout, in seconds.</param>
    /// <param name="options">The options so far.</param>
    let timeout value (options: LinkValidatorOptions) =
        { options with
            Timeout = value
        }

    /// <summary>How many external links to check at once.</summary>
    /// <param name="value">The number of links in flight.</param>
    /// <param name="options">The options so far.</param>
    let concurrency value (options: LinkValidatorOptions) =
        { options with
            Concurrency = value
        }

    /// <summary>Links to leave alone, as regular expressions matched against the whole url.</summary>
    /// <param name="value">The patterns to skip.</param>
    /// <param name="options">The options so far.</param>
    let ignoring value (options: LinkValidatorOptions) =
        { options with
            Ignore = value
        }

    /// <summary>Status codes to accept beyond the usual 2xx and 3xx.</summary>
    /// <param name="value">The codes to treat as reachable - 403 and 429 are common for bots.</param>
    /// <param name="options">The options so far.</param>
    let allowStatusCodes value (options: LinkValidatorOptions) =
        { options with
            AllowStatusCodes = value
        }

    /// <summary>How long an answer stays good, in hours.</summary>
    /// <param name="value">The lifetime of a cached answer.</param>
    /// <param name="options">The options so far.</param>
    let cacheHours value (options: LinkValidatorOptions) =
        { options with
            CacheHours = value
        }

    /// <summary>Checking the site's links, with the default options.</summary>
    let create () = LinksPlugin(defaults) :> IPlugin

    /// <summary>Checking the site's links, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use. Anything it leaves alone keeps its default.</param>
    let createWith (configure: LinkValidatorOptions -> LinkValidatorOptions) =
        LinksPlugin(configure defaults) :> IPlugin

    /// <summary>Check every link the site publishes.</summary>
    let register (site: Site) = Site.plugin (create ()) site

    /// <summary>Check every link the site publishes, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use. Anything it leaves alone keeps its default.</param>
    /// <param name="site">The site being described.</param>
    let registerWith (configure: LinkValidatorOptions -> LinkValidatorOptions) (site: Site) =
        Site.plugin (createWith configure) site

    module O =
        let checkExternal v o =
            { o with
                CheckExternal = v
            }

        let checkWhileWatching o =
            { o with
                CheckWhileWatching = true
            }
