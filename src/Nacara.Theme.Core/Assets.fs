namespace Nacara.Theme.Core

open System.Reflection
open Feliz.ViewEngine
open Nacara.Core

[<RequireQualifiedAccess>]
module Assets =

    /// <summary>Where an asset is served from, its name carrying a hash of its content.</summary>
    /// <remarks>A deployed site can then cache it forever and still pick up a change
    /// immediately.</remarks>
    /// <param name="name">The asset's name, without its extension.</param>
    /// <param name="extension">Its extension, dot included.</param>
    /// <param name="content">What it holds.</param>
    let fingerprint (name: string) (extension: string) (content: string) =
        let hash = BuildCache.Hash(content).Substring(0, 8).ToLowerInvariant()
        $"assets/%s{name}.%s{hash}%s{extension}"

/// <summary>The stylesheets every theme builds on.</summary>
[<RequireQualifiedAccess>]
module Styles =

    let private readResource = Resource.text (Assembly.GetExecutingAssembly())

    /// <summary>The default tokens: colours, spacing, fonts, radii, light and dark.</summary>
    /// <remarks>A theme includes them and overrides what it wants, or brings tokens of its own
    /// that define the same names.</remarks>
    let tokens = lazy ("nacara-core-tokens.css", readResource "css.tokens.css")

    /// <summary>What plugins emit, styled against the tokens: callouts, tabs, steps, file trees,
    /// code frames, heading anchors.</summary>
    let content = lazy ("nacara-core-content.css", readResource "css.content.css")

    /// <summary>Both shared stylesheets, named as a theme's entry imports them.</summary>
    let parts () =
        [
            tokens.Value
            content.Value
        ]

    /// <summary>A theme's stylesheet: the shared parts and its own, and where the bundle is
    /// served from.</summary>
    /// <remarks>Register the result as <c>Bundle(parts, entry, path)</c>. The entry imports the
    /// parts by name, and the site's bundler resolves them.</remarks>
    /// <param name="name">What the bundle is called, without its extension.</param>
    /// <param name="own">The theme's own stylesheets, each named as the entry imports it.</param>
    let bundle (name: string) (own: (string * string) list) =
        let parts = parts () @ own

        let hash =
            parts
            |> List.map snd
            |> String.concat "\n"
            |> BuildCache.Hash
            |> fun value -> value.Substring(0, 8).ToLowerInvariant()

        parts, $"assets/css/%s{name}.%s{hash}.css"

/// <summary>The script every theme shares, and what it attaches to.</summary>
/// <remarks>
/// It reads nothing but <c>data-nacara-*</c> attributes, so a theme opts into a behaviour by
/// writing the attribute and out of it by leaving it off. The colour scheme, dropdowns, the
/// drawer, the widgets shelf, the table of contents, code copying and tabs.
/// </remarks>
[<RequireQualifiedAccess>]
module Behaviours =

    let private readResource = Resource.text (Assembly.GetExecutingAssembly())

    /// <summary>Settles the colour scheme before first paint, so nobody sees the wrong one flash.</summary>
    /// <remarks>Inline it in the head with <see cref="M:Nacara.Theme.Core.Behaviours.bootstrapTag" />.</remarks>
    let bootstrap = lazy readResource "theme-bootstrap.js"

    /// <summary>The script, and where it is served from.</summary>
    let script =
        lazy
            (let content = readResource "nacara-behaviours.js"
             Assets.fingerprint "nacara-behaviours" ".js" content, content)

    /// <summary>The script as an asset, which <see cref="M:Nacara.Theme.Core.ThemeCore.configureBase" />
    /// registers.</summary>
    let asset () =
        let path, content = script.Value
        WriteText(content, RelativePath.create path)

    /// <summary>The inline script for the head.</summary>
    let bootstrapTag () =
        Html.script [ prop.dangerouslySetInnerHTML bootstrap.Value ]

    /// <summary>The deferred script tag, for the end of the body.</summary>
    /// <param name="site">Where the site is served from.</param>
    let scriptTag (site: SiteInfo) =
        Html.script
            [
                prop.src (site.UrlOfAsset(fst script.Value))
                prop.custom ("defer", "defer")
            ]
