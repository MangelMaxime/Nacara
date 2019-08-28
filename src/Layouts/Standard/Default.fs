module Default

open System
open Fable.React
open Fable.React.Props
open Fulma
open Fable.FontAwesome
open Types
open Fable.Core

let private renderTocContent (tocContent : string option) =
    match tocContent with
    | None ->
        nothing

    | Some tocContent ->
        Column.column
            [
                Column.Width (Screen.Desktop, Column.Is2)
                Column.Modifiers [ Modifier.IsHidden (Screen.Touch, true) ]
                Column.CustomClass "full-height-scrollable-content"
            ]
            [ div [ DangerouslySetInnerHTML { __html =  tocContent } ]
                [ ]
            ]

let private renderEditButton (config : Config) (pageContext : PageContext) =
    match config.EditUrl with
    | Some url ->
        let filePath =
            Directory.moveUp pageContext.Path

        Button.a
            [
                Button.IsOutlined
                Button.Color IsPrimary
                Button.Modifiers [ Modifier.IsPulledRight ]
                Button.Props
                    [
                        Target "_blank"
                        Href (url + "/" + filePath)
                    ]
            ]
            [ str "Edit" ]

    | None ->
        nothing


let private renderPage (menu : ReactElement option) (editButton : ReactElement) (tocContent : string option) (title : string) (pageContent : string) =
    div [ ]
        [
            Columns.columns
                [
                    Columns.IsGapless
                    Columns.IsMobile
                ]
                [
                    Column.column
                        [
                            Column.Width (Screen.Desktop, Column.Is2)
                            Column.Width (Screen.Touch, Column.Is3)
                            Column.CustomClass "full-height-scrollable-content"
                            Column.Modifiers
                                [
                                    Modifier.IsHidden (Screen.Mobile, true)
                                ]
                        ]
                        [ ofOption menu ]

                    Column.column
                        [
                            // Even if no TOC is found, we keep the main content size constant
                            // Otherwise it feels "too wild" and also make a strange impression
                            // when switching from one page to another
                            Column.Width (Screen.Desktop, Column.Is8)
                            Column.Width (Screen.Mobile, Column.IsFull)
                            Column.Width (Screen.Tablet, Column.Is9)
                            Column.CustomClass "full-height-scrollable-content toc-scrollable-container"
                            Column.Props
                                [
                                    Style
                                        [
                                            // We need to set ScrollBehavior via style so the polyfill can work
                                            ScrollBehavior "smooth"
                                            OverflowX "hidden"
                                        ]
                                ]
                        ]
                        [
                            Section.section [ ]
                                [
                                    Content.content [ ]
                                        [
                                            header [ Class "page-header" ]
                                                [
                                                    editButton
                                                    h1 [ ]
                                                        [ str title ]
                                                ]
                                            div [ DangerouslySetInnerHTML { __html =  pageContent } ] [ ]
                                        ]
                                ]
                            Columns.columns
                                [
                                    Columns.IsMobile
                                ]
                                [
                                    Column.column
                                        [
                                            Column.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ]
                                        ]
                                        [
                                            Button.button
                                                [
                                                    Button.CustomClass "navigate-to-previous"
                                                    Button.Color IsPrimary
                                                    Button.IsOutlined
                                                ]
                                                [
                                                    Icon.icon [ ]
                                                        [
                                                            Fa.i
                                                                [
                                                                    Fa.Solid.ArrowLeft
                                                                ]
                                                                [ ]
                                                        ]

                                                    span [ ]
                                                        [ str "Previous" ]
                                                ]
                                        ]
                                    Column.column [ ]
                                        [ ]
                                    Column.column
                                        [
                                            Column.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ]
                                        ]
                                        [
                                            Button.button
                                                [
                                                    Button.CustomClass "navigate-to-next"
                                                    Button.Color IsPrimary
                                                    Button.IsOutlined
                                                ]
                                                [
                                                    span [ ]
                                                        [ str "Next" ]

                                                    Icon.icon [ ]
                                                        [
                                                            Fa.i
                                                                [
                                                                    Fa.Solid.ArrowRight
                                                                ]
                                                                [ ]
                                                        ]
                                                ]
                                        ]
                                ]
                        ]

                    renderTocContent tocContent
                ]
        ]

