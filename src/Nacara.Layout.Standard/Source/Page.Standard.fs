module Page.Standard

open Nacara.Core.Types
open Feliz
open Feliz.Bulma
open Fable.FontAwesome
open Page.WithMenuOrToc

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

let private renderNavigationButtons
    (baseUrl : string)
    (allPages : PageContext array)
    (menuOpt : Menu option)
    (pageContext : PageContext) =

    match menuOpt with
    | None ->
        null

    | Some menu ->
        let flatMenu =
            Menu.toFlatMenu menu

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
                            Helpers.getMenuLabel previousPageContext itemInfo

                        Bulma.button.a [
                            prop.className "bd-fat-button is-primary is-light bd-pagination-prev"
                            prop.href (baseUrl + previousPageContext.PageId + ".html")

                            prop.children [
                                Html.i "←"
                                Html.span [
                                    prop.children [
                                        Html.em [
                                            prop.text previousPageContext.Section
                                        ]
                                        Html.strong [
                                            prop.text previousButtonText
                                        ]
                                    ]
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
                            Helpers.getMenuLabel nextPageContext itemInfo

                        Bulma.button.a [
                            prop.className "bd-fat-button is-primary is-light bd-pagination-next"
                            prop.href (baseUrl + nextPageContext.PageId + ".html")

                            prop.children [
                                Html.span [
                                    prop.children [
                                        Html.em [
                                            prop.text nextPageContext.Section
                                        ]
                                        Html.strong [
                                            prop.text nextButtonText
                                        ]
                                    ]
                                ]
                                Html.i "→"
                            ]
                        ]


            Html.div [
                prop.className "section bd-docs-pagination bd-pagination-links"
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
            prop.className "is-ghost"
            helpers.isPulledRight
            prop.target.blank
            prop.href (url + "/" + pageContext.RelativePath)
            prop.text "Edit"
        ]

    | None ->
        null

let private renderBreadcrumb
    (pageContext : PageContext)
    (menu : Menu) =

    match tryFindTitlePathToCurrentPage pageContext [ ] menu with
    | None ->
        null

    | Some titlePath ->
        Bulma.breadcrumb [
            prop.className "is-small"

            helpers.isHiddenTouch
            prop.children [
                Html.ul [
                    yield! renderBreadcrumbItems titlePath
                ]
            ]
        ]

let private renderPageContent
        (titleOpt : string option)
        (editButton : ReactElement)
        (navigationButtons : ReactElement)
        (markdownContent : string)
        pageContext
        sectionMenu =

    React.fragment [
        Bulma.section [
            Bulma.content [
                Html.header [
                    prop.className "page-header"
                    prop.children [
                        editButton

                        match sectionMenu with
                        | Some sectionMenu ->
                            (renderBreadcrumb pageContext sectionMenu)
                        | None -> ()

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

let render (rendererContext : RendererContext) (pageContext : PageContext) =
    promise {
        let rendererContext =
            { rendererContext with
                MarkdownToHtml = rendererContext.MarkdownToHtmlWithPlugins Markdown.configure
            }

        let! pageContent =
            pageContext.Content
            |> rendererContext.MarkdownToHtml

        return Minimal.render
            {
                Config = rendererContext.Config
                Section = pageContext.Section
                TitleOpt = pageContext.Title
                Content =
                    WithMenuOrToc.render {
                        Config = rendererContext.Config
                        SectionMenu = rendererContext.SectionMenu
                        Pages = rendererContext.Pages
                        PageContext = pageContext
                        PageHtml = pageContent
                        RenderMenu = true
                        PageContent =
                            renderPageContent
                                pageContext.Title
                                (renderEditButton rendererContext.Config pageContext)
                                (renderNavigationButtons rendererContext.Config.BaseUrl rendererContext.Pages rendererContext.SectionMenu pageContext)
                                pageContent
                                pageContext
                                rendererContext.SectionMenu
                    }

            }
    }
