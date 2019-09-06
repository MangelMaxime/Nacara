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
                Column.CustomClass "full-height-scrollable-content toc-column"
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
                Button.CustomClass "is-hidden-touch"
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

let private renderMaterialLikeControls (tocContent : string option) =
    fragment [ ]
        [
            // Material like container for desktop
            Text.div
                [
                    CustomClass "material-like-container is-for-desktop"
                    Modifiers [ Modifier.IsHidden (Screen.Touch, true ) ]
                ]
                [
                    div [ Class "material-like-button scroll-to-top"]
                        [
                            Icon.icon [ Icon.Size IsMedium ]
                                [
                                    Fa.i
                                        [
                                            Fa.Solid.AngleUp
                                            Fa.Size Fa.FaLarge
                                        ]
                                        [ ]
                                ]
                        ]
                ]

            // Material like container for Touch screen
            Text.div
                [
                    CustomClass "material-like-container is-for-touch"
                    Modifiers [ Modifier.IsHidden (Screen.Desktop, true ) ]
                ]
                [
                    div [ Class "material-like-container-body" ]
                        [
                            Text.div
                                [
                                    CustomClass "material-like-button toggle-toc"
                                    Modifiers [ Modifier.IsHidden (Screen.All, tocContent.IsNone) ]
                                ]
                                [
                                    Icon.icon [ Icon.Size IsMedium ]
                                        [
                                            Fa.i
                                                [
                                                    Fa.Solid.EllipsisV
                                                    Fa.Size Fa.FaLarge
                                                ]
                                                [ ]
                                        ]
                                    Text.div
                                        [
                                            CustomClass "material-like-button-label"
                                        ]
                                        [ str "Table of content" ]
                                ]

                            div [ Class "material-like-button scroll-to-top"]
                                [
                                    Icon.icon [ Icon.Size IsMedium ]
                                        [
                                            Fa.i
                                                [
                                                    Fa.Solid.AngleUp
                                                    Fa.Size Fa.FaLarge
                                                ]
                                                [ ]
                                        ]
                                    Text.div
                                        [
                                            CustomClass "material-like-button-label"
                                        ]
                                        [ str "Scroll to top" ]
                                ]
                        ]

                    Text.div
                        [
                            CustomClass "material-like-button close-open-button"
                        ]
                        [
                            Icon.icon [ Icon.Size IsMedium ]
                                [
                                    Fa.i
                                        [
                                            Fa.Solid.Plus
                                            Fa.Size Fa.FaLarge
                                        ]
                                        [ ]
                                ]
                        ]
                ]
        ]

let rec private renderBreadcrumbItem (model : Model) (menuItem : MenuItem) =
    match menuItem with
    | MenuItem pageId ->
        [
            Breadcrumb.item [ ]
                [ a [ ]
                    [ str pageId ] ]
        ]

    | MenuList (label, items) ->
        let first =
            Breadcrumb.item [ ]
                [ a [ ]
                    [ str label ] ]

        let tail =
            items
            |> Array.map (renderBreadcrumbItem model)
            |> Array.toList
            |> List.concat

        first :: tail

let private renderBreadcrumb (model : Model) (pageContext : PageContext) =
    let getTitle pageId =
        match Map.tryFind pageId model.DocFiles with
        | Some pageContext ->
            pageContext.Attributes.Title

        | None ->
            Log.error "Unable to find the file: %s" pageId
            sprintf "Page `%s` not found" pageId

    let rec findPathForActiveMenu (searchedId : string) (acc : string list) (menuConfig : MenuItem list) =
        match menuConfig with
        | head::tail ->
            match head with
            | MenuItem pageId ->
                if searchedId = pageId then
                    Some (acc @ [ getTitle pageId ] )
                else
                    findPathForActiveMenu searchedId acc tail
            | MenuList (categoryTitle, items) ->
                let items =
                    items |> Array.toList

                match findPathForActiveMenu searchedId (acc @ [ categoryTitle ]) items with
                | Some res ->
                    Some res

                | None ->
                    findPathForActiveMenu searchedId acc tail

        | [] ->
            None

    let path =
        match model.Config.MenuConfig with
        | Some menuConfig ->
            let pageId = getFileId model.Config.Source pageContext

            menuConfig
            |> List.map ( fun (category, items) ->
                let items =
                        items |> Array.toList

                findPathForActiveMenu pageId [ category ] items
            )
            |> List.filter Option.isSome
            |> List.tryHead
            |> function
            | Some x -> x
            | None -> None

        | None ->
            None

    match path with
    | Some path ->
        path
        |> List.map (fun id ->
            // Make the items active so they aren't styled as clickable
            Breadcrumb.item [ Breadcrumb.Item.IsActive true ]
                [ a [ ]
                    [ str id ] ]
        )
        |> (fun items ->
            let breadcrumbButton =
                Breadcrumb.item [ ]
                    [
                        a [ Class "menu-trigger" ]
                            [
                                Icon.icon
                                    [ ]
                                    [
                                        Fa.i
                                            [
                                                Fa.Solid.Bars
                                            ]
                                            [ ]
                                    ]
                            ]
                    ]

            // Hide the edit button on mobile
            div [ Class "mobile-menu is-hidden-desktop" ]
                [
                    Breadcrumb.breadcrumb [ ]
                        [
                            yield breadcrumbButton
                            yield! items
                        ]
                ]
        )
        |> Some

    | None ->
        None


