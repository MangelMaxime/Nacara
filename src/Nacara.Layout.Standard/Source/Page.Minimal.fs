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

let private renderNacaraNavbarDropdown (dropdown : DropdownInfo) =
    Bulma.navbarItem.div [
        prop.className "has-nacara-dropdown"

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
                    for items in dropdown.Items do
                        match items with
                        | DropdownItem.Spacer ->
                            Bulma.navbarDivider [ ]

                        | DropdownItem.Link linkInfo ->
                            Html.a [
                                prop.className "nacara-dropdown-item"
                                prop.children [
                                    Html.div [
                                        Html.div [
                                            Html.strong linkInfo.Label
                                        ]

                                        if linkInfo.Description.IsSome then
                                            Html.div [
                                                prop.className "nacara-dropdown-item-description"
                                                prop.text linkInfo.Description.Value
                                            ]
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

let private renderStartNavbar (pageSection : string) (items : StartNavbarItem list) =
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
                renderNacaraNavbarDropdown dropdown

        renderNavbarBurgerMenu
    ]

let private renderEndNavbar (items : IconLink list) =
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
                        Html.ul [
                            prop.className "nacara-navbar-menu-dropdown"

                            prop.children [
                                Html.li [
                                    prop.className "nacara-navbar-menu-dropdown-label"
                                    prop.text dropdown.Label
                                ]

                                for item in dropdown.Items do
                                    match item with
                                    | DropdownItem.Link info ->
                                        Html.li [
                                            prop.className "nacara-navbar-menu-dropdown-link"
                                            prop.children [
                                                Html.a [
                                                    prop.href info.Url
                                                    prop.text info.Label
                                                ]
                                            ]
                                        ]

                                    | DropdownItem.Spacer ->
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

let private navbar (config : Config) (pageSection : string) =
    Bulma.navbar [
        navbar.isFixedTop
        prop.className "is-spaced"

        prop.children [
            Bulma.container [
                Bulma.navbarBrand.div [
                    Bulma.navbarItem.a [
                        prop.className "title is-4"
                        prop.href (config.Url + config.BaseUrl)
                        prop.text config.Title
                    ]
                ]

                Bulma.navbarMenu [
                    prop.children [
                        renderStartNavbar pageSection config.Navbar.Start
                        renderEndNavbar config.Navbar.End
                    ]
                ]

                renderNacaraNavbarMenu config.Navbar
            ]
        ]
    ]

[<NoComparison>]
type RenderArgs =
    {
        Config : Config
        Section : string
        TitleOpt : string option
        Content : ReactElement
    }

let render (args : RenderArgs) =
    let titleStr =
        match args.TitleOpt with
        | Some title ->
            args.Config.Title  + " · " + title
        | None ->
            args.Config.Title

    let toUrl (url : string) =
        args.Config.BaseUrl + url

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

                match args.Config.FavIcon with
                | Some favIcon ->
                    Html.link [
                        prop.rel "shortcut icon"
                        prop.href (args.Config.BaseUrl + favIcon)
                    ]

                | None ->
                    null

                Html.link [
                    prop.rel "stylesheet"
                    prop.type' "text/css"
                    prop.href (toUrl "style.css")
                ]

                Html.link [
                    prop.rel "stylesheet"
                    prop.href "https://use.fontawesome.com/releases/v5.7.2/css/all.css"
                    prop.integrity "sha384-fnmOCqbTlWIlj8LyTjo7mOUStjsKC4pOpQbqyi7RrhN7udi9RwhKkMHpvLbHG9Sr"
                    prop.crossOrigin.anonymous
                ]

                Html.script [
                    prop.src "https://unpkg.com/scroll-into-view-if-needed@2.2.28/umd/scroll-into-view-if-needed.min.js"
                ]
            ]

            Html.body [
                navbar args.Config args.Section

                Html.div [
                    prop.className "grey-overlay"
                ]

                args.Content

                Html.script [
                    prop.async true
                    prop.src (args.Config.BaseUrl + Dependencies.menu)
                ]

                if args.Config.IsWatch then
                    Html.script [
                        prop.async true
                        prop.src (args.Config.BaseUrl + "resources/nacara/scripts/live-reload.js")
                    ]
            ]
        ]

    ]
