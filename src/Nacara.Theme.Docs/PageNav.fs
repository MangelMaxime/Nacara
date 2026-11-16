namespace Nacara.Theme.Docs

open Nacara.Core
open Nacara.Theme
open Nacara.Theme.Core

[<RequireQualifiedAccess>]
module PageNav =

    /// <summary>The pages before and after one, in a given order.</summary>
    /// <remarks><c>None</c> when the page is not among them, or stands alone.</remarks>
    /// <param name="site">Where the site is served from.</param>
    /// <param name="ordered">The pages, in the order a reader walks through them.</param>
    /// <param name="current">The page being read.</param>
    let among (site: SiteInfo) (ordered: Page list) (current: Page) : PageNavModel option =
        let link (page: Page) : PageLink =
            {
                Title = page.Title
                Url = site.UrlOf page.Route
            }

        match ordered |> List.tryFindIndex (fun page -> page.Id = current.Id) with
        | None -> None
        | Some index ->
            let previous =
                if index > 0 then
                    Some(link (List.item (index - 1) ordered))
                else
                    None

            let next =
                if index < List.length ordered - 1 then
                    Some(link (List.item (index + 1) ordered))
                else
                    None

            if previous.IsNone && next.IsNone then
                None
            else
                Some
                    {
                        Previous = previous
                        Next = next
                    }

    /// <summary>The previous and next pages of the section, following the order of its menu.</summary>
    /// <param name="options">The menus the site declared, which say the order outright.</param>
    /// <param name="scope">The page being rendered, and the site around it.</param>
    let resolve (options: DocsOptions) (scope: PageScope) =
        let ordered =
            match Sidebar.menuOf options scope.Site (Pages.section scope.Page) with
            | Some items -> Sidebar.pages scope.Pages items
            | None -> Pages.ofSection scope

        among scope.Site ordered scope.Page
