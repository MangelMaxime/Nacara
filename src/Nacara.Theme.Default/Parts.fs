namespace Nacara.Theme

open Feliz.ViewEngine
open Nacara.Core
open Nacara.Theme.Core
open Nacara.Theme.Docs

/// <summary>How the default theme renders each part of a page.</summary>
/// <remarks>
/// Exposed one by one, so a site can replace one of them with
/// <see cref="M:Nacara.Theme.Theme.parts" /> and keep the rest, or wrap one to add to it.
/// </remarks>
[<RequireQualifiedAccess>]
module Parts =

    let private rawHtml (markup: string) =
        Html.span [ prop.dangerouslySetInnerHTML markup ]

    /// <summary>The tags of the head, as <see cref="M:Nacara.Theme.Core.Head.meta" /> writes them.</summary>
    let head (_: PartContext) (model: HeadModel) = Head.meta model

    let rec private navbarEntry (entry: NavbarEntry) =
        match entry with
        | NavbarEntry.Divider -> Html.li [ prop.className "nacara-dropdown__divider" ]
        | NavbarEntry.Link(label, url) ->
            Html.li
                [
                    Html.a
                        [
                            prop.className "nacara-navbar__link"
                            prop.href url
                            prop.text label
                        ]
                ]
        | NavbarEntry.Section(label, url, active) ->
            Html.li
                [
                    Html.a
                        [
                            prop.className "nacara-navbar__link"
                            prop.href url
                            prop.custom (
                                "data-active",
                                (if active then
                                     "true"
                                 else
                                     "false")
                            )
                            prop.text label
                        ]
                ]
        | NavbarEntry.Icon(label, url, svg) ->
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
        | NavbarEntry.Described(label, description, url) ->
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
        | NavbarEntry.Widget html -> Html.li [ prop.dangerouslySetInnerHTML html ]
        | NavbarEntry.LocalePicker(current, translations) ->
            Html.li
                [
                    prop.className "nacara-dropdown"
                    prop.custom ("data-nacara-dropdown-root", "true")
                    prop.children
                        [
                            Html.button
                                [
                                    prop.className "nacara-navbar__link"
                                    prop.type' "button"
                                    prop.custom ("data-nacara-dropdown", "true")
                                    prop.ariaExpanded false
                                    prop.text current.Label
                                ]
                            Html.ul
                                [
                                    prop.className "nacara-dropdown__panel"
                                    prop.children
                                        [
                                            for translation in translations do
                                                Html.li
                                                    [
                                                        Html.a
                                                            [
                                                                prop.className
                                                                    "nacara-dropdown__item"
                                                                prop.href translation.Url
                                                                prop.lang translation.Locale.Code
                                                                if translation.Current then
                                                                    prop.custom (
                                                                        "aria-current",
                                                                        "true"
                                                                    )
                                                                prop.children
                                                                    [
                                                                        Html.span
                                                                            [
                                                                                prop.text
                                                                                    translation
                                                                                        .Locale
                                                                                        .Label
                                                                            ]
                                                                        if
                                                                            not
                                                                                translation
                                                                                    .Translated
                                                                        then
                                                                            Html.span
                                                                                [
                                                                                    prop.className
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
        | NavbarEntry.Dropdown(label, items) ->
            Html.li
                [
                    prop.className "nacara-dropdown"
                    prop.custom ("data-nacara-dropdown-root", "true")
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
                                    prop.children [ for item in items -> navbarEntry item ]
                                ]
                        ]
                ]

    /// <summary>The bar across the top: brand, sections, and whatever sits at its end.</summary>
    let navbar (_: PartContext) (model: NavbarModel) =
        Html.header
            [
                prop.className "nacara-navbar"
                prop.role "banner"
                prop.custom ("data-nacara-navbar", "true")
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
                                prop.href model.BrandUrl
                                prop.children
                                    [
                                        match model.BrandIcon with
                                        | Some(BrandIcon.Image url) ->
                                            Html.img
                                                [
                                                    prop.className "nacara-navbar__brand-icon"
                                                    prop.src url
                                                    prop.alt ""
                                                ]
                                        | Some(BrandIcon.Svg markup) ->
                                            Html.span
                                                [
                                                    prop.className "nacara-navbar__brand-icon"
                                                    prop.ariaHidden true
                                                    prop.dangerouslySetInnerHTML markup
                                                ]
                                        | None -> ()

                                        Html.text model.BrandLabel
                                    ]
                            ]
                        Html.ul
                            [
                                prop.className "nacara-navbar__items"
                                prop.role "navigation"
                                prop.ariaLabel "Main"
                                prop.children [ for entry in model.Start -> navbarEntry entry ]
                            ]
                        Html.ul
                            [
                                prop.className "nacara-navbar__items nacara-navbar__items--end"
                                prop.custom ("data-nacara-widgets-end", "true")
                                prop.children
                                    [
                                        for entry in model.End do
                                            navbarEntry entry

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

    /// <summary>A navbar entry, as the drawer lists it on a screen too narrow for the bar.</summary>
    /// <remarks>
    /// A dropdown becomes a titled group. What sits at the end of the bar is moved down here by
    /// the theme's script, so a widget is left where it is.
    /// </remarks>
    let rec private drawerEntry (entry: NavbarEntry) =
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

        match entry with
        | NavbarEntry.Link(label, url) -> link label url false
        | NavbarEntry.Section(label, url, active) -> link label url active
        | NavbarEntry.Icon(label, url, _) -> link label url false
        | NavbarEntry.Described(label, _, url) -> link label url false
        | NavbarEntry.Dropdown(label, items) ->
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
                            prop.children [ for item in items -> drawerEntry item ]
                        ]
                ]
        | NavbarEntry.Divider
        | NavbarEntry.LocalePicker _
        | NavbarEntry.Widget _ -> Html.none

    let private badge (node: MenuNode) =
        match node.Badge with
        | Some badge ->
            Html.span
                [
                    prop.className "nacara-badge"
                    prop.custom ("data-kind", badge.Kind)
                    prop.text badge.Label
                ]
        | None -> Html.none

    /// <summary>One level of the sidebar.</summary>
    let rec private menuNodes (depth: int) (nodes: MenuNode list) =
        nodes
        |> List.map (fun node ->
            match node.Kind, node.Url with
            | MenuNodeKind.Link, Some url
            | MenuNodeKind.Page, Some url ->
                Html.li
                    [
                        Html.a
                            [
                                prop.className "nacara-sidebar__link"
                                prop.href url
                                if node.Current then
                                    prop.custom ("aria-current", "page")
                                prop.children
                                    [
                                        Html.span node.Label
                                        badge node
                                    ]
                            ]
                    ]
            | MenuNodeKind.Group, Some url ->
                Html.li
                    [
                        Html.details
                            [
                                prop.className "nacara-sidebar__group"
                                if node.Open then
                                    prop.custom ("open", "")
                                prop.custom ("data-nacara-menu-group", node.Label)
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
                                                                prop.href url
                                                                if node.Current then
                                                                    prop.custom (
                                                                        "aria-current",
                                                                        "page"
                                                                    )
                                                                prop.children
                                                                    [
                                                                        Html.span node.Label
                                                                        badge node
                                                                    ]
                                                            ]
                                                    ]
                                            ]
                                        Html.ul
                                            [
                                                prop.className "nacara-sidebar__list"
                                                prop.children (menuNodes (depth + 1) node.Children)
                                            ]
                                    ]
                            ]
                    ]
            | MenuNodeKind.Section, _
            | _, None ->
                let children =
                    Html.ul
                        [
                            prop.className "nacara-sidebar__list"
                            prop.children (menuNodes (depth + 1) node.Children)
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
                                                            Html.span node.Label
                                                            badge node
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
                                    if node.Open then
                                        prop.custom ("open", "")
                                    prop.custom ("data-nacara-menu-group", node.Label)
                                    prop.children
                                        [
                                            Html.summary
                                                [
                                                    prop.className "nacara-sidebar__group-title"
                                                    prop.children
                                                        [
                                                            Html.span node.Label
                                                            badge node
                                                        ]
                                                ]
                                            children
                                        ]
                                ]
                        ]
        )

    /// <summary>The sidebar: the navbar's sections for a narrow screen, the filter box, and the
    /// section's menu.</summary>
    let sidebar (_: PartContext) (model: SidebarModel) =
        let entries =
            match model.Source with
            | MenuSource.Declared
            | MenuSource.Offered -> menuNodes 0 model.Tree
            | MenuSource.SectionPages ->
                model.Tree
                |> List.map (fun node ->
                    Html.li
                        [
                            Html.a
                                [
                                    prop.className "nacara-sidebar__link"
                                    prop.href (node.Url |> Option.defaultValue "")
                                    if node.Current then
                                        prop.custom ("aria-current", "page")
                                    prop.text node.Label
                                ]
                        ]
                )

        Html.nav
            [
                prop.className (
                    if model.ShowMenu then
                        "nacara-sidebar"
                    else
                        // Nothing to show beside the page, and the sections still to offer on a phone.
                        "nacara-sidebar nacara-sidebar--drawer"
                )
                prop.ariaLabel "Section"
                prop.custom ("data-nacara-sidebar", "true")
                if not model.Memory then
                    prop.custom ("data-nacara-menu-memory", "false")
                prop.children
                    [
                        Html.ul
                            [
                                prop.className "nacara-sidebar__sections"
                                prop.children [ for entry in model.Drawer -> drawerEntry entry ]
                            ]

                        match model.Filter with
                        | Some filter ->
                            Html.input
                                [
                                    prop.type' "search"
                                    prop.className "nacara-sidebar__filter"
                                    prop.custom ("data-nacara-menu-filter", "true")
                                    // The menu this page holds is a part of the section's, so the
                                    // box is told where the rest of it is.
                                    match filter.SourceUrl with
                                    | Some url -> prop.custom ("data-nacara-menu-source", url)
                                    | None -> ()
                                    prop.ariaLabel "Filter this menu"
                                    prop.placeholder "Filter"
                                ]
                        | None -> ()

                        if model.ShowMenu then
                            Html.ul
                                [
                                    prop.className "nacara-sidebar__list"
                                    prop.custom ("data-nacara-menu-list", "true")
                                    prop.children entries
                                ]

                        // What the filter box grafts the rest of a pruned menu with.
                        match model.Filter with
                        | Some {
                                   SourceUrl = Some _
                               } ->
                            Interop.createElement
                                "template"
                                [
                                    prop.custom ("data-nacara-menu-leaf-template", "true")
                                    prop.children
                                        [
                                            Html.li
                                                [
                                                    Html.a
                                                        [
                                                            prop.className "nacara-sidebar__link"
                                                            prop.children [ Html.span [] ]
                                                        ]
                                                ]
                                        ]
                                ]

                            Interop.createElement
                                "template"
                                [
                                    prop.custom ("data-nacara-menu-group-template", "true")
                                    prop.children
                                        [
                                            Html.li
                                                [
                                                    Html.details
                                                        [
                                                            prop.className "nacara-sidebar__group"
                                                            prop.children
                                                                [
                                                                    Html.summary
                                                                        [
                                                                            prop.className
                                                                                "nacara-sidebar__group-title"
                                                                            prop.children
                                                                                [
                                                                                    Html.a
                                                                                        [
                                                                                            prop
                                                                                                .className
                                                                                                "nacara-sidebar__group-link"
                                                                                            prop
                                                                                                .children
                                                                                                [
                                                                                                    Html
                                                                                                        .span
                                                                                                        []
                                                                                                ]
                                                                                        ]
                                                                                ]
                                                                        ]
                                                                    Html.ul
                                                                        [
                                                                            prop.className
                                                                                "nacara-sidebar__list"
                                                                        ]
                                                                ]
                                                        ]
                                                ]
                                        ]
                                ]
                        | _ -> ()

                        // Where the navbar's widgets go on a screen too narrow for the bar.
                        Html.ul
                            [
                                prop.className "nacara-sidebar__widgets"
                                prop.custom ("data-nacara-widgets-shelf", "true")
                                prop.custom ("hidden", "")
                            ]
                    ]
            ]

    /// <summary>The headings of this page, in the right-hand column.</summary>
    let toc (_: PartContext) (model: TocModel) =
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
                                        for heading in model.Headings do
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
                                                            prop.custom (
                                                                "data-nacara-toc-link",
                                                                "true"
                                                            )
                                                            prop.text heading.Text
                                                        ]
                                                ]
                                    ]
                            ]
                    ]
            ]

    /// <summary>Previous and next links, following the order of the sidebar.</summary>
    let pageNav (_: PartContext) (model: PageNavModel) =
        let link (className: string) (label: string) (page: PageLink option) =
            match page with
            | None -> Html.none
            | Some page ->
                Html.a
                    [
                        prop.className $"nacara-page-nav__link %s{className}"
                        prop.href page.Url
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

        Html.nav
            [
                prop.className "nacara-page-nav"
                prop.children
                    [
                        link "nacara-page-nav__link--previous" "Previous" model.Previous
                        link "nacara-page-nav__link--next" "Next" model.Next
                    ]
            ]

    /// <summary>Link back to the source of the page in its repository.</summary>
    let editLink (_: PartContext) (model: EditLinkModel) =
        Html.a
            [
                prop.className "nacara-edit-link"
                prop.href model.Url
                prop.children
                    [
                        rawHtml Icons.edit
                        Html.span [ prop.text " Edit this page" ]
                    ]
            ]

    /// <summary>The <c>&lt;main&gt;</c>: a translation notice, the title, the content, the edit
    /// link and the previous and next links.</summary>
    /// <param name="context">The page being rendered, and what it asked for.</param>
    /// <param name="slots">The content and what follows it, rendered already.</param>
    let article (context: PartContext) (slots: ArticleSlots) =
        Html.main
            [
                for name, value in context.Doc.MainAttributes do
                    prop.custom (name, value)
                prop.id "nacara-content"
                prop.tabIndex -1

                if context.Doc.Styled then
                    prop.className "nacara-content"
                yield! Shell.generatedBy context.Page
                prop.children
                    [
                        Shell.untranslatedNotice context.Page
                        if context.Doc.ShowTitle then
                            Html.h1 context.Doc.Title
                        Html.div [ prop.dangerouslySetInnerHTML slots.Content ]
                        slots.EditLink
                        slots.PageNav
                    ]
            ]

    /// <summary>What every page ends with: the footer the site gave, or nothing.</summary>
    let footer (context: PartContext) =
        match context.Options.Footer with
        | Some footer ->
            Html.footer
                [
                    prop.className "nacara-footer"
                    prop.children [ footer ]
                ]
        | None -> Html.none

    /// <summary>The navbar, then the sidebar, the content and the table of contents side by side,
    /// then the footer.</summary>
    /// <param name="context">The page being rendered, and what it asked for.</param>
    /// <param name="slots">The pieces, rendered already.</param>
    let frame (context: PartContext) (slots: FrameSlots) =
        let doc = context.Doc

        let layoutClass =
            if not doc.Styled then
                "nacara-layout nacara-layout--bare"
            elif not doc.ShowMenu then
                "nacara-layout nacara-layout--splash"
            elif not doc.ShowToc then
                "nacara-layout nacara-layout--no-toc"
            else
                "nacara-layout"

        React.fragment
            [
                slots.Navbar
                Html.div
                    [
                        prop.className layoutClass
                        prop.children
                            [
                                // Rendered even with no menu to show: on a narrow screen it is what
                                // the navbar's sections fold into.
                                slots.Sidebar
                                slots.Main
                                match slots.Toc with
                                | Some toc -> toc
                                | None -> ()
                            ]
                    ]
                slots.Footer
            ]

    /// <summary>Every part, as the theme renders it.</summary>
    let defaults: ThemeParts =
        {
            Head = head
            Navbar = navbar
            Sidebar = sidebar
            Toc = toc
            PageNav = pageNav
            EditLink = editLink
            Article = article
            Footer = footer
            Frame = frame
        }
