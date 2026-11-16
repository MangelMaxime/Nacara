namespace Nacara.Theme.Core

open Nacara.Core

[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module PageScope =

    /// <summary>The page a layout is rendering, and the site around it.</summary>
    /// <param name="context">What the layout was given.</param>
    let ofContext (context: PageContext<'FrontMatter>) =
        {
            Site = context.Site
            Pages = context.Pages
            Page = context.Page
        }

/// <summary>Finding pages, and placing them.</summary>
[<RequireQualifiedAccess>]
module Pages =

    /// <summary>The first segment of a route, which is what a theme calls a section.</summary>
    /// <param name="page">The page to place. Its section decides which navbar entry lights up and
    /// which sidebar it is shown.</param>
    let section (page: Page) =
        page.Route.Segments |> List.tryHead |> Option.defaultValue ""

    /// <summary>The page a path names, when exactly one answers to it.</summary>
    /// <remarks>The same as <see cref="M:Nacara.Core.PageIndex.find" />.</remarks>
    /// <param name="pages">Every page of the site.</param>
    /// <param name="path">A source path relative to the content directory, with or without its
    /// extension, or a page id.</param>
    let find (pages: Page list) (path: string) = PageIndex.find pages path

    /// <summary>Pages of the current section, in front-matter order.</summary>
    /// <param name="scope">The page being rendered, and the site around it.</param>
    let ofSection (scope: PageScope) =
        let own = section scope.Page

        scope.Pages
        |> List.filter (fun page ->
            page.Collection = scope.Page.Collection
            && page.Locale.Code = scope.Page.Locale.Code
            && section page = own
        )
        |> List.sortBy (fun page -> page.Order, page.Title)

/// <summary>Urls as a site writes them in its options.</summary>
[<RequireQualifiedAccess>]
module Links =

    /// <summary>The url an option names: a page's source path, or a path inside the site.</summary>
    /// <remarks>Anything else is left as written.</remarks>
    /// <param name="site">Where the site is served from.</param>
    /// <param name="pages">Every page of the site, which is where a source path is looked up.</param>
    /// <param name="url">What the site wrote.</param>
    let resolve (site: SiteInfo) (pages: Page list) (url: string) = PageIndex.link site pages url

[<RequireQualifiedAccess>]
module Translations =

    /// <summary>Translations of a page, one per locale of the site.</summary>
    /// <remarks>
    /// Pages are paired by their locale-independent route key, so a translation is found even
    /// though its file lives in another directory - and a locale with no translation of this page
    /// still appears, pointing at its home page rather than at a 404.
    /// </remarks>
    /// <param name="site">The site's locales and where it is served from.</param>
    /// <param name="pages">Every page of the build, which is where a translation is looked
    /// for.</param>
    /// <param name="page">The page being rendered.</param>
    let resolve (site: SiteInfo) (pages: Page list) (page: Page) : TranslationModel list =
        let key = Route.translationKey page.Route

        site.Locales
        |> List.map (fun locale ->
            let translation =
                pages
                |> List.tryFind (fun candidate ->
                    candidate.Locale.Code = locale.Code
                    && candidate.Collection = page.Collection
                    && Route.translationKey candidate.Route = key
                )

            let route =
                match translation with
                | Some translation -> translation.Route
                | None -> Route.home locale

            {
                Locale = locale
                Url = site.UrlOf route
                Translated = translation.IsSome
                Current = locale.Code = page.Locale.Code
            }
        )
