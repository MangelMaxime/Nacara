namespace Nacara.Theme

open System.IO
open System.Reflection
open Feliz.ViewEngine
open Nacara.Core

/// <summary>The default theme: a documentation layout and the assets it needs.</summary>
[<RequireQualifiedAccess>]
module Theme =

    let defaults =
        {
            Navbar = []
            NavbarEnd = []
            Menus = Map.empty
            EditUrlBase = None
            HeadExtra = []
            Css = []
            Footer = None
            FavIcon = None
        }

    /// <summary>The items on the left of the navbar, after the site's title.</summary>
    /// <param name="value">The items, in the order they are shown.</param>
    /// <param name="options">The options so far.</param>
    let navbar value (options: ThemeOptions) =
        { options with
            Navbar = value
        }

    /// <summary>The items on the right of the navbar - search, a repository link, the theme toggle.</summary>
    /// <param name="value">The items, in the order they are shown.</param>
    /// <param name="options">The options so far.</param>
    let navbarEnd value (options: ThemeOptions) =
        { options with
            NavbarEnd = value
        }

    /// <summary>The menu for one section, replacing the one its pages would have produced.</summary>
    /// <remarks>
    /// Additive: call it once per section. A section you say nothing about gets a menu built from
    /// its pages, or the one a plugin offered.
    /// </remarks>
    /// <example>
    /// <code lang="fsharp">
    /// Theme.defaults
    /// |> Theme.menu "guide" [ Menu.page "guide/getting-started.md" ]
    /// |> Theme.menu "plugins" [ Menu.page "plugins/overview.md" ]
    /// </code>
    /// </example>
    /// <param name="section">The first segment of the routes it covers, such as <c>guide</c>.</param>
    /// <param name="items">What the menu holds.</param>
    /// <param name="options">The options so far.</param>
    let menu section items (options: ThemeOptions) =
        { options with
            Menus = Map.add section items options.Menus
        }

    /// <summary>Every menu at once, replacing any set before.</summary>
    /// <param name="value">The menus, keyed by section.</param>
    /// <param name="options">The options so far.</param>
    let menus value (options: ThemeOptions) =
        { options with
            Menus = value
        }

    /// <summary>Base URL for the "edit this page" link.</summary>
    /// <param name="value">Where an edit starts, such as
    /// <c>https://github.com/owner/repo/edit/main/docs</c>.</param>
    /// <param name="options">The options so far.</param>
    let editUrl value (options: ThemeOptions) =
        { options with
            EditUrlBase = Some value
        }

    /// <summary>Markup added at the end of <c>&lt;head&gt;</c> on every page.</summary>
    /// <param name="value">What to add - an analytics snippet, a font, a meta tag.</param>
    /// <param name="options">The options so far.</param>
    let headExtra value (options: ThemeOptions) =
        { options with
            HeadExtra = value
        }

    /// <summary>CSS added to every page, after the theme's own.</summary>
    /// <remarks>
    /// For a rule or two - a token to override, one section to treat differently. Anything larger
    /// belongs in a file of its own, shipped as a static asset and linked with
    /// <see cref="M:Nacara.Theme.Theme.headExtra" />.
    /// </remarks>
    /// <example>
    /// <code lang="fsharp">
    /// Theme.defaults
    /// |> Theme.css """[data-section="reference"] { --nacara-sidebar-width: 20rem; }"""
    /// </code>
    /// </example>
    /// <param name="value">The rules, as they would be written in a stylesheet.</param>
    /// <param name="options">The options so far.</param>
    let css value (options: ThemeOptions) =
        { options with
            Css = options.Css @ [ value ]
        }

    /// <summary>What every page ends with.</summary>
    /// <param name="value">The footer's markup.</param>
    /// <param name="options">The options so far.</param>
    let footer value (options: ThemeOptions) =
        { options with
            Footer = Some value
        }

    /// <summary>The site's favicon.</summary>
    /// <param name="value">Its path, relative to the site root.</param>
    /// <param name="options">The options so far.</param>
    let favIcon value (options: ThemeOptions) =
        { options with
            FavIcon = Some value
        }

    let private readResource = Resource.text (Assembly.GetExecutingAssembly())

    /// Assets carry a content hash in their name, so a deployed site can cache them forever and
    /// still pick up a change immediately.
    let private fingerprinted (name: string) (extension: string) =
        lazy
            (let content = readResource (name + extension)
             let hash = BuildCache.Hash(content).Substring(0, 8).ToLowerInvariant()
             $"assets/%s{name}.%s{hash}%s{extension}", content)

    let private entryStyleSheet = "nacara.css"

    let private styleSheetParts =
        [
            entryStyleSheet
            "tokens.css"
            "base.css"
            "navbar.css"
            "layout.css"
            "components.css"
            "code.css"
            "responsive.css"
        ]

    /// <summary>The stylesheet's parts, and where the bundle is served from.</summary>
    let private styles =
        lazy
            (let parts =
                styleSheetParts |> List.map (fun name -> name, readResource $"css.%s{name}")

             let hash =
                 parts
                 |> List.map snd
                 |> String.concat "\n"
                 |> BuildCache.Hash
                 |> fun value -> value.Substring(0, 8).ToLowerInvariant()

             parts, $"assets/css/nacara.%s{hash}.css")

    let private script = fingerprinted "nacara" ".js"

    /// <summary>Settles the colour scheme before first paint, so nobody sees the wrong one flash.</summary>
    let private themeBootstrap = lazy readResource "theme-bootstrap.js"

    let private rawHtml (markup: string) =
        Html.span [ prop.dangerouslySetInnerHTML markup ]

    /// <summary>Render a full page with the theme's chrome.</summary>
    /// <param name="options">The theme's own configuration: navbar, menus, footer.</param>
    /// <param name="doc">What the theme needs to know about this page - its title, and which
    /// parts of the chrome it wants.</param>
    /// <param name="context">The page and the site around it, whatever front-matter type the site
    /// declared.</param>
    /// <remarks>
    /// Takes a <see cref="T:Nacara.Theme.DocPage" /> rather than reading front matter itself, so a
    /// site with its own front-matter type uses the same layout by mapping onto that record.
    /// </remarks>
    let shell (options: ThemeOptions) (doc: DocPage) (context: PageContext<'FrontMatter>) =
        let _, cssPath = styles.Value
        let scriptPath, _ = script.Value

        let layoutClass =
            if not doc.Styled then
                "nacara-layout nacara-layout--bare"
            elif not doc.ShowMenu then
                "nacara-layout nacara-layout--splash"
            elif not doc.ShowToc then
                "nacara-layout nacara-layout--no-toc"
            else
                "nacara-layout"

        Html.html
            [
                prop.lang context.Page.Locale.Code
                prop.custom ("dir", context.Page.Locale.Direction.HtmlValue)
                prop.children
                    [
                        Html.head
                            [
                                Html.meta [ prop.charset.utf8 ]
                                Html.meta
                                    [
                                        prop.name "viewport"
                                        prop.content "width=device-width, initial-scale=1"
                                    ]
                                Html.title (
                                    if doc.Title = context.Site.Title then
                                        doc.Title
                                    else
                                        $"%s{doc.Title} · %s{context.Site.Title}"
                                )
                                match doc.Description |> Option.orElse context.Site.Description with
                                | Some description ->
                                    Html.meta
                                        [
                                            prop.name "description"
                                            prop.content description
                                        ]
                                | None -> Html.none
                                match context.Site.AbsoluteUrlOf context.Page.Route with
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
                                | None -> Html.none
                                Html.meta
                                    [
                                        prop.custom ("property", "og:site_name")
                                        prop.content context.Site.Title
                                    ]
                                Html.meta
                                    [
                                        prop.custom ("property", "og:locale")
                                        prop.content context.Page.Locale.Code
                                    ]
                                Html.meta
                                    [
                                        prop.name "twitter:card"
                                        prop.content "summary_large_image"
                                    ]
                                match doc.Description |> Option.orElse context.Site.Description with
                                | Some description ->
                                    Html.meta
                                        [
                                            prop.custom ("property", "og:description")
                                            prop.content description
                                        ]
                                | None -> Html.none
                                Html.meta
                                    [
                                        prop.custom ("property", "og:title")
                                        prop.content doc.Title
                                    ]
                                Html.meta
                                    [
                                        prop.custom ("property", "og:type")
                                        prop.content "article"
                                    ]
                                match options.FavIcon with
                                | Some icon ->
                                    Html.link
                                        [
                                            prop.rel "icon"
                                            prop.href (context.Site.UrlOfAsset icon)
                                        ]
                                | None -> Html.none
                                Html.link
                                    [
                                        prop.rel "stylesheet"
                                        prop.href (context.Site.UrlOfAsset cssPath)
                                    ]
                                Html.script [ prop.dangerouslySetInnerHTML themeBootstrap.Value ]
                                for asset in context.Site.PageAssets do
                                    match asset with
                                    | Stylesheet path ->
                                        Html.link
                                            [
                                                prop.rel "stylesheet"
                                                prop.href (context.Site.UrlOfAsset path)
                                            ]
                                    | Script _
                                    | InlineScript _ -> Html.none
                                yield! options.HeadExtra

                                if not options.Css.IsEmpty then
                                    Html.style
                                        [
                                            prop.dangerouslySetInnerHTML (
                                                String.concat "\n" options.Css
                                            )
                                        ]
                            ]
                        Html.body
                            [
                                prop.custom ("data-section", Components.sectionOf context.Page)
                                prop.children
                                    [
                                        Html.a
                                            [
                                                prop.className "nacara-skip-link"
                                                prop.href "#nacara-content"
                                                prop.text "Skip to content"
                                            ]
                                        Components.navbar options context
                                        Html.div
                                            [
                                                prop.className layoutClass
                                                prop.children
                                                    [
                                                        // Rendered even with no menu to show: on a
                                                        // narrow screen it is what the navbar's
                                                        // sections fold into.
                                                        Components.sidebar options doc context
                                                        Html.main
                                                            [
                                                                for name, value in
                                                                    doc.MainAttributes do
                                                                    prop.custom (name, value)
                                                                prop.id "nacara-content"
                                                                prop.tabIndex -1

                                                                if doc.Styled then
                                                                    prop.className "nacara-content"
                                                                match context.Page.Source with
                                                                | Generated origin ->
                                                                    prop.custom (
                                                                        "data-nacara-generated-by",
                                                                        origin
                                                                    )
                                                                | FromFile _ -> ()
                                                                prop.children
                                                                    [
                                                                        match
                                                                            context.Page.TryData<
                                                                                string
                                                                             >
                                                                                PageData
                                                                                    .UntranslatedFrom
                                                                        with
                                                                        | Some source ->
                                                                            Html.aside
                                                                                [
                                                                                    prop.className
                                                                                        "nacara-callout"
                                                                                    prop.custom (
                                                                                        "data-kind",
                                                                                        "note"
                                                                                    )
                                                                                    prop.custom (
                                                                                        "data-title",
                                                                                        "Not translated yet"
                                                                                    )
                                                                                    prop.children
                                                                                        [
                                                                                            Html.p
                                                                                                $"This page has not been translated into %s{context.Page.Locale.Label} yet - you are reading the '%s{source}' version."
                                                                                        ]
                                                                                ]
                                                                        | None -> Html.none
                                                                        if doc.ShowTitle then
                                                                            Html.h1 doc.Title
                                                                        Html.div
                                                                            [
                                                                                prop
                                                                                    .dangerouslySetInnerHTML
                                                                                    context.Content
                                                                            ]
                                                                        if doc.ShowEditLink then
                                                                            Components.editLink
                                                                                options
                                                                                context
                                                                        if doc.ShowPageNav then
                                                                            Components.pageNav
                                                                                options
                                                                                context
                                                                    ]
                                                            ]
                                                        if
                                                            doc.ShowMenu
                                                            && doc.ShowToc
                                                            && not (
                                                                List.isEmpty context.Page.Headings
                                                            )
                                                        then
                                                            Components.toc context
                                                    ]
                                            ]
                                        match options.Footer with
                                        | Some footer ->
                                            Html.footer
                                                [
                                                    prop.className "nacara-footer"
                                                    prop.children [ footer ]
                                                ]
                                        | None -> Html.none
                                        Html.script
                                            [
                                                prop.src (context.Site.UrlOfAsset scriptPath)
                                                prop.custom ("defer", "defer")
                                            ]
                                        for asset in context.Site.PageAssets do
                                            match asset with
                                            | Script(path, defer) ->
                                                Html.script
                                                    [
                                                        prop.src (context.Site.UrlOfAsset path)
                                                        if defer then
                                                            prop.custom ("defer", "defer")
                                                    ]
                                            | InlineScript code ->
                                                Html.script [ prop.dangerouslySetInnerHTML code ]
                                            | Stylesheet _ -> Html.none
                                    ]
                            ]
                    ]
            ]

    /// <summary>The layout for pages using the theme's own front matter.</summary>
    /// <param name="options">The theme's own configuration.</param>
    /// <param name="context">The page and the site around it. Its front matter is read for the
    /// title, the description and which parts of the chrome to leave out.</param>
    let layout (options: ThemeOptions) (context: PageContext<DocFrontMatter>) =
        shell options (DocFrontMatter.toDocPage context.FrontMatter) context

    /// <summary>
    /// A documentation collection using the theme's front matter and layout.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// Site.create "Nacara" |> Site.collection (Theme.docs options "docs")
    /// </code>
    /// </example>
    /// <param name="options">The theme's own configuration, which its layout is given.</param>
    /// <param name="name">What the collection is called, which is also the directory its content
    /// is read from.</param>
    let docs (options: ThemeOptions) (name: string) =
        Collection.create name DocFrontMatter.decoder
        |> Collection.sourceAll name
        |> Collection.title _.Title
        |> Collection.order (fun frontMatter -> frontMatter.Order |> Option.defaultValue 0)
        |> Collection.toc (fun frontMatter ->
            match frontMatter.Toc with
            | Some(TocLevels range) -> Some range
            | Some TocOff
            | None -> None
        )
        |> Collection.layout (layout options)

    /// <summary>
    /// The page a reader gets when a url matches nothing, unless the site writes its own.
    /// </summary>
    let private writeDefaultNotFound (options: ThemeOptions) (context: HookContext) =
        let occupied =
            context.Pages
            |> List.map (fun page -> RelativePath.value (Url.outputPath page.Route))
            |> Set.ofList

        for locale in context.Site.Locales do
            let route = Route.file locale "404.html"
            let path = RelativePath.value (Url.outputPath route)

            if not (occupied.Contains path) then
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
                        Id = $"theme.default:404:%s{locale.Code}"
                        Collection = "theme.default"
                        Source = Generated "theme.default"
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

                let html =
                    layout
                        options
                        {
                            Page = page
                            FrontMatter = frontMatter
                            Site = context.Site
                            Pages = context.Pages
                            Content = body
                        }
                    |> Render.htmlDocument

                context.Write path html |> ignore

    type private ThemePlugin(options: ThemeOptions) =
        interface IPlugin with
            member _.Name = "theme.default"

            member _.Configure registry =
                let parts, cssPath = styles.Value
                let scriptPath, javascript = script.Value

                OfferedMenus.remember (Registry.extras<MenuOutline> registry)

                registry
                |> Registry.asset (Bundle(parts, entryStyleSheet, RelativePath.create cssPath))
                |> Registry.asset (WriteText(javascript, RelativePath.create scriptPath))
                |> Registry.extra (NacaraCodeBlockRenderer() :> ICodeBlockRenderer)
                |> Registry.onBuildComplete (writeDefaultNotFound options)

    /// <summary>The theme's assets. Add it to the site alongside the layout you use.</summary>
    let create (options: ThemeOptions) = ThemePlugin(options) :> IPlugin

    /// <summary>Add the theme's assets to a site.</summary>
    /// <remarks>
    /// Registers what the theme needs of the site. The layout is chosen per collection, with
    /// <see cref="M:Nacara.Theme.Theme.docs" /> or <see cref="M:Nacara.Theme.Theme.layout" />.
    /// </remarks>
    /// <param name="options">The theme's options: your navbar, your footer, your menus.</param>
    /// <param name="site">The site you are describing.</param>
    let register (options: ThemeOptions) (site: Site) = Site.plugin (create options) site
