module Page.Minimal

open Fable.FontAwesome
open Nacara.Core.Types
open Feliz
open Feliz.Bulma

let private renderIconFromClass (iconClass : string) (colorOpt :string option) =
    Bulma.icon [

        match colorOpt with
        | Some color ->
            prop.style [
                style.custom ("color", color)
            ]

        | None ->
            ()

        prop.children [
            Fa.i [ Fa.Icon iconClass
                   Fa.Size Fa.FaLarge ]
                [ ]
        ]
    ]

let private navbarItemIsFromSection (itemSectionOpt : string option) (pageSection : string) =
    match itemSectionOpt with
    | Some itemSection ->
        itemSection = pageSection

    | None ->
        false

let private renderNacaraNavbarDropdown (partials : Partial array) (dropdown : DropdownInfo) (pageSection : string) =
    let guid = System.Guid.NewGuid()

    let isDropdownFromCurrentPage =
        dropdown.Items
        |> List.tryFind (fun item ->
            match item with
            | DropdownItem.Link info ->
                match info.Section with
                | Some section ->
                    section = pageSection
                | None ->
                    false

            | DropdownItem.Divider ->
                false
        )
        |> Option.isSome

    Bulma.navbarItem.div [
        prop.classes [
            "has-nacara-dropdown"
            if dropdown.IsFullWidth then
                "is-fullwidth"
            else
                "is-floating"

            if isDropdownFromCurrentPage then
                "is-current-page"
        ]
        prop.custom("data-guid", guid.ToString())

        // Non-pinned dropdown are not rendered when on mobile
        // They will be included in the navbar menu
        if not dropdown.IsPinned then
            helpers.isHiddenMobile

        prop.children [
            Html.div [
                prop.className "nacara-dropdown-link"
                prop.text dropdown.Label
            ]

            Html.div [
                prop.classes [
                    "nacara-dropdown"
                    // Control how the dropdown is rendered
                    // Useful when using partial layout for the content
                    if dropdown.IsFullWidth then
                        "is-fullwidth"
                    else
                        "is-floating"
                ]

                prop.children [

                    // If there is a partial set use it for the rendering
                    match dropdown.Partial with
                    | Some requestedPartial ->
                        let partialOpt =
                            partials
                            |> Array.tryFind (fun partial ->
                                partial.Id = requestedPartial
                            )

                        match partialOpt with
                        | Some partial ->
                            partial.Module.``default``

                        | None ->
                            Log.error $"""Dropdown '%s{dropdown.Label}' is requesting partial '%s{requestedPartial}' but it was not found.
Please remove the 'partial' property from the dropdown or create the partial file '_partials/%s{requestedPartial}.js' or ''_partials/%s{requestedPartial}.jsx'
                            """
                            null

                    // Otherwise, use the items arrays
                    | None ->

                        for items in dropdown.Items do
                            match items with
                            | DropdownItem.Divider ->
                                Bulma.navbarDivider [ ]

                            | DropdownItem.Link linkInfo ->
                                Html.a [
                                    prop.className "nacara-dropdown-item"
                                    prop.href linkInfo.Url
                                    prop.children [
                                        match linkInfo.Description with
                                        | Some description ->
                                            Html.div [
                                                Html.div [
                                                    Html.strong linkInfo.Label
                                                ]

                                                Html.div [
                                                    prop.className "nacara-dropdown-item-description"
                                                    prop.dangerouslySetInnerHTML description
                                                ]
                                            ]

                                        | None ->
                                            Html.div [
                                                prop.text linkInfo.Label
                                            ]
                                    ]
                                ]
                ]
            ]
        ]
    ]

let private renderNavbarBurgerMenu =
    Bulma.navbarItem.div [
        prop.className "navbar-burger-dots"
        helpers.isHiddenTablet

        prop.children [
            Svg.svg [
                svg.height 4
                svg.stroke "none"
                svg.viewBox(0, 0, 22, 4)
                svg.width 22
                svg.children [
                    Svg.circle [
                        svg.cx 2
                        svg.cy 2
                        svg.r 2
                    ]
                    Svg.circle [
                        svg.cx 2
                        svg.cy 2
                        svg.r 2
                        svg.transform [
                            transform.translate(9, 0)
                        ]
                    ]
                    Svg.circle [
                        svg.cx 2
                        svg.cy 2
                        svg.r 2
                        svg.transform [
                            transform.translate(18, 0)
                        ]
                    ]
                ]
            ]
        ]
    ]

let private renderStartNavbar (partials : Partial array) (pageSection : string) (items : StartNavbarItem list) =
    Bulma.navbarStart.div [
        for item in items do

            match item with
            | StartNavbarItem.LabelLink info ->
                Bulma.navbarItem.a [
                    prop.href info.Url
                    prop.text info.Label
                    if navbarItemIsFromSection info.Section pageSection then
                        navbarItem.isActive
                    if not info.IsPinned then
                        helpers.isHiddenMobile
                ]

            | StartNavbarItem.Dropdown dropdown ->
                renderNacaraNavbarDropdown partials dropdown pageSection

        renderNavbarBurgerMenu
    ]

let private renderEndNavbar (items : EndNavbarItem list) =
    Bulma.navbarEnd.div [
        // On mobile, we hide the navbar end
        // Its content will be include in the navbar menu
        helpers.isHiddenMobile

        prop.children [
            for item in items do

                Bulma.navbarItem.a [
                    prop.href item.Url
                    prop.children [
                        Bulma.icon [
                            Fa.i [ Fa.Icon item.Icon
                                   Fa.Size Fa.FaLarge ]
                                [ ]
                        ]
                    ]
                ]
        ]
    ]