let private renderMenuItem (model : Model) (menuItem : MenuItem) =
    match menuItem with
    | MenuItem pageId ->
        match Map.tryFind pageId model.DocFiles with
        | Some pageInfo ->
            Menu.Item.li [ Menu.Item.Props [ Data("menu-id", pageId) ]
                           generateUrl model.Config pageInfo
                           |> Menu.Item.Href ]
                [ str pageInfo.Attributes.Title ]
        | None ->
            Log.error "Unable to find the file: %s" pageId
            nothing

    | MenuList (label, items) ->
        let subMenu =
            items
            |> Array.map (function
                | MenuItem pageId ->
                    match Map.tryFind pageId model.DocFiles with
                    | Some pageInfo ->
                        Menu.Item.li [ Menu.Item.Props [ Data("menu-id", pageId) ]
                                       generateUrl model.Config pageInfo
                                       |> Menu.Item.Href ]
                            [ str pageInfo.Attributes.Title ]
                    | None ->
                        Log.error "Unable to find the file: %s" pageId
                        nothing
                | MenuList (label, _) ->
                    Log.warn "Nacara only support only 2 level deep menus. The following menu is too deep:\n%A" label
                    nothing
            )
            |> Array.toList
            |> ul [ ]

        Menu.list [ ]
            [
                li [ ]
                    [
                        a [ Class "menu-group" ]
                            [
                                span [ ]
                                    [ str label ]
                                Icon.icon [ ]
                                    [ Fa.i
                                        [
                                            Fa.Solid.AngleRight
                                            Fa.Size Fa.FaLarge
                                        ]
                                [ ] ]
                            ]
                        subMenu
                    ]
            ]

let private generateMenu (model : Model) (pageContext : PageContext) =
    match model.Config.MenuConfig with
    | Some menuConfig ->
        menuConfig
        |> List.map (fun (category, menuItems) ->
            fragment [ ]
                [
                    Menu.label [ ]
                        [ str category ]
                    menuItems
                    |> Array.map (renderMenuItem model)
                    |> Array.toList
                    |> Menu.list [ ]
                ]

        )
        |> Menu.menu [ Props [ Style [ MarginTop "3.25rem" ] ] ]
        |> Some

    | None ->
        None

let addJavaScriptConfig (model : Model) (pageContext : PageContext) (pageContent : ReactElement) =
    fragment [ ]
        [
            pageContent
            script
                [
                    Type "text/javascript"
                    DangerouslySetInnerHTML
                        {
                            __html = sprintf
                                        """
nacara.pageId = '%s';
                                        """
                                        (getFileId model.Config.Source pageContext)
                        }
                ]
                [ ]
        ]

let private addTocScript (tocContent : string option) (pageContent : ReactElement) =
    match tocContent with
    | Some _ ->
        let sourceCode =
            Directory.join Node.Api.__dirname "${entryDir}/scripts/toc.js"
            |> File.readSync

        fragment [ ]
            [
                pageContent
                script [ Type "text/javascript"
                         DangerouslySetInnerHTML { __html = sourceCode } ]
                    [ ]
            ]

    | None ->
        pageContent

let toHtml (model : Model) (pageContext : PageContext) =
    promise {
        let! (html, tocContent) =
            pageContext
            |> processMarkdown model
            |> Promise.bind Prelude.processTableOfContent

        return
            renderPage
                (generateMenu model pageContext)
                (renderEditButton model.Config pageContext)
                tocContent
                pageContext.Attributes.Title
                html
            |> addJavaScriptConfig model pageContext
            |> addTocScript tocContent
            |> Prelude.basePage model pageContext.Attributes.Title
    }
