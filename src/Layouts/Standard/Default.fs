module Layout.Standard.Default

open Types
open Thoth.Json
open Feliz
open Feliz.Bulma
open Fable.FontAwesome

let private emptyPreviousButton =
    // Empty button to keep the layout working
    // It ensure that the "Next button" is on the right of the page
    Bulma.button.button [
        prop.className "navigate-to-previous is-invisible"
    ]

let private emptyNextButton =
    // Empty button to keep the layout working
    Bulma.button.button [
        prop.className "navigate-to-next is-invisible"
    ]

[<RequireQualifiedAccess>]
type FlatMenu =
    | Link of MenuItemLink
    | Page of MenuItemPage


let getMenuLabel (pageContext : PageContext) (itemInfo : MenuItemPage) =
    match pageContext.Title, itemInfo.Label with
    | Some label, Some _
    | None, Some label
    | Some label, None  -> label
    | None, None ->
        failwith $"Missing label information for '%s{itemInfo.PageId}'. You can set it in the markdown page using 'menu_label' or directly in the menu.json via the 'label' property"


let private renderNavigationButtons
    (baseUrl : string)
    (allPages : PageContext array)
    (menuOpt : Menu option)
    (pageContext : PageContext) =

    let rec flattenMenu (menu : Menu) =
            menu
            |> List.collect (fun menuItem ->
                match menuItem with
                | MenuItem.Page _
                | MenuItem.Link _ -> [ menuItem ]
                | MenuItem.List info ->
                    flattenMenu info.Items
            )

    match menuOpt with
    // No menu so can't generate the navigation elements
    | None ->
        null

    | Some menu ->
        let flatMenu =
            flattenMenu menu
            |> List.map (fun menuItem ->
                match menuItem with
                | MenuItem.Page info -> FlatMenu.Page info
                | MenuItem.Link info -> FlatMenu.Link info
                | MenuItem.List _ ->
                    failwith "Should not happen because all the MenuItem.List should have been flattened"
            )

        let currentPageIndexInTheMenu =
            flatMenu
            |> List.tryFindIndex (
                function
                | FlatMenu.Link _ ->
                    false

                | FlatMenu.Page { PageId = pageId } ->
                    pageId = pageContext.PageId
            )

        match currentPageIndexInTheMenu with
        // The current page is not in the menu so don't can't generate navigation elements
        | None ->
            null

        | Some currentPageIndexInTheMenu ->
            let previousButton =
                // This is the first element of the menu
                if currentPageIndexInTheMenu < 1 then
                    emptyPreviousButton
                else
                    let previousMenuItem = flatMenu.[currentPageIndexInTheMenu - 1]

                    match previousMenuItem with
                    // Direct previous menu item is not a documentation page don't generate the previous button
                    | FlatMenu.Link _ ->
                        null

                    | FlatMenu.Page itemInfo ->
                        let previousPageContext =
                            allPages
                            |> Array.tryFind (fun pageContext ->
                                pageContext.PageId = itemInfo.PageId
                            )
                            |> function
                                | Some previousPageContext ->
                                    previousPageContext

                                | None ->
                                    failwith $"Page of id '%s{itemInfo.PageId}' not found. You either need to create it or remove it from the menu.json file"

                        let previousButtonText =
                            getMenuLabel previousPageContext itemInfo

                        Bulma.button.a [
                            prop.className "navigate-to-previous"
                            color.isPrimary
                            button.isOutlined
                            prop.href (baseUrl + previousPageContext.PageId)

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
                                    prop.text previousButtonText
                                ]
                            ]
                        ]

            let nextButton =
                // This is the first element of the menu
                if currentPageIndexInTheMenu >= flatMenu.Length - 1 then
                    emptyNextButton
                else
                    let nextMenuItem = flatMenu.[currentPageIndexInTheMenu + 1]

                    match nextMenuItem with
                    // Direct previous menu item is not a documentation page don't generate the previous button
                    | FlatMenu.Link _ ->
                        null

                    | FlatMenu.Page itemInfo ->
                        let nextPageContext =
                            allPages
                            |> Array.tryFind (fun pageContext ->
                                pageContext.PageId = itemInfo.PageId
                            )
                            |> function
                                | Some nextPageContext ->
                                    nextPageContext

                                | None ->
                                    failwith $"Page of id '%s{itemInfo.PageId}' not found. You either need to create it or remove it from the menu.json file"

                        let nextButtonText =
                            getMenuLabel nextPageContext itemInfo

                        Bulma.button.a [
                            prop.className "navigate-to-next"
                            color.isPrimary
                            button.isOutlined
                            prop.href (baseUrl + nextPageContext.PageId)

                            prop.children [
                                Bulma.text.span [
                                    text.isUppercase
                                    prop.text nextButtonText
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


            Html.div [
                prop.className "navigation-container"
                prop.children [
                    previousButton
                    nextButton
                ]
            ]

let private renderEditButton (config : Config) (pageContext : PageContext) =
    match config.EditUrl with
    | Some url ->
        Bulma.button.a [
            helpers.isHiddenTouch
            button.isOutlined
            color.isPrimary
            helpers.isPulledRight
            prop.target.blank
            prop.href (url + "/" + pageContext.RelativePath)
            prop.text "Edit"
        ]

    | None ->
        null


let rec private tryFindTitlePathToCurrentPage
    (pageContext : PageContext)
    (acc : string list)
    (menu : Menu) =

    match menu with
    | head :: tail ->
        match head with
        // Skip this item as it doesn't represent a page
        | MenuItem.Link _ ->
            tryFindTitlePathToCurrentPage pageContext acc tail

        | MenuItem.List info ->
            match tryFindTitlePathToCurrentPage pageContext (acc @ [ info.Label ]) info.Items with
            | Some res ->
                Some res

            | None ->
                tryFindTitlePathToCurrentPage pageContext acc tail

        | MenuItem.Page info ->
            if info.PageId = pageContext.PageId then
                let menuLabel =
                    getMenuLabel pageContext info

                Some (acc @ [ menuLabel ])
            else
                tryFindTitlePathToCurrentPage pageContext acc tail

    | [ ] ->
        None

let private renderBreadcrumbItems (items : string list) =
    items
    |> List.map (fun item ->
        Html.li [
            // Make the item active to make it not clickable
            prop.className "is-active"

            prop.children [
                Html.a [
                    prop.text item
                ]
            ]
        ]
    )

let private renderBreadcrumb
    (pageContext : PageContext)
    (menu : Menu) =

    match tryFindTitlePathToCurrentPage pageContext [ ] menu with
    | None ->
        null

    | Some titlePath ->
        Html.div [
            prop.className "mobile-menu"

            prop.children [
                Bulma.breadcrumb [
                    Html.ul [
                        Html.li [
                            Html.a [
                                prop.className "menu-trigger"

                                prop.children [
                                    Html.span [  ]
                                    Html.span [  ]
                                    Html.span [  ]
                                ]
                            ]
                        ]

                        yield! renderBreadcrumbItems titlePath
                    ]
                ]
            ]
        ]


let private renderMainContent
        (titleOpt : string option)
        (editButton : ReactElement)
        (navigationButtons : ReactElement)
        (markdownContent : string) =

    React.fragment [
        Bulma.section [
            Bulma.content [
                Html.header [
                    prop.className "page-header"
                    prop.children [
                        editButton

                        match titleOpt with
                        | Some title ->
                            Bulma.title.h1 title
                        | None ->
                            ()
                    ]
                ]

                Html.div [
                    prop.dangerouslySetInnerHTML markdownContent
                ]
            ]
        ]

        navigationButtons
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

let private renderTableOfContents (tableOfContent : TableOfContentParser.Section list) =
    if tableOfContent.Length > 0 then
       Html.ul [
           prop.className "table-of-content"

           prop.children [
               for tocElement in tableOfContent do
                   renderTopLevelToc tocElement
           ]
        ]
    else
        null

let private renderMenuItemPage
    (rendererContext : RendererContext)
    (info : MenuItemPage)
    (currentPageId : string)
    (tocInformation : TableOfContentParser.Section list) =

    let labelText =
        match info.Label with
        | Some label ->
            label

        | None ->
            let pageContext =
                rendererContext.Pages
                |> Array.tryFind (fun pageContext ->
                    pageContext.PageId = info.PageId
                )
                |> function
                    | Some pageContext ->
                        pageContext

                    | None ->
                        failwith $"Page of id '%s{info.PageId}' not found. You either need to create it or remove it from the menu.json file"

            match pageContext.Title with
            | Some title ->
                title

            | None ->
                failwith $"Page of id '%s{info.PageId}' doesn't have a label set for the menu. You can provide one by using 'label' in the menu.json or adding a 'title' property to the front matter of the file"

    let isCurrentPage =
        info.PageId = currentPageId

    let hasTableOfContent =
        not tocInformation.IsEmpty

    React.fragment [
        Bulma.menuItem.a [
            prop.classes [
                if isCurrentPage then
                    "is-active"

                    if hasTableOfContent then
                        "has-table-of-content"

            ]

            prop.href (rendererContext.Config.BaseUrl + info.PageId)
            prop.text labelText
        ]

        if isCurrentPage then
            renderTableOfContents tocInformation
    ]


/// <summary>
/// Render sub-menu
/// </summary>
let rec private renderSubMenu
    (menu : Menu)
    (rendererContext : RendererContext)
    (currentPageId : string)
    (tocInformation : TableOfContentParser.Section list) =

    menu
    |> List.map (
        function
        | MenuItem.Link info ->
            Bulma.menuItem.a [
                prop.href info.Href
                prop.text info.Label
            ]

        | MenuItem.Page info ->
            renderMenuItemPage rendererContext info currentPageId tocInformation

        | MenuItem.List info ->
            Bulma.menuList [
                prop.className "has-menu-group"

                prop.children [
                    Html.li [
                        Html.a [
                            prop.className "menu-group"
                            prop.children [
                                Html.span info.Label

                                Bulma.icon [
                                    Fa.i [ Fa.Solid.AngleRight; Fa.Size Fa.FaLarge ]
                                        [  ]
                                ]
                            ]
                        ]

                        Html.ul [
                            yield! renderSubMenu info.Items rendererContext currentPageId tocInformation
                        ]
                    ]
                ]
            ]
    )

/// <summary>
/// Render menu from the top level
/// </summary>
let rec private renderMenu
    (menu : Menu)
    (rendererContext : RendererContext)
    (currentPageId : string)
    (tocInformation : TableOfContentParser.Section list) =

    let menuContent =
        menu
        |> List.map (
            function
            | MenuItem.Link info ->
                Bulma.menuItem.a [
                    prop.href info.Href
                    prop.text info.Label
                ]

            | MenuItem.Page info ->
                renderMenuItemPage rendererContext info currentPageId tocInformation

            | MenuItem.List info ->
                Bulma.menuList [
                    Bulma.menuLabel info.Label

                    yield! renderSubMenu info.Items rendererContext currentPageId tocInformation
                ]
        )

    Html.div [
        prop.className "menu-container"

        prop.children [
            Bulma.menu [
                Bulma.menuList [
                    prop.children menuContent
                ]
            ]
        ]
    ]

let private renderPageWithMenuOrTableOfContent
    (rendererContext : RendererContext)
    (breadcrumbElement : ReactElement)
    (menuElement : ReactElement)
    (pageContext : PageContext)
    (pageContent : string) =

    Bulma.container [
        breadcrumbElement

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
                        menuElement
                    ]
                ]

                Bulma.column [
                    column.is8Desktop
                    column.isFullTouch

                    prop.children [
                        renderMainContent
                            pageContext.Title
                            (renderEditButton rendererContext.Config pageContext)
                            (renderNavigationButtons rendererContext.Config.BaseUrl rendererContext.Pages rendererContext.SectionMenu pageContext)
                            pageContent
                    ]
                ]
            ]
        ]
    ]


