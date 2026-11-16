namespace Nacara.Theme.Docs

open System.Text.Json
open Nacara.Core
open Nacara.Theme
open Nacara.Theme.Core

/// <summary>A section's whole menu, written beside the site for the filter box to search.</summary>
[<RequireQualifiedAccess>]
module MenuStore =

    /// <summary>The directory the menus are written under, which pruning must leave alone.</summary>
    let preserved = "assets/menu"

    /// <summary>Where a section's whole menu is written.</summary>
    /// <param name="section">The section the menu is for.</param>
    let path (section: string) =
        $"assets/menu/%s{Slug.create section}.json"

    /// <summary>How many pages a menu names, at every depth.</summary>
    /// <param name="items">The menu.</param>
    let rec size (items: MenuItem list) =
        items
        |> List.sumBy (fun item ->
            match item.Entry with
            | MenuGroup(_, children) -> 1 + size children
            | MenuSection(_, children) -> size children
            | _ -> 1
        )

    /// <summary>A section's menu as json, whole, the way the page would have written it.</summary>
    /// <remarks>Shaped like the markup - a node carries its label, where it goes and what it
    /// holds - so the filter box can put back the entries a pruned page left out.</remarks>
    /// <param name="site">Where the site is served from.</param>
    /// <param name="pages">Every page of the site.</param>
    /// <param name="items">The menu.</param>
    let rec json (site: SiteInfo) (pages: Page list) (items: MenuItem list) =
        let text (value: string) = JsonSerializer.Serialize value

        let node (label: string) (url: string) (children: MenuItem list) =
            let holds =
                if List.isEmpty children then
                    ""
                else
                    $",\"children\":%s{json site pages children}"

            $"{{\"label\":%s{text label},\"url\":%s{text url}%s{holds}}}"

        items
        |> List.choose (fun item ->
            match item.Entry with
            | MenuLink(label, url) -> Some(node label url [])
            | MenuPage path ->
                Pages.find pages path
                |> Option.map (fun page -> node page.Title (site.UrlOf page.Route) [])
            | MenuGroup(path, children) ->
                Pages.find pages path
                |> Option.map (fun page -> node page.Title (site.UrlOf page.Route) children)
            // A section is a label with no page of its own, so its children stand in its place.
            | MenuSection(_, children) ->
                Some(json site pages children |> fun json -> json.Trim('[', ']'))
        )
        |> String.concat ","
        |> sprintf "[%s]"

    /// <summary>Writes every section's menu that a page prunes, whole.</summary>
    /// <remarks>A page holds the menu it was rendered with, which for a large section is a part of
    /// it. The filter box reads this to search the rest. Register with
    /// <see cref="M:Nacara.Core.Registry.onPagesRouted" />, and preserve
    /// <see cref="P:Nacara.Theme.Docs.MenuStore.preserved" />.</remarks>
    /// <param name="options">The menus the site declared, and the limit past which one is pruned.</param>
    /// <param name="context">The routed site.</param>
    let hook (options: DocsOptions) (context: HookContext) =
        let sections =
            [
                for section in options.Menus.Keys -> section, options.Menus[section]
                for outline in context.Site.ExtrasOf<MenuOutline>() do
                    if not (options.Menus.ContainsKey outline.Section) then
                        yield outline.Section, Menu.ofOutline outline.Items
            ]

        for section, items in sections do
            if options.MenuLimit > 0 && size items > options.MenuLimit then
                let json = json context.Site context.Pages items

                context.Write (path section) json |> ignore

