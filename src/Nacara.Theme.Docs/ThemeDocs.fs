namespace Nacara.Theme.Docs

open Feliz.ViewEngine
open Nacara.Core
open Nacara.Theme
open Nacara.Theme.Core

/// <summary>What a theme registers of the shared machinery.</summary>
type ThemeRegistration =
    {
        /// The theme's name, which is what a page it generates says generated it.
        Origin: string
        Docs: DocsOptions
        /// The layout the 404 page is rendered with. <c>None</c> writes no 404 page.
        NotFound: (PageContext<DocFrontMatter> -> ReactElement) option
    }

[<RequireQualifiedAccess>]
module ThemeDocs =

    /// <summary>Register what every documentation theme needs of the site.</summary>
    /// <remarks>
    /// What <see cref="M:Nacara.Theme.Core.ThemeCore.configureBase" /> registers, the menus written
    /// whole for the filter box, the script of the menu, and the 404 page. Call it from the theme's own plugin, then add
    /// the theme's assets.
    /// </remarks>
    /// <param name="registration">What the theme is, and what it was configured with.</param>
    /// <param name="registry">What the theme is contributing to.</param>
    let configure (registration: ThemeRegistration) (registry: Registry) =
        registry
        |> ThemeCore.configureBase
        |> Registry.onPagesRouted (MenuStore.hook registration.Docs)
        |> Registry.preserve MenuStore.preserved
        |> MenuBehaviours.register
        |> fun registry ->
            match registration.NotFound with
            | Some render ->
                Registry.onBuildComplete (NotFound.hook registration.Origin render) registry
            | None -> registry
