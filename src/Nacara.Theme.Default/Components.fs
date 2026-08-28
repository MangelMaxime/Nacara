namespace Nacara.Theme

open Feliz.ViewEngine
open Nacara.Core

/// <summary>The building blocks of the default theme.</summary>
/// <remarks>
/// Exposed one by one, not just as a finished page: a site that outgrows the default layout can
/// keep the navbar and the sidebar and replace only the part it cares about.
/// </remarks>
[<RequireQualifiedAccess>]
module Components =

    let private rawHtml (markup: string) =
        Html.span [ prop.dangerouslySetInnerHTML markup ]

    /// <summary>The first segment of a route, which is what this theme calls a section.</summary>
    /// <param name="page">The page to place. Its section decides which navbar entry lights up and
    /// which sidebar it is shown.</param>
    let sectionOf (page: Page) =
        page.Route.Segments |> List.tryHead |> Option.defaultValue ""

    /// <summary>Translations of a page, one per locale that has one.</summary>
    /// <remarks>
    /// Pages are paired by their locale-independent route key, so a translation is found even
    /// though its file lives in another directory - and a locale with no translation of this page
    /// still appears, pointing at its home page rather than at a 404.
    /// </remarks>
    /// <param name="site">The site's locales and where it is served from.</param>
    /// <param name="pages">Every page of the build, which is where a translation is looked
    /// for.</param>
    /// <param name="page">The page being rendered.</param>
    let translationsOf (site: SiteInfo) (pages: Page list) (page: Page) =
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

            locale, site.UrlOf route, translation.IsSome
        )

    let rec private navbarItem (site: SiteInfo) (page: Page) (pages: Page list) (item: NavbarItem) =
        match item with
        | NavbarDivider -> Html.li [ prop.className "nacara-dropdown__divider" ]
        | NavbarLink(label, url) ->
            Html.li
                [
                    Html.a
                        [
                            prop.className "nacara-navbar__link"
                            prop.href url
                            prop.text label
                        ]
                ]
        | NavbarSection(label, section, url) ->
            Html.li
                [
                    Html.a
                        [
                            prop.className "nacara-navbar__link"
                            prop.href url
                            prop.custom (
                                "data-active",
                                (if section = sectionOf page then
                                     "true"
                                 else
                                     "false")
                            )
                            prop.text label
                        ]
                ]
        | NavbarIcon(label, url, svg) ->
            Html.li
                [
                    Html.a
                        [
                            prop.className "nacara-icon-button"
                            prop.href url
                            prop.ariaLabel label
                            prop.title label
                            prop.children [ rawHtml svg ]
                        ]
                ]
        | NavbarDescribed(label, description, url) ->
            Html.li
                [
                    Html.a
                        [
                            prop.className "nacara-dropdown__item"
                            prop.href url
                            prop.children
                                [
                                    Html.span [ prop.text label ]
                                    Html.span
                                        [
                                            prop.className "nacara-dropdown__description"
                                            prop.text description
                                        ]
                                ]
                        ]
                ]
        | NavbarWidget html -> Html.li [ prop.dangerouslySetInnerHTML html ]
        | NavbarDynamicWidget render -> Html.li [ prop.dangerouslySetInnerHTML (render site) ]
        | NavbarLocalePicker ->
            if List.length site.Locales < 2 then
                Html.none
            else
                Html.li
                    [
                        prop.className "nacara-dropdown"
                        prop.children
                            [
                                Html.button
                                    [
                                        prop.className "nacara-navbar__link"
                                        prop.type' "button"
                                        prop.custom ("data-nacara-dropdown", "true")
                                        prop.ariaExpanded false
                                        prop.text page.Locale.Label
                                    ]
                                Html.ul
                                    [
                                        prop.className "nacara-dropdown__panel"
                                        prop.children
                                            [
                                                for locale, url, translated in
                                                    translationsOf site pages page do
                                                    Html.li
                                                        [
                                                            Html.a
                                                                [
                                                                    prop.className
                                                                        "nacara-dropdown__item"
                                                                    prop.href url
                                                                    prop.lang locale.Code
                                                                    if
                                                                        locale.Code = page.Locale
                                                                                .Code
                                                                    then
                                                                        prop.custom (
                                                                            "aria-current",
                                                                            "true"
                                                                        )
                                                                    prop.children
                                                                        [
                                                                            Html.span
                                                                                [
                                                                                    prop.text
                                                                                        locale.Label
                                                                                ]
                                                                            if not translated then
                                                                                Html.span
                                                                                    [
                                                                                        prop
                                                                                            .className
                                                                                            "nacara-dropdown__description"
                                                                                        prop.text
                                                                                            "Not translated yet"
                                                                                    ]
                                                                        ]
                                                                ]
                                                        ]
                                            ]
                                    ]
                            ]
                    ]
        | NavbarDropdown(label, items) ->
            Html.li
                [
                    prop.className "nacara-dropdown"
                    prop.children
                        [
                            Html.button
                                [
                                    prop.className "nacara-navbar__link"
                                    prop.type' "button"
                                    prop.custom ("data-nacara-dropdown", "true")
                                    prop.ariaExpanded false
                                    prop.text label
                                ]
                            Html.ul
                                [
                                    prop.className "nacara-dropdown__panel"
                                    prop.children
                                        [ for item in items -> navbarItem site page pages item ]
                                ]
                        ]
                ]

    /// <summary>The bar across the top: brand, sections, and whatever sits at its end.</summary>
    /// <param name="options">The theme's configuration, whose <c>Navbar</c> and <c>NavbarEnd</c>
    /// this renders.</param>
    /// <param name="context">The page being rendered, and the site around it.</param>
    let navbar (options: ThemeOptions) (context: PageContext<'FrontMatter>) =
        let site = context.Site
        let page = context.Page
        let pages = context.Pages

        Html.header
            [
                prop.className "nacara-navbar"
                prop.role "banner"
                prop.children
                    [
                        Html.button
                            [
                                prop.className "nacara-icon-button"
                                prop.type' "button"
                                prop.custom ("data-nacara-menu-toggle", "true")
                                prop.ariaLabel "Open the menu"
                                prop.children [ rawHtml Icons.menu ]
                            ]
                        Html.a
                            [
                                prop.className "nacara-navbar__brand"
                                prop.href (site.UrlOf(Route.home site.RootLocale))
                                prop.text site.Title
                            ]
                        Html.ul
                            [
                                prop.className "nacara-navbar__items"
                                prop.role "navigation"
                                prop.ariaLabel "Main"
                                prop.children
                                    [
                                        for item in options.Navbar ->
                                            navbarItem site page pages item
                                    ]
                            ]
                        Html.ul
                            [
                                prop.className "nacara-navbar__items nacara-navbar__items--end"
                                prop.children
                                    [
                                        for item in options.NavbarEnd do
                                            navbarItem site page pages item

                                        Html.li
                                            [
                                                Html.select
                                                    [
                                                        prop.className "nacara-theme-select"
                                                        prop.custom ("data-nacara-theme", "true")
                                                        prop.ariaLabel "Colour scheme"
                                                        prop.children
                                                            [
                                                                for value, label in
                                                                    [
                                                                        "light", "Light"
                                                                        "dark", "Dark"
                                                                        "system", "System"
                                                                    ] ->
                                                                    Html.option
                                                                        [
                                                                            prop.value value
                                                                            prop.text label
                                                                        ]
                                                            ]
                                                    ]
                                            ]
                                    ]
                            ]
                    ]
            ]

    /// <summary>Pages of the current section, in menu order.</summary>
    /// <param name="context">The page being rendered, and the site around it.</param>
    let sectionPages (context: PageContext<'FrontMatter>) =
        let section = sectionOf context.Page

        context.Pages
        |> List.filter (fun page ->
            page.Collection = context.Page.Collection
            && page.Locale.Code = context.Page.Locale.Code
            && sectionOf page = section
        )
        |> List.sortBy (fun page -> page.Order, page.Title)

    let private findPage (pages: Page list) (path: string) =
        pages
        |> List.tryFind (fun page ->
            let id = page.Id

            id.EndsWith(":" + path)
            || id.EndsWith(":" + path + ".md")
            || (
                match page.ProjectPath with
                | Some projectPath -> RelativePath.value projectPath = path
                | None -> false
            )
        )

    /// <summary>Flatten a menu into the page order a reader walks through.</summary>
    let rec private menuPages (pages: Page list) (items: MenuItem list) =
        items
        |> List.map _.Entry
        |> List.collect (
            function
            | MenuPage path -> findPage pages path |> Option.toList
            | MenuGroup(path, items) ->
                (findPage pages path |> Option.toList) @ menuPages pages items
            | MenuSection(_, items) -> menuPages pages items
            | MenuLink _ -> []
        )

    /// <summary>Whether a menu holds the page being read, so its trail can be open.</summary>
    let rec private holds (pages: Page list) (current: Page) (items: MenuItem list) =
        items
        |> List.map _.Entry
        |> List.exists (
            function
            | MenuPage path
            | MenuGroup(path, _) when
                match findPage pages path with
                | Some page -> page.Id = current.Id
                | None -> false
                ->
                true
            | MenuGroup(_, items)
            | MenuSection(_, items) -> holds pages current items
            | MenuPage _
            | MenuLink _ -> false
        )

    /// <summary>
    /// One level of the sidebar.
    /// </summary>
    let rec private menuEntries
        (site: SiteInfo)
        (current: Page)
        (pages: Page list)
        (depth: int)
        (items: MenuItem list)
        =
        items
        |> List.map (fun item ->
            let badge =
                match item.Badge with
                | Some badge ->
                    Html.span
                        [
                            prop.className "nacara-badge"
                            prop.custom ("data-kind", badge.Kind)
                            prop.text badge.Label
                        ]
                | None -> Html.none

            match item.Entry with
            | MenuLink(label, url) ->
                Html.li
                    [
                        Html.a
                            [
                                prop.className "nacara-sidebar__link"
                                prop.href url
                                prop.children
                                    [
                                        Html.span label
                                        badge
                                    ]
                            ]
                    ]
            | MenuPage path ->
                match findPage pages path with
                | None -> Html.none
                | Some page ->
                    Html.li
                        [
                            Html.a
                                [
                                    prop.className "nacara-sidebar__link"
                                    prop.href (site.UrlOf page.Route)
                                    if page.Id = current.Id then
                                        prop.custom ("aria-current", "page")
                                    prop.children
                                        [
                                            Html.span page.Title
                                            badge
                                        ]
                                ]
                        ]
            | MenuGroup(path, items) ->
                match findPage pages path with
                | None -> Html.none
                | Some page ->
                    Html.li
                        [
                            Html.details
                                [
                                    prop.className "nacara-sidebar__group"
                                    if page.Id = current.Id || holds pages current items then
                                        prop.custom ("open", "")
                                    prop.custom ("data-nacara-menu-group", page.Title)
                                    prop.children
                                        [
                                            Html.summary
                                                [
                                                    prop.className "nacara-sidebar__group-title"
                                                    prop.children
                                                        [
                                                            Html.a
                                                                [
                                                                    prop.className
                                                                        "nacara-sidebar__group-link"
                                                                    prop.href (
                                                                        site.UrlOf page.Route
                                                                    )
                                                                    if page.Id = current.Id then
                                                                        prop.custom (
                                                                            "aria-current",
                                                                            "page"
                                                                        )
                                                                    prop.children
                                                                        [
                                                                            Html.span page.Title
                                                                            badge
                                                                        ]
                                                                ]
                                                        ]
                                                ]
                                            Html.ul
                                                [
                                                    prop.className "nacara-sidebar__list"
                                                    prop.children (
                                                        menuEntries
                                                            site
                                                            current
                                                            pages
                                                            (depth + 1)
                                                            items
                                                    )
                                                ]
                                        ]
                                ]
                        ]
            | MenuSection(label, items) ->
                let children =
                    Html.ul
                        [
                            prop.className "nacara-sidebar__list"
                            prop.children (menuEntries site current pages (depth + 1) items)
                        ]

                if depth = 0 then
                    Html.li
                        [
                            Html.div
                                [
                                    prop.className "nacara-sidebar__section"
                                    prop.children
                                        [
                                            Html.p
                                                [
                                                    prop.className "nacara-sidebar__title"
                                                    prop.children
                                                        [
                                                            Html.span label
                                                            badge
                                                        ]
                                                ]
                                            children
                                        ]
                                ]
                        ]
                else
                    Html.li
                        [
                            Html.details
                                [
                                    prop.className "nacara-sidebar__group"
                                    if holds pages current items then
                                        prop.custom ("open", "")
                                    prop.custom ("data-nacara-menu-group", label)
                                    prop.children
                                        [
                                            Html.summary
                                                [
                                                    prop.className "nacara-sidebar__group-title"
                                                    prop.children
                                                        [
                                                            Html.span label
                                                            badge
                                                        ]
                                                ]
                                            children
                                        ]
                                ]
                        ]
        )

    /// <summary>A navbar item, as the drawer lists it on a screen too narrow for the bar.</summary>
    /// <remarks>
    /// A dropdown becomes a titled group: nothing hovers in a drawer, and the drawer is a list
    /// already. What sits at the end of the bar is moved down here by the theme's script instead,
    /// so a widget is left where it is rather than rendered twice.
    /// </remarks>
    /// <param name="page">The page being rendered, which says which section is the current one.</param>
    /// <param name="item">The navbar item to render.</param>
    let rec private drawerItem (page: Page) (item: NavbarItem) =
        let link (label: string) (url: string) (current: bool) =
            Html.li
                [
                    Html.a
                        [
                            prop.className "nacara-sidebar__link"
                            prop.href url
                            if current then
                                prop.custom ("aria-current", "true")
                            prop.text label
                        ]
                ]

        match item with
        | NavbarLink(label, url) -> link label url false
        | NavbarSection(label, section, url) -> link label url (section = sectionOf page)
        | NavbarIcon(label, url, _) -> link label url false
        | NavbarDescribed(label, _, url) -> link label url false
        | NavbarDropdown(label, items) ->
            Html.li
                [
                    Html.p
                        [
                            prop.className "nacara-sidebar__title"
                            prop.text label
                        ]
                    Html.ul
                        [
                            prop.className "nacara-sidebar__list"
                            prop.children [ for item in items -> drawerItem page item ]
                        ]
                ]
        | NavbarDivider
        | NavbarLocalePicker
        | NavbarWidget _
        | NavbarDynamicWidget _ -> Html.none

    /// <summary>
    /// The sidebar of the current section.
    /// </summary>
    /// <remarks>
    /// <para>An explicit menu wins; otherwise the section's own pages are listed in front-matter
    /// order.</para>
    /// <para>It is rendered even when a page asks for no menu, because it is also the drawer the
    /// navbar's sections fold into once the bar is too narrow to hold them.</para>
    /// </remarks>
    /// <param name="options">The theme's configuration, whose <c>Menus</c> this renders. A section
    /// with no menu declared falls back to its pages in their own order.</param>
    /// <param name="doc">What the page asked for: whether the menu offers a filter, and whether it
    /// remembers folding across pages.</param>
    /// <param name="context">The page being rendered, and the site around it.</param>
    let sidebar (options: ThemeOptions) (doc: DocPage) (context: PageContext<'FrontMatter>) =
        let section = sectionOf context.Page

        let sections =
            Html.ul
                [
                    prop.className "nacara-sidebar__sections"
                    prop.children [ for item in options.Navbar -> drawerItem context.Page item ]
                ]

        let declared =
            match Map.tryFind section options.Menus with
            | Some items -> Some items
            | None ->
                OfferedMenus.forSection section
                |> Option.map (fun outline -> Menu.ofOutline outline.Items)

        let entries =
            match declared with
            | Some items -> menuEntries context.Site context.Page context.Pages 0 items
            | None ->
                sectionPages context
                |> List.map (fun page ->
                    Html.li
                        [
                            Html.a
                                [
                                    prop.className "nacara-sidebar__link"
                                    prop.href (context.Site.UrlOf page.Route)
                                    if page.Id = context.Page.Id then
                                        prop.custom ("aria-current", "page")
                                    prop.text page.Title
                                ]
                        ]
                )

        let filtered =
            match doc.MenuFilter with
            | Some answer -> answer
            | None ->
                let rec count (items: MenuItem list) =
                    items
                    |> List.sumBy (fun item ->
                        match item.Entry with
                        | MenuGroup(_, children) -> 1 + count children
                        | MenuSection(_, children) -> count children
                        | _ -> 1
                    )

                declared
                |> Option.map (fun items -> count items > 30)
                |> Option.defaultValue false

        Html.nav
            [
                prop.className (
                    if doc.ShowMenu then
                        "nacara-sidebar"
                    else
                        // Nothing to show beside the page, and the sections still to offer on a phone.
                        "nacara-sidebar nacara-sidebar--drawer"
                )
                prop.ariaLabel "Section"
                if not doc.MenuMemory then
                    prop.custom ("data-nacara-menu-memory", "false")
                prop.children
                    [
                        sections

                        if filtered then
                            Html.input
                                [
                                    prop.type' "search"
                                    prop.className "nacara-sidebar__filter"
                                    prop.custom ("data-nacara-menu-filter", "true")
                                    prop.ariaLabel "Filter this menu"
                                    prop.placeholder "Filter"
                                ]

                        if doc.ShowMenu then
                            Html.ul
                                [
                                    prop.className "nacara-sidebar__list"
                                    prop.children entries
                                ]
                    ]
            ]

    /// <summary>The headings of this page, in the right-hand column.</summary>
    /// <param name="context">The page being rendered, and the site around it.</param>
    let toc (context: PageContext<'FrontMatter>) =
        Html.nav
            [
                prop.className "nacara-toc"
                prop.ariaLabel "On this page"
                prop.children
                    [
                        Html.p
                            [
                                prop.className "nacara-toc__title"
                                prop.text "On this page"
                            ]
                        Html.ul
                            [
                                prop.className "nacara-toc__list"
                                prop.children
                                    [
                                        for heading in context.Page.Headings do
                                            Html.li
                                                [
                                                    Html.a
                                                        [
                                                            prop.className "nacara-toc__link"
                                                            prop.custom (
                                                                "data-level",
                                                                string heading.Level
                                                            )
                                                            prop.href ("#" + heading.Anchor)
                                                            prop.text heading.Text
                                                        ]
                                                ]
                                    ]
                            ]
                    ]
            ]

    /// <summary>Previous and next links, following the order of the sidebar.</summary>
    /// <param name="options">The theme's configuration, read for the menu that decides what comes
    /// before and after.</param>
    /// <param name="context">The page being rendered, and the site around it.</param>
    let pageNav (options: ThemeOptions) (context: PageContext<'FrontMatter>) =
        let ordered =
            match Map.tryFind (sectionOf context.Page) options.Menus with
            | Some items -> menuPages context.Pages items
            | None -> sectionPages context

        let index = ordered |> List.tryFindIndex (fun page -> page.Id = context.Page.Id)

        let link (className: string) (label: string) (page: Page option) =
            match page with
            | None -> Html.none
            | Some page ->
                Html.a
                    [
                        prop.className $"nacara-page-nav__link %s{className}"
                        prop.href (context.Site.UrlOf page.Route)
                        prop.children
                            [
                                Html.span
                                    [
                                        prop.className "nacara-page-nav__label"
                                        prop.text label
                                    ]
                                Html.span [ prop.text page.Title ]
                            ]
                    ]

        match index with
        | None -> Html.none
        | Some index ->
            let previous =
                if index > 0 then
                    Some(List.item (index - 1) ordered)
                else
                    None

            let next =
                if index < List.length ordered - 1 then
                    Some(List.item (index + 1) ordered)
                else
                    None

            if previous.IsNone && next.IsNone then
                Html.none
            else
                Html.nav
                    [
                        prop.className "nacara-page-nav"
                        prop.children
                            [
                                link "nacara-page-nav__link--previous" "Previous" previous
                                link "nacara-page-nav__link--next" "Next" next
                            ]
                    ]

    /// <summary>Link back to the source of the page in its repository.</summary>
    /// <param name="options">The theme's configuration; nothing is rendered unless
    /// <c>EditUrlBase</c> says where the repository is.</param>
    /// <param name="context">The page being rendered, and the site around it.</param>
    let editLink (options: ThemeOptions) (context: PageContext<'FrontMatter>) =
        match options.EditUrlBase, context.Page.ProjectPath with
        | Some editBase, Some path ->
            Html.a
                [
                    prop.className "nacara-edit-link"
                    prop.href (editBase.TrimEnd '/' + "/" + RelativePath.value path)
                    prop.children
                        [
                            rawHtml Icons.edit
                            Html.span [ prop.text " Edit this page" ]
                        ]
                ]
        | _ -> Html.none
