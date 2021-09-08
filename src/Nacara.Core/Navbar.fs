module Navbar

open Nacara.Core.Types

let tryFindWebsiteSectionLabelForPage
    (navbar : NavbarConfig)
    (pageContext : PageContext) =
        navbar.Start
        |> List.tryFind (
            function
            | StartNavbarItem.LabelLink { Section = Some itemSection } ->
                itemSection = pageContext.Section

            | _ ->
                false
        )
        |> Option.map (function
            | StartNavbarItem.LabelLink { Label = label } ->
                label
            | _ ->
                failwith "Should not happen, we filtered the navbar.Start elements before"
        )
