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

    /// <summary>Register what every theme needs, documentation or not: the code-block renderer and
    /// the behaviours script.</summary>
    /// <param name="registry">What the theme is contributing to.</param>
    let configureBase (registry: Registry) =
        registry
        |> Registry.extra (NacaraCodeBlockRenderer() :> ICodeBlockRenderer)
        |> Registry.asset (Behaviours.asset ())

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
