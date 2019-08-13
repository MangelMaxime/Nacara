module Render.Common

open Fable.React
open Fable.React.Props
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
    Navbar.navbar [ Navbar.IsFixedTop ]
        [ Container.container [ ]
            [ Navbar.Brand.div [ ]
                [ Navbar.Item.div [ Navbar.Item.CustomClass "title is-4"
                                    // TODO: This should go in the CSS, but I had some trouble
                                    Navbar.Item.Props [ Style [ MarginBottom "0" ] ] ]
                    [ str config.Title ]
                  Navbar.burger
                        [ GenericOption.Props [ Data("target", "navMenu") ] ]
                        [ span [ ] [ ]
                          span [ ] [ ]
                          span [ ] [ ] ] ]
              Navbar.menu [ Navbar.Menu.Props [Id "navMenu"] ]
                [ navbarItems config ]
            ] ]

let basePage (model : Model) (pageTitle : string) (content : ReactElement) =
    let titleStr = pageTitle + " · " + model.Config.Title

    let toUrl (url : string) =
        model.Config.BaseUrl + url

    let menuScript =
        let sourceCode =
            Directory.join Node.Api.__dirname "../scripts/menu.js"
            |> File.readSync

        script [ Type "text/javascript"
                 DangerouslySetInnerHTML { __html = sourceCode } ]
            [ ]


    html [ Class "has-navbar-fixed-top" ]
        [ head [ ]
            [ title [ ]
                [ str titleStr ]
              meta [ HttpEquiv "Content-Type"
                     HTMLAttr.Content "text/html; charset=utf-8" ]
              meta [ Name "viewport"
                     HTMLAttr.Content "width=device-width, initial-scale=1" ]
              link [ Rel "stylesheet"
                     Type "text/css"
                     Href (toUrl "style.css") ]
              link [ Rel "stylesheet"
                     Href "https://use.fontawesome.com/releases/v5.7.2/css/all.css"
                     Integrity "sha384-fnmOCqbTlWIlj8LyTjo7mOUStjsKC4pOpQbqyi7RrhN7udi9RwhKkMHpvLbHG9Sr"
                     CrossOrigin "anonymous" ]
              script [ Src "https://polyfill.app/api/polyfill?features=scroll-behavior" ]
                [ ]
              script [ Type "text/javascript"
                       DangerouslySetInnerHTML { __html = sprintf
                """
var nacara = {};
                """ } ]
                    [ ]
              menuScript ]
          body [ ]
            [ navbar model.Config
              Container.container [ ]
                [ content ] ] ]
