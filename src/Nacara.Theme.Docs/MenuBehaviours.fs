namespace Nacara.Theme.Docs

open System.Reflection
open Nacara.Core
open Nacara.Theme.Core

/// <summary>The script of a documentation menu: the folds remembered from page to page, the
/// filter box, and the rest of a pruned menu grafted back.</summary>
/// <remarks>It reads the <c>data-nacara-menu-*</c> attributes, and the menus
/// <see cref="T:Nacara.Theme.Docs.MenuStore" /> writes.</remarks>
[<RequireQualifiedAccess>]
module MenuBehaviours =

    let private readResource = Resource.text (Assembly.GetExecutingAssembly())

    /// <summary>The script, and where it is served from.</summary>
    let script =
        lazy
            (let content = readResource "nacara-menu.js"
             Assets.fingerprint "nacara-menu" ".js" content, content)

    /// <summary>Register the script, written by <see cref="M:Nacara.Theme.Core.Shell.scripts" />
    /// at the end of every page.</summary>
    /// <param name="registry">What the theme is contributing to.</param>
    let register (registry: Registry) =
        let path, content = script.Value

        registry
        |> Registry.asset (WriteText(content, RelativePath.create path))
        |> Registry.extra (Script(path, true))
