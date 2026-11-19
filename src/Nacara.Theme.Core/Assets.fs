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

/// <summary>A part of a theme's stylesheet, written into the cascade layer
/// <c>nacara.&lt;Name&gt;</c>.</summary>
type StyleLayer =
    {
        Name: string
        Css: string
    }

/// <summary>The stylesheets every theme builds on, and the cascade layers they are written
/// into.</summary>
[<RequireQualifiedAccess>]
module Styles =

    let private readResource = Resource.text (Assembly.GetExecutingAssembly())

    /// <summary>The layer every plugin writes its stylesheet into, after the layers of the
    /// theme.</summary>
    [<Literal>]
    let PluginsLayer = "nacara.plugins"

    /// <summary>A layer of a theme's stylesheet.</summary>
    /// <param name="name">Lowercase letters, digits and hyphens, starting with a letter. The
    /// layer is <c>nacara.&lt;name&gt;</c>. <c>plugins</c> is taken by
    /// <see cref="F:Nacara.Theme.Core.Styles.PluginsLayer" />.</param>
    /// <param name="css">The rules of the layer.</param>
    let layer (name: string) (css: string) =
        if
            not (System.Text.RegularExpressions.Regex.IsMatch(name, "^[a-z][a-z0-9-]*$"))
            || name = "plugins"
        then
            invalidArg
                (nameof name)
                $"'%s{name}' is not a layer name. Use lowercase letters, digits and hyphens, and not 'plugins'"

        {
            Name = name
            Css = css
        }

    /// <summary>The default tokens, in the <c>nacara.tokens</c> layer: colours, spacing, fonts,
    /// radii, light and dark.</summary>
    let tokens = lazy (layer "tokens" (readResource "css.tokens.css"))

    /// <summary>What plugins emit, in the <c>nacara.content</c> layer, styled against the tokens:
    /// callouts, tabs, steps, file trees, code frames, heading anchors.</summary>
    let content = lazy (layer "content" (readResource "css.content.css"))

    /// <summary>The <c>@layer</c> statement that orders the layers, with
    /// <see cref="F:Nacara.Theme.Core.Styles.PluginsLayer" /> last.</summary>
    /// <param name="layers">The layers, first one lowest.</param>
    let order (layers: StyleLayer list) =
        let names =
            [
                for layer in layers do
                    $"nacara.%s{layer.Name}"

                PluginsLayer
            ]

        $"""@layer %s{String.concat ", " names};"""

    /// <summary>The rules of a layer, inside their <c>@layer</c> block.</summary>
    /// <param name="layer">The layer to write.</param>
    let wrap (layer: StyleLayer) =
        $"@layer nacara.%s{layer.Name} {{\n%s{layer.Css}\n}}\n"

    /// <summary>A theme's stylesheet, as a <c>Bundle</c> asset, and the path it is served
    /// from.</summary>
    /// <remarks>Each layer is a file of the bundle. The entry orders the layers and imports
    /// them.</remarks>
    /// <param name="name">What the bundle is called, without its extension.</param>
    /// <param name="layers">The layers, first one lowest.</param>
    let bundle (name: string) (layers: StyleLayer list) =
        let duplicates =
            layers
            |> List.countBy _.Name
            |> List.filter (fun (_, count) -> count > 1)
            |> List.map fst

        if not duplicates.IsEmpty then
            invalidArg
                (nameof layers)
                $"""These layers are given more than once: %s{String.concat ", " duplicates}"""

        let files =
            [
                for layer in layers do
                    $"nacara-%s{layer.Name}.css", wrap layer
            ]

        let entryName = $"%s{name}.css"

        let entry =
            [
                order layers

                for file, _ in files do
                    $"@import \"%s{file}\";"
            ]
            |> String.concat "\n"

        let parts = (entryName, entry) :: files

        let hash =
            parts
            |> List.map snd
            |> String.concat "\n"
            |> BuildCache.Hash
            |> fun value -> value.Substring(0, 8).ToLowerInvariant()

        let path = $"assets/css/%s{name}.%s{hash}.css"

        Bundle(parts, entryName, RelativePath.create path), path

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