[<RequireQualifiedAccess>]
module Sidebar =

    /// <summary>The menu of a section: what the site declared, else what a plugin offered.</summary>
    /// <remarks><c>None</c> when neither said anything, which is when a theme lists the section's
    /// pages instead.</remarks>
    /// <param name="options">The menus the site declared.</param>
    /// <param name="site">The site, which carries what plugins offered.</param>
    /// <param name="section">The section the menu is for.</param>
    let menuOf (options: DocsOptions) (site: SiteInfo) (section: string) =
        match Map.tryFind section options.Menus with
        | Some items -> Some items
        | None ->
            site.ExtrasOf<MenuOutline>()
            |> List.tryFind (fun outline -> outline.Section = section)
            |> Option.map (fun outline -> Menu.ofOutline outline.Items)

    /// <summary>Flatten a menu into the page order a reader walks through.</summary>
    /// <param name="all">Every page of the site.</param>
    /// <param name="items">The menu.</param>
    let rec pages (all: Page list) (items: MenuItem list) =
        items
        |> List.map _.Entry
        |> List.collect (
            function
            | MenuPage path -> Pages.find all path |> Option.toList
            | MenuGroup(path, items) -> (Pages.find all path |> Option.toList) @ pages all items
            | MenuSection(_, items) -> pages all items
            | MenuLink _ -> []
        )

    /// <summary>Whether a menu holds the page being read, so its trail can be open.</summary>
    let rec private holds (pages: Page list) (current: Page) (items: MenuItem list) =
        items
        |> List.map _.Entry
        |> List.exists (
            function
            | MenuPage path
            | MenuGroup(path, _) when
                match Pages.find pages path with
                | Some page -> page.Id = current.Id
                | None -> false
                ->
                true
            | MenuGroup(_, items)
            | MenuSection(_, items) -> holds pages current items
            | MenuPage _
            | MenuLink _ -> false
        )

    /// <summary>The menu as it is written into this page.</summary>
    /// <remarks>A group of thousands of pages would otherwise be written into every page of the
    /// site. Above the limit a group keeps the trail to the page being read and a link to its own
    /// page, which lists the rest.</remarks>
    let rec private pruned
        (site: SiteInfo)
        (current: Page)
        (pages: Page list)
        (limit: int)
        (items: MenuItem list)
        =
        items
        |> List.map (fun item ->
            match item.Entry with
            | MenuSection(label, children) ->
                { item with
                    Entry = MenuSection(label, pruned site current pages limit children)
                }
            | MenuGroup(path, children) when limit > 0 && List.length children > limit ->
                let kept = children |> List.filter (fun child -> holds pages current [ child ])
                let hidden = List.length children - List.length kept

                let rest =
                    match Pages.find pages path with
                    | Some page when hidden > 0 ->
                        [ Menu.link $"%i{hidden} more" (site.UrlOf page.Route) ]
                    | _ -> []

                { item with
                    Entry = MenuGroup(path, pruned site current pages limit kept @ rest)
                }
            | MenuGroup(path, children) ->
                { item with
                    Entry = MenuGroup(path, pruned site current pages limit children)
                }
            | MenuPage _
            | MenuLink _ -> item
        )

    let rec private nodes (scope: PageScope) (items: MenuItem list) : MenuNode list =
        let current = scope.Page

        items
        |> List.choose (fun item ->
            match item.Entry with
            | MenuLink(label, url) ->
                Some
                    {
                        Label = label
                        Url = Some url
                        Kind = MenuNodeKind.Link
                        Current = false
                        Open = false
                        Badge = item.Badge
                        Children = []
                    }
            | MenuPage path ->
                Pages.find scope.Pages path
                |> Option.map (fun page ->
                    {
                        Label = page.Title
                        Url = Some(scope.Site.UrlOf page.Route)
                        Kind = MenuNodeKind.Page
                        Current = page.Id = current.Id
                        Open = page.Id = current.Id
                        Badge = item.Badge
                        Children = []
                    }
                )
            | MenuGroup(path, children) ->
                Pages.find scope.Pages path
                |> Option.map (fun page ->
                    {
                        Label = page.Title
                        Url = Some(scope.Site.UrlOf page.Route)
                        Kind = MenuNodeKind.Group
                        Current = page.Id = current.Id
                        Open = page.Id = current.Id || holds scope.Pages current children
                        Badge = item.Badge
                        Children = nodes scope children
                    }
                )
            | MenuSection(label, children) ->
                Some
                    {
                        Label = label
                        Url = None
                        Kind = MenuNodeKind.Section
                        Current = false
                        Open = holds scope.Pages current children
                        Badge = item.Badge
                        Children = nodes scope children
                    }
        )

    /// <summary>The sidebar of a page: the section's menu with the trail to the page open.</summary>
    /// <remarks>
    /// <para>An explicit menu wins, then one a plugin offered; otherwise the section's own pages
    /// are listed in front-matter order.</para>
    /// <para>Resolved even when a page asks for no menu: the drawer the navbar's sections fold
    /// into on a narrow screen is part of it.</para>
    /// </remarks>
    /// <param name="options">The menus the site declared, and the limits past which one is pruned.</param>
    /// <param name="doc">What the page asked for: whether the menu offers a filter, and whether it
    /// remembers folding across pages.</param>
    /// <param name="scope">The page being rendered, and the site around it.</param>
    let resolve (options: DocsOptions) (doc: DocPage) (scope: PageScope) : SidebarModel =
        let section = Pages.section scope.Page
        let whole = menuOf options scope.Site section

        let prunes =
            match whole with
            | Some items -> options.MenuLimit > 0 && MenuStore.size items > options.MenuLimit
            | None -> false

        let declared =
            whole
            |> Option.map (fun items ->
                if prunes then
                    pruned scope.Site scope.Page scope.Pages options.MenuGroupLimit items
                else
                    items
            )

        let tree, source =
            match declared with
            | Some items ->
                let source =
                    if options.Menus.ContainsKey section then
                        MenuSource.Declared
                    else
                        MenuSource.Offered

                nodes scope items, source
            | None ->
                let listed =
                    Pages.ofSection scope
                    |> List.map (fun page ->
                        {
                            Label = page.Title
                            Url = Some(scope.Site.UrlOf page.Route)
                            Kind = MenuNodeKind.Page
                            Current = page.Id = scope.Page.Id
                            Open = page.Id = scope.Page.Id
                            Badge = None
                            Children = []
                        }
                    )

                listed, MenuSource.SectionPages

        let filtered =
            match doc.MenuFilter with
            | Some answer -> answer
            | None ->
                prunes
                || declared
                   |> Option.map (fun items -> MenuStore.size items > 30)
                   |> Option.defaultValue false

        {
            Drawer = (Navbar.resolve options scope).Start
            Tree = tree
            Source = source
            Filter =
                if filtered then
                    Some
                        {
                            SourceUrl =
                                if prunes then
                                    Some(scope.Site.UrlOfAsset(MenuStore.path section))
                                else
                                    None
                        }
                else
                    None
            Memory = doc.MenuMemory
            ShowMenu = doc.ShowMenu
            Section = section
        }
