module Prelude

open Fable.React
open Fable.React.Props
open Fulma
open Fable.FontAwesome
open Types
open Fable.Core
open System.Text.RegularExpressions

let processTableOfContent (pageContext : PageContext) =
    promise {
        let tocRegex = new Regex("""(<nav class="toc-container">.*<\/nav>)""")

        let tocContent =
            let result = tocRegex.Match(pageContext.Content)

            if result.Success then
                Some result.Value
            else
                None

        let pageContent =
            tocRegex.Replace(pageContext.Content, "")

        return pageContent , tocContent
    }

type LinkProps =
    {
        Icon : ReactElement
        Label : ReactElement
        Href : string
        Visibility : Modifier.IModifier list
        IsExternal : bool
        Color : string option
    }

let private genericRenderLink (props : LinkProps) =
    let color = Option.defaultValue null props.Color
    let target =
        if props.IsExternal then
            "_blank"
        else
            "_self"

    Navbar.Item.a
        [
            Navbar.Item.Props
                [
                    Href props.Href
                    Target target
                    Style [ Color color ]
                ]
            Navbar.Item.Modifiers props.Visibility
        ]
        [
            props.Icon
            props.Label
        ]

let private renderLinkIcon (iconClass : string) =
    Icon.icon [ ]
        [ Fa.i [ Fa.Icon iconClass
                 Fa.Size Fa.FaLarge ]
            [ ] ]

let private renderLink (link : LinkType) =

    match link with
    | IconOnly link ->
        genericRenderLink
            {
                Icon = renderLinkIcon link.Icon
                Label = nothing
                Href = link.Href
                Visibility =
                    [
                        Modifier.IsHidden (Screen.Desktop, false)
                        Modifier.IsHidden (Screen.Touch, true)
                    ]
                IsExternal = link.IsExternal
                Color = link.Color
            }

    | TextOnly link ->
        genericRenderLink
            {
                Icon = nothing
                Label =
                    span [ ]
                        [ str link.Label ]
                Href = link.Href
                Visibility =
                    [
                        // Modifier.IsHidden (Screen.Desktop, false)
                        // Modifier.IsHidden (Screen.Touch, true)
                    ]
                IsExternal = link.IsExternal
                Color = link.Color
            }

    | IconAndText link ->
        genericRenderLink
            {
                Icon = renderLinkIcon link.Icon
                Label =
                    span [ ]
                        [ str link.Label ]
                Href = link.Href
                Visibility =
                    [
                        // Modifier.IsHidden (Screen.Desktop, true)
                        // Modifier.IsHidden (Screen.Touch, false)
                    ]
                IsExternal = link.IsExternal
                Color = link.Color
            }


let private renderIconOnlyForMobile (links : LinkType list) =
    links
    |> List.map (fun link ->
        match link with
        | IconOnly link ->
            genericRenderLink
                {
                    Icon = renderLinkIcon link.Icon
                    Label = nothing
                    Href = link.Href
                    Visibility =
                        [
                            Modifier.IsHidden (Screen.Desktop, true)
                            Modifier.IsHidden (Screen.Touch, false)
                        ]
                    IsExternal = link.IsExternal
                    Color = link.Color
                }
        | TextOnly _
        | IconAndText _ ->
            nothing
    )

let private navbarItems (config : Config) =
    match config.Navbar with
    | Some navbarConfig ->
        let versionItem =
            match navbarConfig.ShowVersion with
            | true ->
                Navbar.Item.a [ ]
                    [ str config.Version ]
            | false -> nothing

        let linksItem =
            navbarConfig.Links
            |> List.map renderLink

        let startItems =
            Navbar.Start.div [ ]
                [ versionItem ]

        let endItems =
            Navbar.End.div [ ]
                linksItem

        fragment [ ]
            [ startItems
              endItems ]

    | None -> nothing

let private navbar (config : Config) =
    Navbar.navbar [ Navbar.IsFixedTop ]
        [
            Container.container [ ]
                [
                    Navbar.Brand.div [ ]
                        [
                            Navbar.Item.div [ Navbar.Item.CustomClass "title is-4" ]
                                [ str config.Title ]

                            config.Navbar
                            |> Option.bind (fun navbarConfig ->
                                renderIconOnlyForMobile navbarConfig.Links
                                |> fragment [ ]
                                |> Some
                            )
                            |> Option.defaultValue nothing

                            Navbar.burger [ Props [ Data ("target", "nav-menu" ) ] ]
                                [
                                    span [ ] [ ]
                                    span [ ] [ ]
                                    span [ ] [ ]
                                ]
                        ]

                    Navbar.menu [ Navbar.Menu.Props [ Id "nav-menu" ] ]
                        [ navbarItems config ]
                ]
        ]

let basePage (model : Model) (pageTitle : string) (content : ReactElement) =
    let titleStr = pageTitle + " · " + model.Config.Title

    let toUrl (url : string) =
        model.Config.BaseUrl + url

    let menuScript =
        let sourceCode =
            Directory.join Node.Api.__dirname "${entryDir}/scripts/menu.js"
            |> File.readSync

        script [ Type "text/javascript"
                 DangerouslySetInnerHTML { __html = sourceCode } ]
            [ ]


    html [ Class "has-navbar-fixed-top" ]
        [
            head [ ]
                [
                    title [ ]
                        [ str titleStr ]
                    meta
                        [
                            HttpEquiv "Content-Type"
                            HTMLAttr.Content "text/html; charset=utf-8"
                        ]
                    meta
                        [
                            Name "viewport"
                            HTMLAttr.Content "width=device-width, initial-scale=1"
                        ]
                    link
                        [
                            Rel "stylesheet"
                            Type "text/css"
                            Href (toUrl "style.css")
                        ]
                    link
                        [
                            Rel "stylesheet"
                            Href "https://use.fontawesome.com/releases/v5.7.2/css/all.css"
                            Integrity "sha384-fnmOCqbTlWIlj8LyTjo7mOUStjsKC4pOpQbqyi7RrhN7udi9RwhKkMHpvLbHG9Sr"
                            CrossOrigin "anonymous"
                        ]
                    script [ Src "https://polyfill.app/api/polyfill?features=scroll-behavior" ]
                        [ ]
                    script
                        [
                            Type "text/javascript"
                            DangerouslySetInnerHTML
                                {
                                __html = sprintf
                                    """
var nacara = {};
                                    """
                                }
                        ]
                        [ ]
                    menuScript
                ]
            body [ ]
                [
                    navbar model.Config
                    Container.container [ ]
                        [ content ]
                ]
        ]