let private renderPageWithoutMenuOrTableOfContent
    (rendererContext : RendererContext)
    (pageContext : PageContext)
    (pageContent : string) =

    Bulma.container [
        Bulma.columns [
            columns.isMobile

            prop.children [
                Bulma.column [
                    column.is8Desktop
                    column.isOffset2Desktop

                    prop.children [
                        renderMainContent
                            pageContext.Title
                            (renderEditButton rendererContext.Config pageContext)
                            (renderNavigationButtons rendererContext.Config.BaseUrl rendererContext.Pages rendererContext.SectionMenu pageContext)
                            pageContent
                    ]
                ]
            ]
        ]
    ]

let private renderTableOfContentOnly
    (tocInformation : TableOfContentParser.Section list) =

    Html.div [
        prop.className "menu-container"

        prop.children [
            Bulma.menu [
                Bulma.menuList [

                    Bulma.menuItem.a [
                        prop.className "is-active has-table-of-content"
                        prop.text "Table of content"
                    ]

                    renderTableOfContents tocInformation
                ]
            ]
        ]
    ]

let private renderPage
    (rendererContext : RendererContext)
    (pageContext : PageContext)
    (pageContent : string) =
    let tocInformation =
        TableOfContentParser.parse pageContent pageContext.RelativePath

    match rendererContext.SectionMenu, tocInformation.IsEmpty with
    // If there is a menu, we render it with the menu
    // The menu renderer will take care of generating the TOC elements if needed
    | Some sectionMenu, false
    | Some sectionMenu, true ->
        printfn "has menu"
        renderPageWithMenuOrTableOfContent
            rendererContext
            (renderBreadcrumb pageContext sectionMenu)
            (renderMenu sectionMenu rendererContext pageContext.PageId tocInformation)
            pageContext
            pageContent

    | None, false ->
        renderPageWithMenuOrTableOfContent
            rendererContext
            null // No breadcrumb because there is no menu
            (renderTableOfContentOnly tocInformation)
            pageContext
            pageContent

    | None, true ->
        renderPageWithMenuOrTableOfContent
            rendererContext
            null
            null
            pageContext
            pageContent



