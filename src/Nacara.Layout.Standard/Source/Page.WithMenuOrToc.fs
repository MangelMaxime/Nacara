module Page.WithMenuOrToc

open Nacara.Core.Types
open Feliz
open Feliz.Bulma
open Fable.FontAwesome

let private renderTopLevelToc (section : TableOfContentParser.Section) =
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
        Html.li [
            Html.ul [
                prop.className "table-of-content"

                prop.children [
                    for tocElement in tableOfContent do
                        renderTopLevelToc tocElement
                ]
            ]
        ]
    else
        null

let private renderMenuItemPage
    (config : Config)
    (pages : PageContext array)
    (info : MenuItemPage)
    (currentPageId : string)
    (tocInformation : TableOfContentParser.Section list) =

    let labelText =
        match info.Label with
        | Some label ->
            label

        | None ->
            let pageContext =
                pages
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

            prop.href (config.BaseUrl + info.PageId + ".html")
            prop.text labelText
        ]

        if isCurrentPage then
            renderTableOfContents tocInformation
    ]

/// <summary>
/// Render sub-menu
/// </summary>
let rec private renderSubMenu
    (config : Config)
    (pages : PageContext array)
    (menu : Menu)
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
            renderMenuItemPage config pages info currentPageId tocInformation

        | MenuItem.List info ->
            let defaultState =
                if info.Collapsed then
                    "collapsed"
                else
                    "expanded"
            Html.li [
                Html.a [
                    prop.classes [
                        "menu-group"
                        if not info.Collapsed then
                            "is-expanded"
                    ]
                    prop.custom("data-default-state", defaultState)
                    prop.custom("data-collapsible", info.Collapsible)
                    prop.children [
                        Html.span info.Label

                        if info.Collapsible then
                            Bulma.icon [
                                Fa.i [ Fa.Solid.AngleRight; Fa.Size Fa.FaLarge ]
                                    [  ]
                            ]
                    ]
                ]

                Html.ul [
                    yield! renderSubMenu config pages info.Items currentPageId tocInformation
                ]
            ]

    )

/// <summary>
/// Render menu from the top level
/// </summary>
let rec private renderMenu
    (config : Config)
    (pages : PageContext array)
    (menu : Menu)
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
                renderMenuItemPage config pages info currentPageId tocInformation

            | MenuItem.List info ->
                React.fragment [
                    Bulma.menuLabel info.Label

                    Bulma.menuList [
                        yield! renderSubMenu config pages info.Items currentPageId tocInformation
                    ]
                ]
        )

    Html.div [
        prop.className "menu-container"

        prop.children [
            Bulma.menu [
                prop.children menuContent
            ]
        ]
    ]


let private renderPageWithMenuOrTableOfContent
    (breadcrumbElement : ReactElement)
    (menuElement : ReactElement)
    (pageContent : ReactElement) =

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

                    prop.children [
                        menuElement
                    ]
                ]

                Bulma.column [
                    column.is8Desktop
                    column.isFullTouch

                    prop.children [
                        pageContent
                    ]
                ]
            ]
        ]
    ]


let private renderPageWithoutMenuOrTableOfContent (pageContent : ReactElement) =

    Bulma.container [
        Bulma.columns [
            columns.isMobile

            prop.children [
                Bulma.column [
                    column.is8Desktop
                    column.isOffset2Desktop

                    prop.children [
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
                    Helpers.getMenuLabel pageContext info

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


[<NoComparison>]
type RenderArgs =
    {
        Config : Config
        SectionMenu : Menu option
        Pages : PageContext array
        PageContext : PageContext
        PageHtml : string
        PageContent : ReactElement
        RenderMenu : bool
    }

let render (args : RenderArgs) =

    let tocInformation =
        TableOfContentParser.parse args.PageHtml args.PageContext.RelativePath

    if args.RenderMenu then
        match args.SectionMenu, tocInformation.IsEmpty with
        // If there is a menu, we render it with the menu
        // The menu renderer will take care of generating the TOC elements if needed
        | Some sectionMenu, false
        | Some sectionMenu, true ->
            renderPageWithMenuOrTableOfContent
                (renderBreadcrumb args.PageContext sectionMenu)
                (renderMenu args.Config args.Pages sectionMenu args.PageContext.PageId tocInformation)
                args.PageContent

        | None, false ->
            renderPageWithMenuOrTableOfContent
                null // No breadcrumb because there is no menu
                (renderTableOfContentOnly tocInformation)
                args.PageContent

        | None, true ->
            renderPageWithMenuOrTableOfContent
                null
                null
                args.PageContent

    // Layout forced to not render the menu, so only try to render the page with a TOC
    else
        if tocInformation.IsEmpty then
            renderPageWithMenuOrTableOfContent
                null
                null
                args.PageContent

        else
            renderPageWithMenuOrTableOfContent
                null // No breadcrumb because there is no menu
                (renderTableOfContentOnly tocInformation)
                args.PageContent