let private renderPage (menu : ReactElement option) (breadcrumb : ReactElement option) (editButton : ReactElement) (tocContent : string option) (title : string) (pageContent : string) =
    div [ ]
        [
            ofOption breadcrumb
            Columns.columns
                [
                    Columns.IsGapless
                    Columns.IsMobile
                ]
                [
                    Column.column
                        [
                            Column.Width (Screen.Desktop, Column.Is2)
                            Column.CustomClass "is-menu-column"
                            Column.Modifiers
                                [
                                    Modifier.IsHidden (Screen.Touch, true)
                                ]
                            Column.Props
                                [
                                    Style [ PaddingLeft "1.5rem !important"]
                                ]
                        ]
                        [ ofOption menu ]

                    Column.column
                        [
                            // Even if no TOC is found, we keep the main content size constant
                            // Otherwise it feels "too wild" and also make a strange impression
                            // when switching from one page to another
                            Column.Width (Screen.Desktop, Column.Is8)
                            Column.Width (Screen.Touch, Column.IsFull)
                            Column.CustomClass "full-height-scrollable-content toc-scrollable-container is-main-content"
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
                            div [ Class "navigation-container" ]
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

                                            Text.span
                                                [
                                                    CustomClass "text"
                                                    Modifiers [ Modifier.TextTransform TextTransform.UpperCase ]
                                                ]
                                                [ str "Previous" ]
                                        ]

                                    Button.button
                                        [
                                            Button.CustomClass "navigate-to-next"
                                            Button.Color IsPrimary
                                            Button.IsOutlined
                                        ]
                                        [
                                            Text.span
                                                [
                                                    CustomClass "text"
                                                    Modifiers [ Modifier.TextTransform TextTransform.UpperCase ]
                                                ]
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

                    renderTocContent tocContent
                ]

            renderMaterialLikeControls tocContent

        ]

let rec private renderMenuItem (model : Model) (menuItem : MenuItem) =
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
                | MenuList (label, x) ->

                    renderMenuItem model (MenuList (label, x))
                    // Log.warn "Nacara only support only 2 level deep menus. The following menu is too deep:\n%A" label
                    // nothing
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
        |> fun content ->
            div [ Class "menu-container full-height-scrollable-content" ]
                [
                    Menu.menu [ Props [ Style [ MarginTop "3.25rem" ] ] ]
                        content
                ]
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

let addScrollToTop (pageContent : ReactElement) =
    let sourceCode =
        Directory.join Node.Api.__dirname "${entryDir}/scripts/scroll-to-top.js"
        |> File.readSync

    fragment [ ]
        [
            pageContent
            script [ Type "text/javascript"
                     DangerouslySetInnerHTML { __html = sourceCode } ]
                [ ]
        ]


let toHtml (model : Model) (pageContext : PageContext) =
    promise {
        let! (html, tocContent) =
            pageContext
            |> PageContext.processMarkdown model
            |> Promise.bind Prelude.processTableOfContent

        return
            renderPage
                (generateMenu model pageContext)
                (renderBreadcrumb model pageContext)
                (renderEditButton model.Config pageContext)
                tocContent
                pageContext.Attributes.Title
                html
            |> addJavaScriptConfig model pageContext
            |> addTocScript tocContent
            |> addScrollToTop
            |> Prelude.basePage model pageContext.Attributes.Title
    }