let render (rendererContext : RendererContext) (pageContext : PageContext) =
    promise {
        let! pageContent =
            pageContext.Content
            |> rendererContext.MarkdownToHtml

        return Prelude.basePage
            {
                Config = rendererContext.Config
                Section = pageContext.Section
                TitleOpt = pageContext.Title
                Content =
                    React.fragment [
                        renderPage rendererContext pageContext pageContent
                        Html.script [
                            prop.async true
                            prop.src (rendererContext.Config.BaseUrl + Dependencies.menu)
                        ]
                    ]
            }
    }


//    let rec private renderBreadcrumbItem (model : Model) (menuItem : MenuItem) =
//        match menuItem with
//        | MenuItem pageId ->
//            [
//                Breadcrumb.item [ ]
//                    [ a [ ]
//                        [ str pageId ] ]
//            ]
//
//        | MenuList (label, items) ->
//            let first =
//                Breadcrumb.item [ ]
//                    [ a [ ]
//                        [ str label ] ]
//
//            let tail =
//                items
//                |> Array.map (renderBreadcrumbItem model)
//                |> Array.toList
//                |> List.concat
//
//            first :: tail
//
//    let private renderBreadcrumb (model : Model) (pageAttributes : Attributes) (pageContext : PageContext) =
//        let getTitle pageId =
//            let pageContext = model.DocFiles.get pageId
//
//            if box pageContext <> null then
//                pageAttributes.Title
//
//            else
//                Log.error "Unable to find the fil2e: %s" pageId
//                sprintf "Page `%s` not found" pageId
//
//        let rec findPathForActiveMenu (searchedId : string) (acc : string list) (menuConfig : MenuItem list) =
//            match menuConfig with
//            | head::tail ->
//                match head with
//                | MenuItem pageId ->
//                    if searchedId = pageId then
//                        Some (acc @ [ getTitle pageId ] )
//                    else
//                        findPathForActiveMenu searchedId acc tail
//                | MenuList (categoryTitle, items) ->
//                    let items =
//                        items |> Array.toList
//
//                    match findPathForActiveMenu searchedId (acc @ [ categoryTitle ]) items with
//                    | Some res ->
//                        Some res
//
//                    | None ->
//                        findPathForActiveMenu searchedId acc tail
//
//            | [] ->
//                None
//
//        let path =
//            match model.Config.MenuConfig with
//            | Some menuConfig ->
//                let pageId = getFileId model.Config.Source pageContext
//
//                menuConfig
//                |> List.map ( fun (category, items) ->
//                    let items =
//                            items |> Array.toList
//
//                    findPathForActiveMenu pageId [ category ] items
//                )
//                |> List.filter Option.isSome
//                |> List.tryHead
//                |> function
//                | Some x -> x
//                | None -> None
//
//            | None ->
//                None
//
//        match path with
//        | Some path ->2
//            path
//            |> List.map (fun id ->
//                // Make the items active so they aren't styled as clickable
//                Breadcrumb.item [ Breadcrumb.Item.IsActive true ]
//                    [ a [ ]
//                        [ str id ] ]
//            )
//            |> (fun items ->
//                let breadcrumbButton =
//                    Breadcrumb.item [ ]
//                        [
//                            a [ Class "menu-trigger" ]
//                                [
//                                    span [ ] [ ]
//                                    span [ ] [ ]
//                                    span [ ] [ ]
//                                ]
//                        ]
//
//                // Hide the edit button on mobile
//                div [ Class "mobile-menu is-hidden-desktop" ]
//                    [
//                        Breadcrumb.breadcrumb [ ]
//                            [
//                                yield breadcrumbButton
//                                yield! items
//                            ]
//                    ]
//            )
//            |> Some
//
//        | None ->
//            None
//
//
//    let private renderPageWithoutMenu (model : Model) (pageAttributes : Attributes) (pageContext : PageContext) (breadcrumb : ReactElement option) (editButton : ReactElement) (title : string) (pageContent : string) =
//        Bulma.container [
//            ofOption breadcrumb
//
//            Bulma.columns [
//                prop.children [
//                    Bulma.column [
//                        column.is8Desktop
//                        column.isOffset2Desktop
//
//                        prop.children [
//                            renderMainContent model pageAttributes pageContext editButton title pageContent
//                        ]
//                    ]
//                ]
//            ]
//        ]
//
//    let renderTopLevelToc (section : TableOfContentParser.Section) =
//        Html.li [
//            Html.a [
//                prop.dangerouslySetInnerHTML section.Header.Title
//                prop.href section.Header.Link
//                prop.custom("data-toc-element", true)
//            ]
//
//            if not section.SubSections.IsEmpty then
//
//                Html.ul [
//                    prop.className "table-of-content"
//
//                    prop.children [
//                        for subSection in section.SubSections do
//                            Html.li [
//                                Html.a [
//                                    prop.dangerouslySetInnerHTML subSection.Title
//                                    prop.href subSection.Link
//                                    prop.custom("data-toc-element", true)
//                                ]
//                            ]
//                    ]
//                ]
//        ]
//
//    let renderMenuItem (model : Model) (pageAttributes : Attributes) (pageContext : PageContext) (tableOfContent : TableOfContentParser.TableOfContent) pageId =
//
//        let pageInfo = model.DocFiles.get pageId
//
//        let currentPageId = getFileId model.Config.Source pageContext
//
//        let isCurrentPage = (currentPageId = pageId)
//
//        let hasTableOfContent =
//            isCurrentPage && not tableOfContent.IsEmpty
//
//        let tableOfContent =
//            if hasTableOfContent then
//                Html.ul [
//                    prop.className "table-of-content"
//
//                    prop.children [
//                        for tocElement in tableOfContent do
//                            renderTopLevelToc tocElement
//                    ]
//                ]
//
//            else
//                nothing
//
//        if box pageInfo <> null then
//            Html.li [
//                if hasTableOfContent then
//                    prop.className "has-table-of-content"
//
//                prop.children [
//                    Menu.Item.a [ Menu.Item.Props [ Data("menu-id", pageId) ]
//                                  generateUrl model.Config pageInfo
//                                  |> Menu.Item.Href
//                                  Menu.Item.IsActive isCurrentPage ]
//                        [
//                            str pageAttributes.Title
//                        ]
//
//                    tableOfContent
//                ]
//            ]
//
//        else
//            Log.error "Unable to find the file33: %s" pageId
//            nothing
//
//    let rec private renderMenu (model : Model) (pageAttributes : Attributes) (pageContext : PageContext) (tableOfContent : TableOfContentParser.TableOfContent) (menuItem : MenuItem) =
//        match menuItem with
//        | MenuItem pageId ->
//            renderMenuItem model pageAttributes pageContext tableOfContent pageId
//
//        | MenuList (label, items) ->
//            let subMenu =
//                items
//                |> Array.map (function
//                    | MenuItem pageId ->
//                        renderMenuItem model pageAttributes pageContext tableOfContent pageId
//
//                    | MenuList (label, x) ->
//
//                        renderMenu model pageAttributes pageContext tableOfContent (MenuList (label, x))
//                        // Log.warn "Nacara only support only 2 level deep menus. The following menu is too deep:\n%A" label
//                        // nothing
//                )
//                |> Array.toList
//                |> ul [ ]
//
//            Menu.list [ ]
//                [
//                    li [ ]
//                        [
//                            a [ Class "menu-group" ]
//                                [
//                                    span [ ]
//                                        [ str label ]
//                                    Icon.icon [ ]
//                                        [ Fa.i
//                                            [
//                                                Fa.Solid.AngleRight
//                                                Fa.Size Fa.FaLarge
//                                            ]
//                                    [ ] ]
//                                ]
//                            subMenu
//                        ]
//                ]
//
//    let private generateMenu (model : Model) (pageAttributes : Attributes) (pageContext : PageContext) (tableOfContent : TableOfContentParser.TableOfContent) =
//        match model.Config.MenuConfig with
//        | Some menuConfig ->
//            menuConfig
//            |> List.map (fun (category, menuItems) ->
//                fragment [ ]
//                    [
//                        Menu.label [ ]
//                            [ str category ]
//                        menuItems
//                        |> Array.map (renderMenu model pageAttributes pageContext tableOfContent)
//                        |> Array.toList
//                        |> Menu.list [ ]
//                    ]
//
//            )
//            |> fun content ->
//                div [ Class "menu-container" ]
//                    [
//                        Menu.menu [ ]
//                            content
//                    ]
//            |> Some
//
//        | None ->
//            None
//
//    let private addMenuScript (content : ReactElement) =
//        fragment [ ]
//            [
//                content
//
//                Html.script [
//                    prop.src "/static/nacara_internals/menu.js"
//                ]
//            ]
//
//    let private generateTocOnly (tableOfContent : TableOfContentParser.TableOfContent) =
//        let hasTableOfContent =
//            not tableOfContent.IsEmpty
//
//        if hasTableOfContent then
//            Html.ul [
//                prop.className "table-of-content"
//
//                prop.children [
//                    for tocElement in tableOfContent do
//                        renderTopLevelToc tocElement
//                ]
//            ]
//
//        else
//            nothing
//
//
//    let private generateMenuOrToc (model : Model) (pageAttributes : Attributes) (pageContext : PageContext) =
//        let tocInformation =
//            TableOfContentParser.parse pageContext
//
//        if pageAttributes.Menu then
//            generateMenu model pageAttributes pageContext tocInformation
//        else
//            if pageAttributes.ShowToc then
//                div [ Class "menu-container" ]
//                    [
//                        Bulma.menu [
//                            Bulma.menuList [
//                                Html.li [
//                                    prop.className "has-table-of-content"
//
//                                    prop.children [
//                                        Html.a [
//                                            prop.className "is-active"
//                                            prop.text "Table of content"
//                                        ]
//
//                                        generateTocOnly tocInformation
//                                    ]
//                                ]
//                            ]
//                        ]
//                    ]
//                |> Some
//            else
//                None
//
//
//    let toHtml (model : Model) (pageContext : PageContext) =
//        promise {
//            match Decode.fromValue "$" Attributes.Decoder pageContext.FrontMatter with
//            | Ok pageAttributes ->
//                let! pageContext =
//                    pageContext
//                    |> PageContext.processMarkdown model
//
//                let menuOpt =
//                    generateMenuOrToc model pageAttributes pageContext
//
//                let page =
//                    match menuOpt with
//                    | Some menu ->
//                        renderPageWithMenu
//                            model
//                            pageAttributes
//                            pageContext
//                            menu
//                            (renderBreadcrumb model pageAttributes pageContext)
//                            (renderEditButton model.Config pageAttributes pageContext)
//                            pageAttributes.Title
//                            pageContext.Content
//
//                    | None ->
//                        renderPageWithoutMenu
//                            model
//                            pageAttributes
//                            pageContext
//                            (renderBreadcrumb model pageAttributes pageContext)
//                            (renderEditButton model.Config pageAttributes pageContext)
//                            pageAttributes.Title
//                            pageContext.Content
//
//                return
//                    page
//                    |> addMenuScript
//                    |> Prelude.basePage model (Some pageAttributes.Title)
//
//            | Error errorMessage ->
//                return failwith errorMessage
//        }
