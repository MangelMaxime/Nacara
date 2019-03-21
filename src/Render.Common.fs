module Render.Common

open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fulma
open Types
open Fable.FontAwesome

let private renderLink (link : Link) =
    let color = Option.defaultValue null link.Color
    let target =
        if link.IsExternal then
            "_blank"
        else
            "_self"

    let iconItem =
        match link.Icon with
        | Some iconClass ->
            Icon.icon [ ]
                [ Fa.i [ Fa.Icon iconClass
                         Fa.Size Fa.FaLarge ]
                    [ ] ]
        | None -> nothing

    let labelItem =
        match link.Label with
        | Some labelText ->
            span [ ]
                [ str labelText ]
        | None -> nothing

    Navbar.Item.a [ Navbar.Item.Props [ Href link.Href
                                        Target target
                                        Style [ Color color ] ] ]
        [ iconItem
          labelItem ]

let private navbarItems (config : Config) =
    match config.Navbar with
    | Some navbarConfig ->
        let versionItem =
            match navbarConfig.ShowVersion with
            | true ->
                Navbar.Item.a [ ]
                    [ str config.Version ]
            | false -> nothing

        let docItem =
            match navbarConfig.Doc with
            | Some doc ->
                Navbar.Item.a [ Navbar.Item.Props [ Href doc ] ]
                    [ Icon.icon [ ]
                        [ Fa.i [ Fa.Solid.Book
                                 Fa.Size Fa.FaLarge ]
                            [ ] ]
                      span [ ]
                        [ str "Documentation" ] ]
            | None -> nothing

        let linksItem =
            navbarConfig.Links
            |> List.map renderLink

        let startItems =
            Navbar.Start.div [ ]
                [ versionItem ]

        let endItems =
            Navbar.End.div [ ]
                (docItem::linksItem)

        fragment [ ]
            [ startItems
              endItems ]

    | None -> nothing

let private navbar (config : Config) =
    Navbar.navbar [ Navbar.IsFixedTop
                    Navbar.Color IsPrimary ]
        [ Container.container [ ]
            [ Navbar.Brand.div [ ]
                [ Navbar.Item.div [ Navbar.Item.CustomClass "title is-4" ]
                    [ str config.Title ] ]
              navbarItems config ] ]

let basePage (model : Model) (content : React.ReactElement) =
    let siteTitle = ""
    let pageTitle = ""
    let titleStr = pageTitle + " · " +  siteTitle

    html [ Class "has-navbar-fixed-top" ]
        [ head [ ]
            [ title [ ]
                [ str titleStr ]
              link [ Rel "stylesheet"
                     Type "text/css"
                     Href "style.css" ]
              link [ Rel "stylesheet"
                     Href "https://use.fontawesome.com/releases/v5.7.2/css/all.css"
                     Integrity "sha384-fnmOCqbTlWIlj8LyTjo7mOUStjsKC4pOpQbqyi7RrhN7udi9RwhKkMHpvLbHG9Sr"
                     CrossOrigin "anonymous" ]
              script [ Src "https://polyfill.app/api/polyfill?features=scroll-behavior" ]
                [ ] ]
          body [ ]
            [ navbar model.Config
              Columns.columns [ Columns.IsGapless
                                Columns.CustomClass "page-content" ]
                [ Column.column [ Column.Width (Screen.All, Column.Is2)
                                  Column.CustomClass "sidebar" ]
                    [ ]
                  Column.column [ Column.CustomClass "main-content"
                                  // We need to set ScrollBehavior as inline style
                                  // for the polyfill to detect it and apply
                                  // Needed for IE11 + Safari for example
                                  Column.Props [ Style [ ScrollBehavior "smooth" ] ] ]
                    [ content ] ] ] ]