/// <summary>
/// Render the navbar menu.
///
/// It is the menu used on mobile.
/// </summary>
/// <param name="navbarConfig">Configuration of the navbar</param>
/// <returns>A <c>ReactElement</c> representing the navbar menu</returns>
let private renderNacaraNavbarMenu (navbarConfig : NavbarConfig) =
    Html.div [
        prop.className "nacara-navbar-menu"

        prop.children [

            for item in navbarConfig.Start do
                match item with
                | StartNavbarItem.LabelLink info ->
                    // Skip the item if it is pinned
                    if info.IsPinned then
                        null
                    else
                        Html.a [
                            prop.className "nacara-navbar-menu-item"
                            prop.href info.Url
                            prop.text info.Label
                        ]

                | StartNavbarItem.Dropdown dropdown ->
                    // Skip the item if it is pinned
                    if dropdown.IsPinned then
                        null

                    else
                        Html.div [
                            prop.className "nacara-navbar-menu-dropdown"

                            prop.children [
                                Html.div [
                                    prop.className "nacara-navbar-menu-dropdown-label"
                                    prop.text dropdown.Label
                                ]

                                for item in dropdown.Items do
                                    match item with
                                    | DropdownItem.Link info ->
                                        Html.a [
                                            prop.className "nacara-navbar-menu-dropdown-link"
                                            prop.href info.Url
                                            prop.children [
                                                Html.span [
                                                    prop.text info.Label
                                                ]
                                            ]
                                        ]

                                    | DropdownItem.Divider ->
                                        null
                            ]
                        ]

            // Render the navbar end items using the label only
            for item in navbarConfig.End do
                Html.a [
                    prop.className "nacara-navbar-menu-item"
                    prop.href item.Url
                    prop.text item.Label
                ]
        ]
    ]

let private navbar (partials : Partial array) (config : Config) (pageSection : string) =
    Bulma.navbar [
        navbar.isFixedTop
        prop.className "is-spaced"

        prop.children [
            Bulma.container [
                Bulma.navbarBrand.div [
                    Bulma.navbarItem.a [
                        prop.className "title is-4"
                        // In watch mode make the link redirect to the localhost instead of
                        // the real website
                        if config.IsWatch then
                            prop.href config.SiteMetadata.BaseUrl
                        else
                            prop.href (config.SiteMetadata.Url + config.SiteMetadata.BaseUrl)
                        prop.text config.SiteMetadata.Title
                    ]
                ]

                Bulma.navbarMenu [
                    prop.children [
                        renderStartNavbar partials pageSection config.Navbar.Start
                        renderEndNavbar config.Navbar.End
                    ]
                ]

                renderNacaraNavbarMenu config.Navbar
            ]
        ]
    ]

let render
    (rendererContext : RendererContext)
    (pageContext : PageContext)
    (content : ReactElement) =

    let titleStr =
        match pageContext.Title with
        | Some title ->
            rendererContext.Config.SiteMetadata.Title  + " · " + title
        | None ->
            rendererContext.Config.SiteMetadata.Title

    let toUrl (url : string) =
        rendererContext.Config.SiteMetadata.BaseUrl + url

    let footerOpt =
        rendererContext.Partials
        |> Array.tryFind (fun partial ->
            partial.Id = "footer"
        )

    Html.html [
        prop.className "has-navbar-fixed-top"
        prop.custom ("lang", "en")

        prop.children [
            Html.head [
                Html.title titleStr

                Html.meta [
                    prop.httpEquiv.contentType
                    prop.content "text/html; charset=UTF-8"
                    // prop.charset "UTF-8"
                    prop.custom ("charSet", "UTF-8")
                ]

                Html.meta [
                    prop.name "viewport"
                    prop.content "width=device-width, initial-scale=1"
                ]

                match rendererContext.Config.SiteMetadata.FavIcon with
                | Some favIcon ->
                    Html.link [
                        prop.rel "shortcut icon"
                        prop.href (rendererContext.Config.SiteMetadata.BaseUrl + favIcon)
                    ]

                | None ->
                    null

                Html.link [
                    prop.rel "stylesheet"
                    prop.type' "text/css"
                    prop.href (toUrl "style.css")
                ]

                Html.script [
                    prop.src "https://unpkg.com/scroll-into-view-if-needed@2.2.28/umd/scroll-into-view-if-needed.min.js"
                ]
            ]

            Html.body [
                if footerOpt.IsSome then
                    prop.className "has-footer"


                prop.children [
                    navbar rendererContext.Partials rendererContext.Config pageContext.Section

                    Html.div [
                        prop.className "grey-overlay"
                    ]

                    Html.div [
                        prop.className "nacara-content"

                        prop.children [
                            content
                        ]
                    ]

                    Html.script [
                        prop.async true
                        prop.src (rendererContext.Config.SiteMetadata.BaseUrl + Dependencies.menu)
                    ]

                    match footerOpt with
                    | Some footer ->
                        Bulma.footer [
                            footer.Module.``default``
                        ]

                    | None ->
                        null

                    if rendererContext.Config.IsWatch then
                        Html.script [
                            prop.async true
                            prop.src (rendererContext.Config.SiteMetadata.BaseUrl + "resources/nacara/scripts/live-reload.js")
                        ]
                ]
            ]
        ]

    ]
