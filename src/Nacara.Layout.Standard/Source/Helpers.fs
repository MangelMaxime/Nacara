module Helpers

open Nacara.Core.Types

let getMenuLabel (pageContext : PageContext) (itemInfo : MenuItemPage) =
    match pageContext.Title, itemInfo.Label with
    | Some label, Some _
    | None, Some label
    | Some label, None  -> label
    | None, None ->
        failwith $"Missing label information for '%s{itemInfo.PageId}'. You can set it in the markdown page using the 'title' property or directly in the menu.json via the 'label' property"
