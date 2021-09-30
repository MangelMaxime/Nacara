module Helpers

open Feliz
open Nacara.Core.Types

let getMenuLabel (pageContext : PageContext) (itemInfo : MenuItemPage) =
    match pageContext.Title, itemInfo.Label with
    | Some label, Some _
    | None, Some label
    | Some label, None  -> label
    | None, None ->
        failwith $"Missing label information for '%s{itemInfo.PageId}'. You can set it in the markdown page using the 'title' property or directly in the menu.json via the 'label' property"


let rec tryFindTitlePathToCurrentPage
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


let renderBreadcrumbItems (items : string list) =
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
