module Menu

open Nacara.Core.Types


/// <summary>
/// Transform the given menu into a flat list of MenuItem.Page and MenuItem.Link
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
/// Transform a menu into a FlatMenu representation
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
