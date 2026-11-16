namespace Nacara.Theme.Docs

open Feliz.ViewEngine
open Nacara.Core
open Nacara.Theme
open Nacara.Theme.Core

/// <summary>The page a reader gets when a url matches nothing.</summary>
[<RequireQualifiedAccess>]
module NotFound =

    /// <summary>The page, as the theme's layout receives it.</summary>
    /// <param name="origin">The theme, which is what the page says generated it.</param>
    /// <param name="locale">The locale the page is written for.</param>
    /// <param name="context">The built site.</param>
    let page (origin: string) (locale: Locale) (context: HookContext) =
        let route = Route.file locale "404.html"

        let frontMatter =
            {
                Title = "Page not found"
                Description = Some "That page does not exist"
                Order = None
                Layout = Some "bare"
                PageNav = None
                MenuFilter = None
                MenuMemory = None
                Toc = None
                Main = []
            }

        let body =
            [
                "<p>"
                "The page you asked for is not here. It may have been renamed, or the link "
                "that brought you here may be out of date."
                "</p>"
                "<p>"
                $"""<a href="%s{context.Site.UrlOf(Route.home locale)}">Back to the start of the site</a>"""
                "</p>"
            ]
            |> String.concat ""

        let page =
            {
                Id = $"%s{origin}:404:%s{locale.Code}"
                Collection = origin
                Source = Generated origin
                ProjectPath = None
                Format = ".html"
                Locale = locale
                Route = route
                BodyLine = 1
                Title = frontMatter.Title
                Order = 0
                Body = body
                Html = body
                Headings = []
                FrontMatter = box frontMatter
                Dependencies = []
                Data = Map.empty
            }

        {
            Page = page
            FrontMatter = frontMatter
            Site = context.Site
            Pages = context.Pages
            Content = body
        }

    /// <summary>Writes a 404 page per locale, unless the site wrote its own.</summary>
    /// <remarks>Register with <see cref="M:Nacara.Core.Registry.onBuildComplete" />.</remarks>
    /// <param name="origin">The theme, which is what the page says generated it.</param>
    /// <param name="render">The theme's layout for its own front matter.</param>
    /// <param name="context">The built site.</param>
    let hook
        (origin: string)
        (render: PageContext<DocFrontMatter> -> ReactElement)
        (context: HookContext)
        =
        let occupied =
            context.Pages
            |> List.map (fun page -> RelativePath.value (Url.outputPath page.Route))
            |> Set.ofList

        for locale in context.Site.Locales do
            let path = RelativePath.value (Url.outputPath (Route.file locale "404.html"))

            if not (occupied.Contains path) then
                let html = page origin locale context |> render |> Render.htmlDocument
                context.Write path html |> ignore
