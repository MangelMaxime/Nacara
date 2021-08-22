module Page.Minimal

open Fable.FontAwesome
open Nacara.Core.Types
open Feliz
open Feliz.Bulma

let private renderIconFromClass (iconClass : string) (colorOpt :string option) =
    Bulma.icon [
        helpers.isHiddenTouch
        prop.className "icon-no-margin-left"


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

let private navbarItemIsFromSection (item : NavbarLink) (pageSection : string) =
    match item.Section with
    | Some itemSection ->
        itemSection = pageSection

    | None ->
        false

let private renderNavbarContainer container (pageSection : string) (items : NavbarLink list) =
    container [
        for item in items do

            match item with
            // Label only
            | { Label = Some label; Icon = None } ->
                Bulma.navbarItem.a [
                    prop.href item.Url
                    prop.text label
                    if navbarItemIsFromSection item pageSection then
                        navbarItem.isActive

                    match item.IconColor with
                    | Some color ->
                        prop.style [
                            style.custom ("color", color)
                        ]

                    | None ->
                        ()
                ]

            // Label and icon
            | { Label = Some label; Icon = Some icon } ->
                Bulma.navbarItem.a [
                    prop.href item.Url
                    if navbarItemIsFromSection item pageSection then
                        navbarItem.isActive

                    prop.children [
                        renderIconFromClass icon item.IconColor
                        Html.span [
                            helpers.isHiddenDesktop
                            prop.text label
                        ]
                    ]
                ]

            // Icon only
            | { Label = None; Icon = Some icon } ->
                Bulma.navbarItem.a [
                    prop.href item.Url
                    helpers.isHiddenMobile

                    if navbarItemIsFromSection item pageSection then
                        navbarItem.isActive

                    prop.children [
                        renderIconFromClass icon item.IconColor
                    ]
                ]

            | _ ->
                printf $"""%A{item} is not a valid NavbarLink.

A NavbarLink either have:
- A label and no icon
- A label and an icon
- No label and an icon
"""

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

                    Bulma.navbarBurger [
                        prop.custom ("data-target", "nav-menu")
                        prop.className "navbar-burger-dots"
                        helpers.isHiddenDesktop
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
                ]


                match config.Navbar with
                | Some navbarConfig ->
                    Bulma.navbarMenu [
                        prop.id "nav-menu"

                        prop.children [
                            renderNavbarContainer Bulma.navbarStart.div pageSection navbarConfig.Start
                            renderNavbarContainer Bulma.navbarEnd.div pageSection navbarConfig.End
                        ]
                    ]

                | None ->
                    ()
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
        prop.custom ("lang", "en")
        prop.children [
            Html.head [
                Html.title titleStr

                Html.meta [
                    prop.custom ("charSet", "utf-8")
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
            ]

            Html.body [
                navbar args.Config args.Section
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
