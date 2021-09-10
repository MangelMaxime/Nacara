module Navbar

open Nacara.Core.Types

let tryFindWebsiteSectionLabelForPage
    (navbar : NavbarConfig)
    (pageContext : PageContext) =
        let rec tryFindFromDropdown (dropdownItems : DropdownItem list) =
            match dropdownItems with
            | head :: tail ->
                match head with
                | DropdownItem.Divider ->
                    tryFindFromDropdown tail

                | DropdownItem.Link { Section = Some itemSection; Label = itemLabel } ->
                    if itemSection = pageContext.Section then
                        Some itemLabel
                    else
                        tryFindFromDropdown tail

                | DropdownItem.Link _ ->
                    tryFindFromDropdown tail

            | [] ->
                None

        let rec tryFind (navbarItems : StartNavbarItem list) =
            match navbarItems with
            | head :: tail ->
                match head with
                | StartNavbarItem.LabelLink { Section = Some itemSection; Label = itemLabel } ->
                    if itemSection = pageContext.Section then
                        Some [ itemLabel ]
                    else
                        tryFind tail

                | StartNavbarItem.Dropdown dropdownInfo ->
                    match tryFindFromDropdown dropdownInfo.Items with
                    | Some label ->
                        Some [ dropdownInfo.Label; label ]

                    | None ->
                        None

                | _ ->
                    tryFind tail

            | [] ->
                None

        tryFind navbar.Start
