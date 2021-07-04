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
    open Thoth.Json

    type Attributes =
        {
            Title : string
            Layout : string
            ExcludeFromNavigation : bool
            Menu : bool
            ShowTitle : bool
            ShowEditButton : bool
            ShowToc : bool
            Id : string option
        }

        static member Decoder =
            Decode.object (fun get ->
                {
                    Title = get.Required.Field "title" Decode.string
                    ExcludeFromNavigation = get.Optional.Field "excludeFromNavigation" Decode.bool
                                                |> Option.defaultValue false
                    Layout = get.Optional.Field "layout" Decode.string
                                |> Option.defaultValue "default"
                    Menu = get.Optional.Field "menu" Decode.bool
                                |> Option.defaultValue true
                    ShowTitle = get.Optional.Field "showTitle" Decode.bool
                                |> Option.defaultValue true
                    ShowEditButton = get.Optional.Field "showEditButton" Decode.bool
                                |> Option.defaultValue true
                    ShowToc = get.Optional.Field "showToc" Decode.bool
                                |> Option.defaultValue true
                    Id = get.Optional.Field "id" Decode.string
                }
            )

    let private renderEditButton (config : Config) (pageAttributes : Attributes) (pageContext : PageContext) =
        if pageAttributes.ShowEditButton then
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
        else
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

    let private renderBreadcrumb (model : Model) (pageAttributes : Attributes) (pageContext : PageContext) =
        let getTitle pageId =
            let pageContext = model.DocFiles.get pageId

            if box pageContext <> null then
                pageAttributes.Title

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

    let private renderNavigationButtons  (model : Model) (pageContext : PageContext)  =
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

        match currentPageIndexInTheMenu with
        | None ->
            nothing

        | Some currentPageIndexInTheMenu ->

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
                        match Decode.fromValue "$" Attributes.Decoder previousPageContext.FrontMatter with
                        | Ok previousPageAttributes ->
                            if previousPageAttributes.ExcludeFromNavigation then
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

                                            prop.text previousPageAttributes.Title
                                        ]
                                    ]
                                ]

                        | Error errorMessage ->
                            failwith $"Page {previousPageContext.Path} has invalid attributes information. Error:\n{errorMessage}"

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
                        match Decode.fromValue "$" Attributes.Decoder nextPageContext.FrontMatter with
                        | Ok nextPageAttributes ->
                            if nextPageAttributes.ExcludeFromNavigation then
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

                                            prop.text nextPageAttributes.Title
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
                        | Error errorMessage ->
                            failwith $"Page {nextPageContext.Path} has invalid attributes information. Error:\n{errorMessage}"
                else
                    emptyNextButton

            div [ Class "navigation-container" ]
                [

                    previousButton
                    nextButton
                ]

    let private renderMainContent (model : Model) (pageAttributes : Attributes) (pageContext : PageContext) (editButton : ReactElement) (title : string) (pageContent : string) =
        let pageTitle =
            if pageAttributes.ShowTitle then
                Html.h1 title
            else
                nothing

        React.fragment [
            Bulma.section [
                Bulma.content [
                    Html.header [
                        prop.className "page-header"
                        prop.children [
                            editButton
                            pageTitle
                        ]
                    ]

                    Html.div [
                        prop.dangerouslySetInnerHTML pageContent
                    ]
                ]
            ]

            renderNavigationButtons model pageContext
        ]

    let private renderPageWithMenu (model : Model) (pageAttributes : Attributes) (pageContext : PageContext) (menu : ReactElement) (breadcrumb : ReactElement option) (editButton : ReactElement) (title : string) (pageContent : string) =

        let pageTitle =
            if pageAttributes.ShowTitle then
                Html.h1 title
            else
                nothing

        Bulma.container [
            ofOption breadcrumb

            Bulma.columns [
                columns.isGapless
                columns.isMobile

                prop.children [
                    Bulma.column [
                        prop.className "is-menu-column"
                        column.is3Desktop
                        helpers.isHiddenTouch
                        spacing.ml5

                        prop.children [
                            menu
                        ]
                    ]


                    Bulma.column [
                        column.is8Desktop
                        column.isFullTouch

                        prop.children [
                            renderMainContent model pageAttributes pageContext editButton title pageContent
                        ]
                    ]
                ]
            ]
        ]


    let private renderPageWithoutMenu (model : Model) (pageAttributes : Attributes) (pageContext : PageContext) (breadcrumb : ReactElement option) (editButton : ReactElement) (title : string) (pageContent : string) =
        Bulma.container [
            ofOption breadcrumb

            Bulma.columns [
                prop.children [
                    Bulma.column [
                        column.is8Desktop
                        column.isOffset2Desktop

                        prop.children [
                            renderMainContent model pageAttributes pageContext editButton title pageContent
                        ]
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

    let renderMenuItem (model : Model) (pageAttributes : Attributes) (pageContext : PageContext) (tableOfContent : TableOfContentParser.TableOfContent) pageId =

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
                            str pageAttributes.Title
                        ]

                    tableOfContent
                ]
            ]

        else
            Log.error "Unable to find the file33: %s" pageId
            nothing

    let rec private renderMenu (model : Model) (pageAttributes : Attributes) (pageContext : PageContext) (tableOfContent : TableOfContentParser.TableOfContent) (menuItem : MenuItem) =
        match menuItem with
        | MenuItem pageId ->
            renderMenuItem model pageAttributes pageContext tableOfContent pageId

        | MenuList (label, items) ->
            let subMenu =
                items
                |> Array.map (function
                    | MenuItem pageId ->
                        renderMenuItem model pageAttributes pageContext tableOfContent pageId

                    | MenuList (label, x) ->

                        renderMenu model pageAttributes pageContext tableOfContent (MenuList (label, x))
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

    let private generateMenu (model : Model) (pageAttributes : Attributes) (pageContext : PageContext) (tableOfContent : TableOfContentParser.TableOfContent) =
        match model.Config.MenuConfig with
        | Some menuConfig ->
            menuConfig
            |> List.map (fun (category, menuItems) ->
                fragment [ ]
                    [
                        Menu.label [ ]
                            [ str category ]
                        menuItems
                        |> Array.map (renderMenu model pageAttributes pageContext tableOfContent)
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
                    prop.src "/static/nacara_internals/menu.js"
                ]
            ]

    let private generateTocOnly (tableOfContent : TableOfContentParser.TableOfContent) =
        let hasTableOfContent =
            not tableOfContent.IsEmpty

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


    let private generateMenuOrToc (model : Model) (pageAttributes : Attributes) (pageContext : PageContext) =
        let tocInformation =
            TableOfContentParser.parse pageContext

        if pageAttributes.Menu then
            generateMenu model pageAttributes pageContext tocInformation
        else
            if pageAttributes.ShowToc then
                div [ Class "menu-container" ]
                    [
                        Bulma.menu [
                            Bulma.menuList [
                                Html.li [
                                    prop.className "has-table-of-content"

                                    prop.children [
                                        Html.a [
                                            prop.className "is-active"
                                            prop.text "Table of content"
                                        ]

                                        generateTocOnly tocInformation
                                    ]
                                ]
                            ]
                        ]
                    ]
                |> Some
            else
                None


    let toHtml (model : Model) (pageContext : PageContext) =
        promise {
            match Decode.fromValue "$" Attributes.Decoder pageContext.FrontMatter with
            | Ok pageAttributes ->
                let! pageContext =
                    pageContext
                    |> PageContext.processMarkdown model

                let menuOpt =
                    generateMenuOrToc model pageAttributes pageContext

                let page =
                    match menuOpt with
                    | Some menu ->
                        renderPageWithMenu
                            model
                            pageAttributes
                            pageContext
                            menu
                            (renderBreadcrumb model pageAttributes pageContext)
                            (renderEditButton model.Config pageAttributes pageContext)
                            pageAttributes.Title
                            pageContext.Content

                    | None ->
                        renderPageWithoutMenu
                            model
                            pageAttributes
                            pageContext
                            (renderBreadcrumb model pageAttributes pageContext)
                            (renderEditButton model.Config pageAttributes pageContext)
                            pageAttributes.Title
                            pageContext.Content

                return
                    page
                    |> addMenuScript
                    |> Prelude.basePage model (Some pageAttributes.Title)

            | Error errorMessage ->
                return failwith errorMessage
        }
