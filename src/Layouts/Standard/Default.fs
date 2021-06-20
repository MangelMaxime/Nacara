namespace Layout.Standard
open System.Text.RegularExpressions

module Default =

    open System
    open Fable.React
    open Fable.React.Props
    open Fulma
    open Fable.FontAwesome
    open Types
    open Feliz
    open Feliz.Bulma
    open Fable.Core

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
            let pageContext = model.DocFiles.get pageId

            if box pageContext <> null then
                pageContext.Attributes.Title

            else
                Log.error "Unable to find the fil2e: %s" pageId
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
                                    span [ ] [ ]
                                    span [ ] [ ]
                                    span [ ] [ ]
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


    let private renderPage (model : Model) (pageContext : PageContext) (menu : ReactElement option) (breadcrumb : ReactElement option) (editButton : ReactElement) (tocContent : string option) (title : string) (pageContent : string) =
        let menuList =
            match model.Config.MenuConfig with
            | Some menuConfig ->
                menuConfig
                |> List.collect (fun (category, menuItems) ->
                    let rec menuItemToList (menuItems : MenuItem array) =
                        menuItems
                        |> Array.toList
                        |> List.collect (fun menuItem ->
                            match menuItem with
                            | MenuItem pageId ->
                                [ pageId ]

                            | MenuList (label, items) ->
                                menuItemToList items
                        )

                    (menuItemToList menuItems)
                )

            | None ->
                [ ]

        let currentPageId =
            getFileId model.Config.Source pageContext

        let currentPageIndexInTheMenu =
            menuList
            |> List.tryFindIndex (fun pageId ->
                pageId = currentPageId
            )
            |> function
            | Some pageIndex ->
                pageIndex
            | None ->
                failwithf "Page %s not found in the menu" currentPageId

        let previousButton =
            let emptyPreviousButton =
                // Empty button to keep the layout working
                // It ensure that the "Next button" is on the right of the page
                Bulma.button.button [
                    prop.className "navigate-to-previous is-invisible"
                ]

            if currentPageIndexInTheMenu >= 1 then
                let previousPageFieldId = menuList.[currentPageIndexInTheMenu - 1]
                let previousPageContext = model.DocFiles.get previousPageFieldId

                if isNull (box previousPageContext) then
                    failwithf "Page %s not found in the list of pages" previousPageFieldId
                else
                    if previousPageContext.Attributes.ExcludeFromNavigation then
                        emptyPreviousButton
                    else

                        Bulma.button.a [
                            prop.className "navigate-to-previous"
                            color.isPrimary
                            button.isOutlined
                            prop.href (generateUrl model.Config previousPageContext)

                            prop.children [
                                Bulma.icon [
                                    Fa.i
                                        [
                                            Fa.Solid.ArrowLeft
                                        ]
                                        [ ]
                                ]

                                Bulma.text.span [
                                    text.isUppercase

                                    prop.text previousPageContext.Attributes.Title
                                ]
                            ]
                        ]

            else
                emptyPreviousButton

        let nextButton =
            let emptyNextButton =
                // Empty button to keep the layout working
                Bulma.button.button [
                    prop.className "navigate-to-next is-invisible"
                ]

            if currentPageIndexInTheMenu < menuList.Length - 1 then
                let nextPageFieldId = menuList.[currentPageIndexInTheMenu + 1]
                let nextPageContext = model.DocFiles.get nextPageFieldId

                if isNull (box nextPageContext) then
                    failwithf "Page %s not found in the list of pages" nextPageFieldId
                else
                    if nextPageContext.Attributes.ExcludeFromNavigation then
                        emptyNextButton
                    else
                        Bulma.button.a [
                            prop.className "navigate-to-next"
                            color.isPrimary
                            button.isOutlined
                            prop.href (generateUrl model.Config nextPageContext)

                            prop.children [
                                Bulma.text.span [
                                    text.isUppercase

                                    prop.text nextPageContext.Attributes.Title
                                ]

                                Bulma.icon [
                                    Fa.i
                                        [
                                            Fa.Solid.ArrowRight
                                        ]
                                        [ ]
                                ]
                            ]
                        ]
            else
                emptyNextButton

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
                                Column.Width (Screen.Desktop, Column.Is3)
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
                                Column.Width (Screen.Desktop, Column.Is8)
                                Column.Width (Screen.Touch, Column.IsFull)
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

                                        previousButton


                                        // Button.button
                                        //     [
                                        //         Button.CustomClass "navigate-to-previous"
                                        //         Button.Color IsPrimary
                                        //         Button.IsOutlined
                                        //     ]
                                        //     [
                                        //         Icon.icon [ ]
                                        //             [
                                        //                 Fa.i
                                        //                     [
                                        //                         Fa.Solid.ArrowLeft
                                        //                     ]
                                        //                     [ ]
                                        //             ]

                                        //         Text.span
                                        //             [
                                        //                 CustomClass "text"
                                        //                 Modifiers [ Modifier.TextTransform TextTransform.UpperCase ]
                                        //             ]
                                        //             [ str "Previous" ]
                                        //     ]

                                        nextButton

                                        // Button.button
                                        //     [
                                        //         Button.CustomClass "navigate-to-next"
                                        //         Button.Color IsPrimary
                                        //         Button.IsOutlined
                                        //     ]
                                        //     [
                                        //         Text.span
                                        //             [
                                        //                 CustomClass "text"
                                        //                 Modifiers [ Modifier.TextTransform TextTransform.UpperCase ]
                                        //             ]
                                        //             [ str "Next" ]

                                        //         Icon.icon [ ]
                                        //             [
                                        //                 Fa.i
                                        //                     [
                                        //                         Fa.Solid.ArrowRight
                                        //                     ]
                                        //                     [ ]
                                        //             ]
                                        //     ]
                                    ]
                            ]
                    ]

            ]

    let renderTopLevelToc (section : TableOfContentParser.Section) =
        Html.li [
            Html.a [
                prop.dangerouslySetInnerHTML section.Header.Title
                prop.href section.Header.Link
                prop.custom("data-toc-element", true)
            ]

            if not section.SubSections.IsEmpty then

                Html.ul [
                    prop.className "table-of-content"

                    prop.children [
                        for subSection in section.SubSections do
                            Html.li [
                                Html.a [
                                    prop.dangerouslySetInnerHTML subSection.Title
                                    prop.href subSection.Link
                                    prop.custom("data-toc-element", true)
                                ]
                            ]
                    ]
                ]
        ]

    let renderMenuItem (model : Model) (pageContext : PageContext) (tableOfContent : TableOfContentParser.TableOfContent) pageId =

        let pageInfo = model.DocFiles.get pageId

        let currentPageId = getFileId model.Config.Source pageContext

        let isCurrentPage = (currentPageId = pageId)

        let hasTableOfContent =
            isCurrentPage && not tableOfContent.IsEmpty

        let tableOfContent =
            if hasTableOfContent then
                Html.ul [
                    prop.className "table-of-content"

                    prop.children [
                        for tocElement in tableOfContent do
                            renderTopLevelToc tocElement
                    ]
                ]

            else
                nothing

        if box pageInfo <> null then
            Html.li [
                if hasTableOfContent then
                    prop.className "has-table-of-content"

                prop.children [
                    Menu.Item.a [ Menu.Item.Props [ Data("menu-id", pageId) ]
                                  generateUrl model.Config pageInfo
                                  |> Menu.Item.Href
                                  Menu.Item.IsActive isCurrentPage ]
                        [
                            str pageInfo.Attributes.Title
                        ]

                    tableOfContent
                ]
            ]

        else
            Log.error "Unable to find the file33: %s" pageId
            nothing

    let rec private renderMenu (model : Model) (pageContext : PageContext) (tableOfContent : TableOfContentParser.TableOfContent) (menuItem : MenuItem) =
        match menuItem with
        | MenuItem pageId ->
            renderMenuItem model pageContext tableOfContent pageId

        | MenuList (label, items) ->
            let subMenu =
                items
                |> Array.map (function
                    | MenuItem pageId ->
                        renderMenuItem model pageContext tableOfContent pageId

                    | MenuList (label, x) ->

                        renderMenu model pageContext tableOfContent (MenuList (label, x))
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

    let private generateMenu (model : Model) (pageContext : PageContext) (tableOfContent : TableOfContentParser.TableOfContent) =
        match model.Config.MenuConfig with
        | Some menuConfig ->
            menuConfig
            |> List.map (fun (category, menuItems) ->
                fragment [ ]
                    [
                        Menu.label [ ]
                            [ str category ]
                        menuItems
                        |> Array.map (renderMenu model pageContext tableOfContent)
                        |> Array.toList
                        |> Menu.list [ ]
                    ]

            )
            |> fun content ->
                div [ Class "menu-container" ]
                    [
                        Menu.menu [ ]
                            content
                    ]
            |> Some

        | None ->
            None

    let private addMenuScript (content : ReactElement) =
        fragment [ ]
            [
                content

                Html.script [
                    prop.src "/static/nacara-standard/menu.js"
                ]
            ]



    let toHtml (model : Model) (pageContext : PageContext) =
        promise {
            let! pageContext =
                pageContext
                |> PageContext.processMarkdown model
                // |> Promise.bind Prelude.processTableOfContent

            let tocInformation =
                TableOfContentParser.parse pageContext

            return
                renderPage model pageContext
                    (generateMenu model pageContext tocInformation)
                    (renderBreadcrumb model pageContext)
                    (renderEditButton model.Config pageContext)
                    None
                    pageContext.Attributes.Title
                    pageContext.Content
                |> addMenuScript
                |> Prelude.basePage model pageContext.Attributes.Title
        }
