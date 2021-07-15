module Layout.Standard.Prelude

open Fable.FontAwesome
open Types
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

let private navbarItemIsFromCategory (item : NavbarLink) (pageCategory : string) =
    match item.Section with
    | Some itemCategory ->
        itemCategory = pageCategory

    | None ->
        false

let private renderNavbarContainer container (pageCategory : string) (items : NavbarLink list) =
    container [
        for item in items do

            match item with
            // Label only
            | { Label = Some label; Icon = None } ->
                Bulma.navbarItem.a [
                    prop.href item.Url
                    prop.text label
                    if navbarItemIsFromCategory item pageCategory then
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
                    if navbarItemIsFromCategory item pageCategory then
                        navbarItem.isActive

                    prop.children [
                        renderIconFromClass icon item.IconColor
                        Html.span label
                    ]
                ]

            // Icon only
            | { Label = None; Icon = Some icon } ->
                Bulma.navbarItem.a [
                    prop.href item.Url
                    if navbarItemIsFromCategory item pageCategory then
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

let private renderMobileNavbarItems (items : NavbarLink list) =
    [
        for item in items do

            match item with
            // Only render
            | { Label = None; Icon = Some icon } ->
                Bulma.navbarItem.a [
                    prop.href item.Url
                    helpers.isHiddenDesktop

                    prop.children [
                        renderIconFromClass icon item.IconColor
                    ]
                ]

            | _ ->
                ()
    ]

let private navbar (config : Config) (pageCategory : string) =
    Bulma.navbar [
        navbar.isFixedTop

        prop.children [
            Bulma.container [
                Bulma.navbarBrand.div [
                    Bulma.navbarItem.a [
                        prop.className "title is-4"
                        prop.href (config.Url + config.BaseUrl)
                        prop.text config.Title
                    ]

                    match config.Navbar with
                    | Some navbarConfig ->
                        // On mobile, only the items in the end of the navbar
                        // have a chance to be rendered
                        yield! renderMobileNavbarItems navbarConfig.End

                    | None ->
                        ()

                    Bulma.navbarBurger [
                        prop.custom ("data-target", "nav-menu")
                        prop.children [
                            Html.span [ ]
                            Html.span [ ]
                            Html.span [ ]
                        ]
                    ]
                ]

                match config.Navbar with
                | Some navbarConfig ->
                    Bulma.navbarMenu [
                        prop.id "nav-menu"

                        prop.children [
                            renderNavbarContainer Bulma.navbarStart.div pageCategory navbarConfig.Start
                            renderNavbarContainer Bulma.navbarEnd.div pageCategory navbarConfig.End
                        ]
                    ]

                | None ->
                    ()
            ]
        ]
    ]

[<NoComparison>]
type BasePageArgs =
    {
        Config : Config
        Section : string
        TitleOpt : string option
        Content : ReactElement
    }

let basePage (args : BasePageArgs) =
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

        prop.children [
            Html.head [
                Html.title titleStr

                Html.meta [
                    prop.httpEquiv.contentType
                    prop.custom ("httpEquiv", "content-type")
//                    prop.content "text/html; charset=UTF-8"
                ]

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
//                    prop.custom ("crossOrigin", "anonymous")
                ]
            ]

            Html.body [
                navbar args.Config args.Section
                args.Content
            ]
        ]

    ]
