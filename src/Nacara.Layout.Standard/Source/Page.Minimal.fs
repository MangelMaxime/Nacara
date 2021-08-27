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
                        Html.span label
                    ]
                ]

            // Icon only
            | { Label = None; Icon = Some icon } ->
                Bulma.navbarItem.a [
                    prop.href item.Url
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

let private renderMobileNavbarItems (pageSection : string) (navbarConfig : NavbarConfig) =

    let sectionLabelOpt =
        navbarConfig.Start @ navbarConfig.End
        |> List.tryFind (fun navbarLink ->
            match navbarLink.Section with
            | Some section ->
                section = pageSection

            | None ->
                false
        )
        |> Option.map (fun navbarLink ->
            navbarLink.Label
        )

    [
        // If we found a navbar link with the name of the page section
        // Display it in the navbar for mobile render
        // So people know where they are in the webiste
        match sectionLabelOpt with
        | Some (Some sectionLabel) ->
            Bulma.navbarItem.a [
                helpers.isHiddenDesktop
                navbarItem.isActive
                prop.text sectionLabel
            ]

        | Some None
        | None ->
            null

        for item in navbarConfig.End do

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

let private navbar (config : Config) (pageSection : string) =
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
                        yield! renderMobileNavbarItems pageSection navbarConfig

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
