namespace Nacara.Theme.Core

open Feliz.ViewEngine
open Nacara.Core
open Nacara.Theme

/// <summary>Options of <see cref="M:Nacara.Theme.Core.ThemeCore.registerWith" />.</summary>
type SiteOptions =
    {
        /// <summary>Register <see cref="P:Nacara.Theme.Core.Styles.tokens" /> before the site's
        /// stylesheet. Turn it off when the site defines every <c>--nacara-*</c> token itself.</summary>
        DefaultTokens: bool
    }

[<RequireQualifiedAccess>]
module ThemeCore =

    let private socialImage =
        System.Text.RegularExpressions.Regex("<meta property=\"og:image\" content=\"([^\"]*)\"")

    let private checkSocialImages (context: HookContext) =
        let images =
            context.Rendered
            |> List.choose (fun (page, html) ->
                let found = socialImage.Match html

                if found.Success then
                    Some(page, System.Net.WebUtility.HtmlDecode found.Groups[1].Value)
                else
                    None
            )

        let relative = images |> List.filter (fun (_, url) -> url.StartsWith "/")

        if not relative.IsEmpty then
            context.Diagnostics.Add(
                Diagnostic.warning
                    "image-needs-origin"
                    $"%i{relative.Length} pages have a social image, but the site has no origin, so its url is not absolute"
                |> Diagnostic.withHint "Set it: |> Site.origin \"https://example.com\""
            )

        if context.Writes then
            let baseUrl = context.Site.Url.BaseUrl.TrimEnd '/' + "/"
            let own = (context.Site.Origin |> Option.defaultValue "") + baseUrl

            for url, pages in
                images
                |> List.groupBy snd
                |> List.map (fun (url, pages) -> url, List.map fst pages) do
                if url.StartsWith own || url.StartsWith baseUrl then
                    let inSite =
                        if url.StartsWith own then
                            url.Substring own.Length
                        else
                            url.Substring baseUrl.Length

                    let file =
                        AbsolutePath.combine
                            context.OutputDirectory
                            [ (System.Uri.UnescapeDataString inSite).TrimStart '/' ]

                    if not (System.IO.File.Exists(AbsolutePath.value file)) then
                        context.Diagnostics.Add(
                            Diagnostic.warning
                                "image-missing"
                                $"The social image '%s{url}' is not in the output"
                            |> Diagnostic.withHint
                                $"""Put it in the static directory, or change the path. It is used by %i{pages.Length} pages, such as '%s{(List.head pages).Id}'"""
                        )

    /// <summary>Register what every theme needs, documentation or not: the code-block renderer,
    /// the behaviours script, and the check that a page's social image exists.</summary>
    /// <param name="registry">What the theme is contributing to.</param>
    let configureBase (registry: Registry) =
        registry
        |> Registry.extra (NacaraCodeBlockRenderer() :> ICodeBlockRenderer)
        |> Registry.asset (Behaviours.asset ())
        |> Registry.onBuildComplete checkSocialImages

    let private standalone (layer: StyleLayer) (registry: Registry) =
        let css =
            Styles.order
                [
                    Styles.tokens.Value
                    Styles.content.Value
                ]
            + "\n"
            + Styles.wrap layer

        let path = Assets.fingerprint $"nacara-core-%s{layer.Name}" ".css" css

        registry
        |> Registry.asset (WriteText(css, RelativePath.create path))
        |> Registry.extra (Stylesheet path)

    /// <summary>Register the shared content stylesheet on its own, in the <c>nacara.content</c>
    /// layer, linked from every page by
    /// <see cref="M:Nacara.Theme.Core.Shell.stylesheetLinks" />.</summary>
    /// <remarks>It reads the <c>--nacara-*</c> tokens, which the site defines or takes from
    /// <see cref="M:Nacara.Theme.Core.ThemeCore.tokensStylesheet" />.</remarks>
    /// <param name="registry">What the theme is contributing to.</param>
    let contentStylesheet (registry: Registry) =
        standalone Styles.content.Value registry

    /// <summary>Register the default tokens on their own, in the <c>nacara.tokens</c> layer,
    /// linked from every page before the site's stylesheet.</summary>
    /// <remarks>They set the tokens on <c>:root</c>, <c>:root[data-theme="light"]</c> and
    /// <c>:root[data-theme="dark"]</c>. A site overriding one writes it under the same
    /// selector.</remarks>
    /// <param name="registry">What the theme is contributing to.</param>
    let tokensStylesheet (registry: Registry) = standalone Styles.tokens.Value registry

    let defaults =
        {
            DefaultTokens = true
        }

    let defaultTokens value (options: SiteOptions) =
        { options with
            DefaultTokens = value
        }

    type private SitePlugin(options: SiteOptions) =
        interface IPlugin with
            member _.Name = "theme-core"

            member _.Configure registry =
                registry
                |> configureBase
                |> fun registry ->
                    if options.DefaultTokens then
                        tokensStylesheet registry
                    else
                        registry
                |> contentStylesheet

    /// <summary>A plugin for a site that writes its own layouts and stylesheet.</summary>
    /// <remarks>It registers what <see cref="M:Nacara.Theme.Core.ThemeCore.configureBase" /> does,
    /// the default tokens and the content stylesheet.</remarks>
    let plugin () = SitePlugin(defaults) :> IPlugin

    let pluginWith (configure: SiteOptions -> SiteOptions) =
        SitePlugin(configure defaults) :> IPlugin

    /// <summary>Add <see cref="M:Nacara.Theme.Core.ThemeCore.plugin" /> to a site.</summary>
    /// <param name="site">The site being described.</param>
    let register (site: Site) = Site.plugin (plugin ()) site

    let registerWith (configure: SiteOptions -> SiteOptions) (site: Site) =
        Site.plugin (pluginWith configure) site
