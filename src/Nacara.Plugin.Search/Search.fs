namespace Nacara.Plugins

open System.Diagnostics
open System.IO
open System.Reflection
open Nacara.Core
open Nacara.Plugins.Internal

/// <summary>Options of the search plugin.</summary>
type SearchOptions =
    {
        /// Path of an existing pagefind binary. Downloaded and cached when not set.
        BinaryPath: string option
        /// CSS selector of the element pagefind should index as the page body.
        RootSelector: string
    }

[<RequireQualifiedAccess>]
module Search =

    let defaults =
        {
            BinaryPath = None
            RootSelector = "main"
        }

    let private readResource = Resource.text (Assembly.GetExecutingAssembly())

    let private escapeAttribute (value: string) =
        value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;")

    /// One element per line. Joined with nothing between them, so the markup is unchanged.
    let private searchIcon =
        [
            """<svg xmlns="http://www.w3.org/2000/svg"
                     viewBox="0 0 24 24"
                     fill="none"
                     stroke="currentColor"
                     stroke-width="2"
                     stroke-linecap="round"
                     aria-hidden="true">"""
            """<circle cx="11" cy="11" r="7"/>"""
            """<path d="m20 20-3.5-3.5"/>"""
            "</svg>"
        ]
        |> String.concat ""

    /// <summary>
    /// Markup for the search box, to place in the navbar.
    /// </summary>
    /// <remarks>Styled with the theme's tokens, so it belongs in the navbar as it is.</remarks>
    /// <example>
    /// <code lang="fsharp">
    /// NavbarEnd = [ NavbarDynamicWidget Search.trigger ]
    /// </code>
    /// </example>
    /// <param name="site">Where the site is served from, so the widget finds the index belonging
    /// to this build.</param>
    let trigger (site: SiteInfo) =
        // A trailing slash, because the script appends file names to this.
        let bundle = site.UrlOfAsset("pagefind").TrimEnd '/' + "/"

        [
            $"""<button class="nacara-search__trigger"
                        type="button"
                        data-nacara-search
                        data-bundle="%s{escapeAttribute bundle}"
                        aria-label="Search">"""
            searchIcon
            "<span>Search</span>"
            """<span class="nacara-search__kbd" data-nacara-search-shortcut>Ctrl K</span>"""
            "</button>"
        ]
        |> String.concat ""

    let private index (options: SearchOptions) (context: HookContext) =
        if not context.Writes then
            // pagefind reads the built site, and under `check` there is no built site.
            Log.debug "Skipping the search index: this build writes nothing"
        else

            let binary =
                match options.BinaryPath with
                | Some path -> Ok path
                | None -> Pagefind.resolve ()

            match binary with
            | Error message ->
                context.Diagnostics.Add(
                    Diagnostic.error "pagefind-missing" $"Search is not indexed: %s{message}"
                    |> Diagnostic.withHint
                        "Set BinaryPath to use your own pagefind, or drop the plugin if this site does not need search"
                )
            | Ok binary ->
                let arguments =
                    $"--site \"%s{AbsolutePath.value context.OutputDirectory}\" --root-selector \"%s{options.RootSelector}\""

                let startInfo =
                    ProcessStartInfo(
                        binary,
                        arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    )

                use pagefind = Process.Start startInfo
                let output = pagefind.StandardOutput.ReadToEnd()
                let error = pagefind.StandardError.ReadToEnd()
                pagefind.WaitForExit()

                if pagefind.ExitCode = 0 then
                    Log.debug (output.Trim())
                    Log.success "Search index built"
                else
                    context.Diagnostics.Add(
                        Diagnostic.error
                            "pagefind-failed"
                            $"pagefind failed: %s{(error + output).Trim()}"
                        |> Diagnostic.withHint
                            "The pages are written; only the index is missing. Run the build again once pagefind can run."
                    )

    let private searchCss = lazy readResource "search.css"
    let private searchJs = lazy readResource "search.js"

    type private SearchPlugin(options: SearchOptions) =
        interface IPlugin with
            member _.Name = "search"

            member _.Configure registry =
                registry
                |> Registry.asset (
                    WriteText(searchCss.Value, RelativePath.create "assets/search.css")
                )
                |> Registry.asset (
                    WriteText(searchJs.Value, RelativePath.create "assets/search.js")
                )
                |> Registry.extra (Stylesheet "assets/search.css")
                |> Registry.extra (Script("assets/search.js", true))
                // Written by the hook below, so the build's own bookkeeping would prune it.
                |> Registry.preserve "pagefind"
                |> Registry.onBuildComplete (index options)

    /// <summary>Path of an existing pagefind binary.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let binaryPath value (options: SearchOptions) =
        { options with
            BinaryPath = value
        }

    /// <summary>CSS selector of the element pagefind should index as the page body.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let rootSelector value (options: SearchOptions) =
        { options with
            RootSelector = value
        }

    /// <summary>Search powered by pagefind, with its default options.</summary>
    let pagefind () = SearchPlugin(defaults) :> IPlugin

    /// <summary>Search powered by pagefind, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use. Anything it leaves alone
    /// keeps its default.</param>
    let pagefindWith (configure: SearchOptions -> SearchOptions) =
        SearchPlugin(configure defaults) :> IPlugin

    /// <summary>Add search to a site.</summary>
    let register (site: Site) = Site.plugin (pagefind ()) site

    /// <summary>Add search to a site, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use. Anything it leaves alone keeps its default.</param>
    /// <param name="site">The site being described.</param>
    let registerWith (configure: SearchOptions -> SearchOptions) (site: Site) =
        Site.plugin (pagefindWith configure) site
