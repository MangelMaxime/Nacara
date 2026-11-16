namespace Nacara.Theme.Core

open Feliz.ViewEngine
open Nacara.Core

[<RequireQualifiedAccess>]
module Head =

    /// <summary>What the head of a page says about it, resolved against the site.</summary>
    /// <param name="title">The page's title.</param>
    /// <param name="description">The page's description, when it has one. The site's stands in
    /// otherwise.</param>
    /// <param name="favicon">The favicon's path, relative to the site root.</param>
    /// <param name="scope">The page being rendered, and the site around it.</param>
    let resolve
        (title: string)
        (description: string option)
        (favicon: string option)
        (scope: PageScope)
        : HeadModel
        =
        {
            Title = title
            WindowTitle =
                if title = scope.Site.Title then
                    title
                else
                    $"%s{title} · %s{scope.Site.Title}"
            Description = description |> Option.orElse scope.Site.Description
            Canonical = scope.Site.AbsoluteUrlOf scope.Page.Route
            SiteName = scope.Site.Title
            Locale = scope.Page.Locale.Code
            OgType = "article"
            Favicon = favicon |> Option.map scope.Site.UrlOfAsset
        }

    /// <summary>The tags every page carries: charset, viewport, title, description, canonical
    /// url, social cards, favicon.</summary>
    /// <param name="model">What the head says.</param>
    let meta (model: HeadModel) : ReactElement list =
        [
            Html.meta [ prop.charset.utf8 ]
            Html.meta
                [
                    prop.name "viewport"
                    prop.content "width=device-width, initial-scale=1"
                ]
            Html.title model.WindowTitle
            match model.Description with
            | Some description ->
                Html.meta
                    [
                        prop.name "description"
                        prop.content description
                    ]
            | None -> ()
            match model.Canonical with
            | Some url ->
                Html.link
                    [
                        prop.rel "canonical"
                        prop.href url
                    ]

                Html.meta
                    [
                        prop.custom ("property", "og:url")
                        prop.content url
                    ]
            | None -> ()
            Html.meta
                [
                    prop.custom ("property", "og:site_name")
                    prop.content model.SiteName
                ]
            Html.meta
                [
                    prop.custom ("property", "og:locale")
                    prop.content model.Locale
                ]
            Html.meta
                [
                    prop.name "twitter:card"
                    prop.content "summary_large_image"
                ]
            match model.Description with
            | Some description ->
                Html.meta
                    [
                        prop.custom ("property", "og:description")
                        prop.content description
                    ]
            | None -> ()
            Html.meta
                [
                    prop.custom ("property", "og:title")
                    prop.content model.Title
                ]
            Html.meta
                [
                    prop.custom ("property", "og:type")
                    prop.content model.OgType
                ]
            match model.Favicon with
            | Some icon ->
                Html.link
                    [
                        prop.rel "icon"
                        prop.href icon
                    ]
            | None -> ()
        ]
