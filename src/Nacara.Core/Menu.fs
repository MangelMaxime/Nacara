/// Helpers making it easier to work with menus
module Menu

open Nacara.Core.Types


/// <summary>
/// Transform the given menu into a flat list of <c>MenuItem.Page</c> and <c>MenuItem.Link</c>
///
/// This function is mostly used by <c>toFlatMenu</c> function.
/// </summary>
/// <param name="menu">Menu to flatten</param>
/// <returns>A flatten representation of the menu</returns>
let rec flatten (menu : Menu) =
    menu
    |> List.collect (fun menuItem ->
        match menuItem with
        | MenuItem.Page _
        | MenuItem.Link _ -> [ menuItem ]
        | MenuItem.List info ->
            flatten info.Items
    )


/// <summary>
/// Transform a menu into a <c>FlatMenu</c> representation
///
/// This function is handy when generating menu or the navigation button between pages.
/// </summary>
/// <param name="menu">Menu to transform</param>
/// <returns>Converted menu to FlatMenu type</returns>
let toFlatMenu (menu : Menu) =
    flatten menu
    |> List.map (fun menuItem ->
        match menuItem with
        | MenuItem.Page info -> FlatMenu.Page info
        | MenuItem.Link info -> FlatMenu.Link info
        | MenuItem.List _ ->
            failwith "Should not happen because all the MenuItem.List should have been flattened"
    )

/// <summary>
/// It returns a <c>MenuItem.List</c> if it finds a page with the given pageId under a MenuItem.List.
///
/// Used for obtaining the section name for navigation buttons.
/// </summary>
/// <param name="menu">Menu where to look for a page ID.</param>
/// <param name="pageId">ID of the page to look for.</param>
/// <returns>MenuItem.List or MenuItem.Page or None.</returns>
let rec tryFindSection (menu : Menu) pageId =
    menu
    |> List.tryFind (fun menuItem ->
        match menuItem with
        | MenuItem.Page page -> page.PageId = pageId
        | MenuItem.Link _ -> false
        | MenuItem.List list ->
            match list.Items with
            | [] -> false
            | items ->
                match tryFindSection items pageId with
                | None -> false
                | Some _ -> true
    )
