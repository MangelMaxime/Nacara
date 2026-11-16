namespace Nacara.Theme.Docs

open Nacara.Core
open Nacara.Theme
open Nacara.Theme.Core

[<RequireQualifiedAccess>]
module EditLink =

    /// <summary>Where a page is edited at its source.</summary>
    /// <remarks><c>None</c> unless the site said where its repository is, and the page came from a
    /// file.</remarks>
    /// <param name="options">The options, read for the base of the url.</param>
    /// <param name="scope">The page being rendered.</param>
    let resolve (options: DocsOptions) (scope: PageScope) : EditLinkModel option =
        match options.EditUrlBase, scope.Page.ProjectPath with
        | Some editBase, Some path ->
            Some
                {
                    Url = editBase.TrimEnd '/' + "/" + RelativePath.value path
                }
        | _ -> None
