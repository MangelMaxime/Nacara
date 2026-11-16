namespace Nacara.Theme.Core

open Feliz.ViewEngine
open Nacara.Core

/// <summary>The parts of a page every theme writes the same way.</summary>
[<RequireQualifiedAccess>]
module Shell =

    /// <summary>A link to every stylesheet plugins contributed, for the head.</summary>
    /// <param name="site">The site, which carries what plugins contributed.</param>
    let stylesheetLinks (site: SiteInfo) : ReactElement list =
        [
            for asset in site.PageAssets do
                match asset with
                | Stylesheet path ->
                    Html.link
                        [
                            prop.rel "stylesheet"
                            prop.href (site.UrlOfAsset path)
                        ]
                | Script _
                | InlineScript _ -> ()
        ]

    /// <summary>Every script plugins contributed, for the end of the body.</summary>
    /// <param name="site">The site, which carries what plugins contributed.</param>
    let scripts (site: SiteInfo) : ReactElement list =
        [
            for asset in site.PageAssets do
                match asset with
                | Script(path, defer) ->
                    Html.script
                        [
                            prop.src (site.UrlOfAsset path)
                            if defer then
                                prop.custom ("defer", "defer")
                        ]
                | InlineScript code -> Html.script [ prop.dangerouslySetInnerHTML code ]
                | Stylesheet _ -> ()
        ]

    /// <summary>The link a keyboard reader uses to jump past the navigation.</summary>
    /// <param name="className">What the theme styles it as.</param>
    /// <param name="target">The id of the content, <c>#</c> included.</param>
    let skipLink (className: string) (target: string) =
        Html.a
            [
                prop.className className
                prop.href target
                prop.text "Skip to content"
            ]

    /// <summary>A note that the page is standing in for a translation that does not exist yet.</summary>
    /// <remarks>Nothing, for a page read in its own locale.</remarks>
    /// <param name="page">The page being rendered.</param>
    let untranslatedNotice (page: Page) =
        match page.TryData<string> PageData.UntranslatedFrom with
        | Some source ->
            Html.aside
                [
                    prop.className "nacara-callout"
                    prop.custom ("data-kind", "note")
                    prop.custom ("data-title", "Not translated yet")
                    prop.children
                        [
                            Html.p
                                $"This page has not been translated into %s{page.Locale.Label} yet - you are reading the '%s{source}' version."
                        ]
                ]
        | None -> Html.none

    /// <summary>The attribute naming the plugin that generated a page, for one that was.</summary>
    /// <param name="page">The page being rendered.</param>
    let generatedBy (page: Page) : IReactProperty list =
        match page.Source with
        | Generated origin -> [ prop.custom ("data-nacara-generated-by", origin) ]
        | FromFile _ -> []
