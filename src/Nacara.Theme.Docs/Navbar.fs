namespace Nacara.Theme.Docs

open Nacara.Core
open Nacara.Theme
open Nacara.Theme.Core

[<RequireQualifiedAccess>]
module Navbar =

    let rec private entry (scope: PageScope) (item: NavbarItem) =
        let url = Links.resolve scope.Site scope.Pages

        match item with
        | NavbarDivider -> Some NavbarEntry.Divider
        | NavbarLink(label, target) -> Some(NavbarEntry.Link(label, url target))
        | NavbarSection(label, section, target) ->
            Some(NavbarEntry.Section(label, url target, section = Pages.section scope.Page))
        | NavbarIcon(label, target, svg) -> Some(NavbarEntry.Icon(label, url target, svg))
        | NavbarDescribed(label, description, target) ->
            Some(NavbarEntry.Described(label, description, url target))
        | NavbarWidget html -> Some(NavbarEntry.Widget html)
        | NavbarDynamicWidget render -> Some(NavbarEntry.Widget(render scope.Site))
        | NavbarLocalePicker ->
            if List.length scope.Site.Locales < 2 then
                None
            else
                Some(
                    NavbarEntry.LocalePicker(
                        scope.Page.Locale,
                        Translations.resolve scope.Site scope.Pages scope.Page
                    )
                )
        | NavbarDropdown(label, items) ->
            Some(NavbarEntry.Dropdown(label, items |> List.choose (entry scope)))

    /// <summary>The navbar of a page: every url resolved, the current section lit.</summary>
    /// <param name="options">What the site put in the bar.</param>
    /// <param name="scope">The page being rendered, and the site around it.</param>
    let resolve (options: DocsOptions) (scope: PageScope) : NavbarModel =
        {
            BrandLabel = scope.Site.Title
            BrandUrl = scope.Site.UrlOf(Route.home scope.Site.RootLocale)
            Start = options.Navbar |> List.choose (entry scope)
            End = options.NavbarEnd |> List.choose (entry scope)
            Section = Pages.section scope.Page
        }
