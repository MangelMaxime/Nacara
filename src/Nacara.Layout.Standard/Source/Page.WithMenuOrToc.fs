module Page.WithMenuOrToc

open Nacara.Core.Types
open Feliz
open Feliz.Bulma
open Thoth.Json

type TocConfig =
    {
        From : int
        To : int
    }

type TocAttributes = TocConfig option

module TocAttributes =

    let decoder : Decoder<TocAttributes> =
        let defaultConfig =
            {
                From = 2
                To = 2
            }

        Decode.oneOf [
            Decode.object (fun get ->
                {
                    From = get.Optional.At [ "toc"; "from" ] Decode.int
                        |> Option.defaultValue 2
                    To = get.Optional.At [ "toc"; "to" ] Decode.int
                        |> Option.defaultValue 2
                }
            )
            |> Decode.map Some

            Decode.optional "toc" (
                Decode.bool
                |> Decode.andThen (
                    function
                    // True means we activate the TOC with the default config
                    | true ->
                        Decode.succeed (Some defaultConfig)

                    // False means we disable the TOC
                    | false  ->
                        Decode.succeed None
                )
            )
            |> Decode.map Option.flatten
        ]

let private renderTocElement
    (tocConfig : TocConfig)
    (rank : int)
    (headerInfo : TableOfContentParser.HeaderInfo) =
    if tocConfig.From <= rank && rank <= tocConfig.To then
        Html.li [
            prop.custom("data-toc-rank", rank)
            prop.children [
                Html.a [
                    prop.dangerouslySetInnerHTML headerInfo.Title
                    prop.href headerInfo.Link
                    prop.custom("data-toc-element", true)
                ]
            ]
        ]
    else
        null

let private renderTableOfContents
    (tocConfig : TocConfig)
    (tableOfContent : TableOfContentParser.Header list) =
    if tableOfContent.Length > 0 then
        Html.li [
            Html.ul [
                prop.className "table-of-content"

                prop.children [
                    for tocElement in tableOfContent do
                        match tocElement with
                        | TableOfContentParser.Header2 headerInfo ->
                            renderTocElement tocConfig 2 headerInfo

                        | TableOfContentParser.Header3 headerInfo ->
                            renderTocElement tocConfig 3 headerInfo

                        | TableOfContentParser.Header4 headerInfo ->
                            renderTocElement tocConfig 4 headerInfo

                        | TableOfContentParser.Header5 headerInfo ->
                            renderTocElement tocConfig 5 headerInfo

                        | TableOfContentParser.Header6 headerInfo ->
                            renderTocElement tocConfig 6 headerInfo
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
    (tocConfig : TocConfig)
    (tocInformation : TableOfContentParser.Header list) =

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

    React.fragment [
        Bulma.menuItem.a [
            prop.classes [
                if isCurrentPage then
                    "is-active"
            ]

            prop.href (config.SiteMetadata.BaseUrl + info.PageId + ".html")
            prop.text labelText
        ]

        if isCurrentPage then
            renderTableOfContents tocConfig tocInformation
    ]

/// <summary>
/// Render sub-menu
/// </summary>
let rec private renderSubMenu
    (config : Config)
    (pages : PageContext array)
    (menu : Menu)
    (currentPageId : string)
    (tocConfig : TocConfig)
    (tocInformation : TableOfContentParser.Header list) =

    menu
    |> List.map (
        function
        | MenuItem.Link info ->
            Bulma.menuItem.a [
                prop.className "menu-external-link"
                prop.href info.Href
                prop.text info.Label
                prop.target.blank
            ]

        | MenuItem.Page info ->
            renderMenuItemPage config pages info currentPageId tocConfig tocInformation

        | _ -> Html.none
    )

/// <summary>
/// Render menu from the top level
/// </summary>
let rec private renderMenu
    (config : Config)
    (pages : PageContext array)
    (menu : Menu)
    (currentPageId : string)
    (tocConfig : TocConfig)
    (tocInformation : TableOfContentParser.Header list) =

    let menuContent =
        menu
        |> List.map (
            function
            | MenuItem.Link info ->
                Bulma.menuList [
                    Bulma.menuItem.a [
                        prop.href info.Href
                        prop.text info.Label
                        prop.target.blank
                    ]
                ]

            | MenuItem.Page info ->
                Bulma.menuList [
                    Html.li [
                        renderMenuItemPage config pages info currentPageId tocConfig tocInformation
                    ]
                ]

            | MenuItem.List info ->
                React.fragment [
                    Bulma.menuLabel info.Label

                    Bulma.menuList [
                        yield! renderSubMenu config pages info.Items currentPageId tocConfig tocInformation
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
    (mobileMenu : ReactElement)
    (menuElement : ReactElement)
    (pageContent : ReactElement) =

    Bulma.container [
        mobileMenu

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

let private renderPageContentOnly (pageContent : ReactElement) =
    Bulma.container [
        Bulma.columns [
            Bulma.column [
                column.is8Desktop
                column.isOffset2Desktop

                prop.children [
                    pageContent
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
    (tocConfig : TocConfig)
    (tocInformation : TableOfContentParser.Header list) =

    Html.div [
        prop.className "menu-container"

        prop.children [
            Bulma.menu [
                Bulma.menuList [

                    Bulma.menuItem.a [
                        prop.className "is-active"
                        prop.text "Table of content"
                    ]

                    renderTableOfContents tocConfig tocInformation
                ]
            ]
        ]
    ]

let private renderMobileMenu
    (navbar : NavbarConfig)
    (pageContext : PageContext)
    (menu : Menu) =

    match Helpers.tryFindTitlePathToCurrentPage pageContext [ ] menu with
    | None ->
        null

    | Some titlePath ->
        let titlePath =
            match Navbar.tryFindWebsiteSectionLabelForPage navbar pageContext with
            | Some sectionLabel ->
                sectionLabel @ titlePath

            | None ->
                titlePath

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

                        yield! Helpers.renderBreadcrumbItems titlePath
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
    }

let render (args : RenderArgs) =

    let tocInformation =
        TableOfContentParser.parse args.PageHtml

    let tocAttributes =
        Decode.fromValue
            "$"
            TocAttributes.decoder
            args.PageContext.Attributes

    match tocAttributes with
    | Ok None ->
        renderPageContentOnly args.PageContent

    | Ok (Some tocConfig) ->
        // Print a warning an incoherent interval if given
        if tocConfig.To < tocConfig.From then
            failwith $"Invalid TOC interval provide for the page %s{args.PageContext.PageId}.\ toc.from muss be less than toc.to"

        match args.SectionMenu, tocInformation.IsEmpty with
        // If there is a menu, we render it with the menu
        // The menu renderer will take care of generating the TOC elements if needed
        | Some sectionMenu, _ ->
            renderPageWithMenuOrTableOfContent
                (renderMobileMenu args.Config.Navbar args.PageContext sectionMenu)
                (renderMenu args.Config args.Pages sectionMenu args.PageContext.PageId tocConfig tocInformation)
                args.PageContent

        // There is no menu but there is a table of content
        | None, false ->
            renderPageWithMenuOrTableOfContent
                null // No breadcrumb because there is no menu
                (renderTableOfContentOnly tocConfig tocInformation)
                args.PageContent

        // There is no menu neither a table of content
        | None, true ->
            renderPageContentOnly args.PageContent

    | Error errorMessage ->
        failwith $"Invalid toc configuration given for the page %s{args.PageContext.PageId}\n%s{errorMessage}"
