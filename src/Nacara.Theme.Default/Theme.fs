namespace Nacara.Theme

open System.Reflection
open Feliz.ViewEngine
open Nacara.Core
open Nacara.Theme.Core
open Nacara.Theme.Docs

/// <summary>The default theme: a documentation layout and the assets it needs.</summary>
[<RequireQualifiedAccess>]
module Theme =

    let defaults: ThemeOptions =
        {
            Docs = DocsOptions.defaults
            HeadExtra = []
            Css = []
            Footer = None
            FavIcon = None
            Parts = Parts.defaults
        }

    let private withDocs (configure: DocsOptions -> DocsOptions) (options: ThemeOptions) =
        { options with
            Docs = configure options.Docs
        }

    /// <summary>How many pages a section's menu writes into a page before it prunes.</summary>
    /// <param name="value">The value to use. <c>0</c> never prunes.</param>
    /// <param name="options">The options so far.</param>
    let menuLimit value (options: ThemeOptions) =
        withDocs (DocsOptions.menuLimit value) options

    /// <summary>Which groups are pruned, once a menu is over the limit.</summary>
    /// <param name="value">The value to use. <c>0</c> prunes none of them.</param>
    /// <param name="options">The options so far.</param>
    let menuGroupLimit value (options: ThemeOptions) =
        withDocs (DocsOptions.menuGroupLimit value) options

    /// <summary>The items on the left of the navbar, after the site's title.</summary>
    /// <param name="value">The items, in the order they are shown.</param>
    /// <param name="options">The options so far.</param>
    let navbar value (options: ThemeOptions) =
        withDocs (DocsOptions.navbar value) options

    /// <summary>The items on the right of the navbar - search, a repository link, the theme toggle.</summary>
    /// <param name="value">The items, in the order they are shown.</param>
    /// <param name="options">The options so far.</param>
    let navbarEnd value (options: ThemeOptions) =
        withDocs (DocsOptions.navbarEnd value) options

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
        withDocs (DocsOptions.menu section items) options

    /// <summary>Every menu at once, replacing any set before.</summary>
    /// <param name="value">The menus, keyed by section.</param>
    /// <param name="options">The options so far.</param>
    let menus value (options: ThemeOptions) =
        withDocs (DocsOptions.menus value) options

    /// <summary>Base URL for the "edit this page" link.</summary>
    /// <param name="value">Where an edit starts, such as
    /// <c>https://github.com/owner/repo/edit/main/docs</c>.</param>
    /// <param name="options">The options so far.</param>
    let editUrl value (options: ThemeOptions) =
        withDocs (DocsOptions.editUrl value) options

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

    /// <summary>Replace how a part of the page is rendered, keeping the rest.</summary>
    /// <example>
    /// <code lang="fsharp">
    /// Theme.defaults
    /// |> Theme.parts (fun parts ->
    ///     { parts with
    ///         Footer = fun context -> Html.div [ Parts.footer context; Html.p "Hosted by us" ] })
    /// </code>
    /// </example>
    /// <param name="configure">What to change of the parts, starting from the theme's own.</param>
    /// <param name="options">The options so far.</param>
    let parts (configure: ThemeParts -> ThemeParts) (options: ThemeOptions) =
        { options with
            Parts = configure options.Parts
        }

    let private readResource = Resource.text (Assembly.GetExecutingAssembly())

    let private entryStyleSheet = "nacara.css"

    let private styleSheetParts =
        [
            entryStyleSheet
            "base.css"
            "navbar.css"
            "layout.css"
            "responsive.css"
        ]

    /// <summary>The stylesheet's parts, and where the bundle is served from.</summary>
    let private styles =
        lazy
            (styleSheetParts
             |> List.map (fun name -> name, readResource $"css.%s{name}")
             |> Styles.bundle "nacara")

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
        let scope = PageScope.ofContext context
        let site = context.Site
        let parts = options.Parts

        let part =
            {
                Scope = scope
                Doc = doc
                Options = options
            }

        let render (renderer: PartContext -> 'Model -> ReactElement) (model: 'Model option) =
            match model with
            | Some model -> renderer part model
            | None -> Html.none

        let editLink =
            if doc.ShowEditLink then
                EditLink.resolve options.Docs scope |> render parts.EditLink
            else
                Html.none

        let pageNav =
            if doc.ShowPageNav then
                PageNav.resolve options.Docs scope |> render parts.PageNav
            else
                Html.none

        let toc =
            if doc.ShowMenu then
                Toc.resolve doc scope |> Option.map (parts.Toc part)
            else
                None

        Html.html
            [
                prop.lang context.Page.Locale.Code
                prop.custom ("dir", context.Page.Locale.Direction.HtmlValue)
                prop.children
                    [
                        Html.head
                            [
                                yield!
                                    parts.Head
                                        part
                                        (Head.resolve
                                            doc.Title
                                            doc.Description
                                            options.FavIcon
                                            scope)
                                Html.link
                                    [
                                        prop.rel "stylesheet"
                                        prop.href (site.UrlOfAsset cssPath)
                                    ]
                                Behaviours.bootstrapTag ()
                                yield! Shell.stylesheetLinks site
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
                                prop.custom ("data-section", Pages.section context.Page)
                                prop.children
                                    [
                                        Shell.skipLink "nacara-skip-link" "#nacara-content"
                                        parts.Frame
                                            part
                                            {
                                                Navbar =
                                                    parts.Navbar
                                                        part
                                                        (Navbar.resolve options.Docs scope)
                                                Sidebar =
                                                    parts.Sidebar
                                                        part
                                                        (Sidebar.resolve options.Docs doc scope)
                                                Main =
                                                    parts.Article
                                                        part
                                                        {
                                                            Content = context.Content
                                                            EditLink = editLink
                                                            PageNav = pageNav
                                                        }
                                                Toc = toc
                                                Footer = parts.Footer part
                                            }
                                        Behaviours.scriptTag site
                                        yield! Shell.scripts site
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
    let docs (options: ThemeOptions) (name: string) = Docs.collection name (layout options)

    type private ThemePlugin(options: ThemeOptions) =
        interface IPlugin with
            member _.Name = "theme.default"

            member _.Configure registry =
                let parts, cssPath = styles.Value

                registry
                |> ThemeDocs.configure
                    {
                        Origin = "theme.default"
                        Docs = options.Docs
                        NotFound = Some(layout options)
                    }
                |> Registry.asset (Bundle(parts, entryStyleSheet, RelativePath.create cssPath))

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
