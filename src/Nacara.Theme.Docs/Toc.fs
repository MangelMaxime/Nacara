namespace Nacara.Theme.Docs

open Nacara.Theme
open Nacara.Theme.Core

[<RequireQualifiedAccess>]
module Toc =

    /// <summary>The headings of a page, when it shows them.</summary>
    /// <remarks><c>None</c> when the page asked for no table of contents, or has no heading to
    /// list.</remarks>
    /// <param name="doc">What the page asked for.</param>
    /// <param name="scope">The page being rendered.</param>
    let resolve (doc: DocPage) (scope: PageScope) : TocModel option =
        if doc.ShowToc && not (List.isEmpty scope.Page.Headings) then
            Some
                {
                    Headings = scope.Page.Headings
                }
        else
            None
