namespace Nacara.Core

/// <summary>One page of a list split into pages.</summary>
type Paged<'Item> =
    {
        /// Which page this is, counting from 1.
        Number: int
        /// How many pages the list was split into. Always at least 1.
        PageCount: int
        /// The items on this page, in the list's order.
        Items: 'Item list
        /// How many items the whole list holds.
        ItemCount: int
        /// How many items come before this page.
        Offset: int
    }

    member this.Previous =
        if this.Number > 1 then
            Some(this.Number - 1)
        else
            None

    member this.Next =
        if this.Number < this.PageCount then
            Some(this.Number + 1)
        else
            None

/// <summary>A slot of a pager: the number of a page, or a gap standing for the pages left out.</summary>
type PagerSlot =
    | PageNumber of int
    | Gap

[<RequireQualifiedAccess>]
module Paging =

    /// <summary>Split a list into pages.</summary>
    /// <remarks>An empty list gives one page with no items, so an index always has a first page.</remarks>
    /// <param name="size">How many items a page holds.</param>
    /// <param name="items">The list, in the order the pages show it.</param>
    let split (size: int) (items: 'Item list) : Paged<'Item> list =
        if size < 1 then
            invalidArg (nameof size) $"A page holds at least one item, not %i{size}"

        let chunks =
            match List.chunkBySize size items with
            | [] -> [ [] ]
            | chunks -> chunks

        let count = List.length items

        chunks
        |> List.mapi (fun index chunk ->
            {
                Number = index + 1
                PageCount = List.length chunks
                Items = chunk
                ItemCount = count
                Offset = index * size
            }
        )

    /// <summary>The route of a page of an index.</summary>
    /// <example>
    /// <code lang="fsharp">
    /// Paging.route (Route.create locale [ "blog" ]) 1 // /blog/
    /// Paging.route (Route.create locale [ "blog" ]) 3 // /blog/page/3/
    /// </code>
    /// </example>
    /// <param name="first">The route of the first page.</param>
    /// <param name="number">Which page, counting from 1.</param>
    let route (first: Route) (number: int) =
        if number <= 1 then
            first
        else
            { first with
                Segments =
                    first.Segments
                    @ [
                        "page"
                        string number
                    ]
            }

    /// <summary>The slots of a pager around the current page.</summary>
    /// <param name="current">The page being shown, counting from 1.</param>
    /// <param name="total">How many pages there are.</param>
    let slots (current: int) (total: int) : PagerSlot list =
        if total <= 7 then
            [ for number in 1..total -> PageNumber number ]
        else
            let kept =
                Set.ofList
                    [
                        1
                        total
                        current - 1
                        current
                        current + 1
                        if current <= 4 then
                            yield! [ 2..5 ]
                        if current >= total - 3 then
                            yield! [ total - 4 .. total - 1 ]
                    ]

            [ 1..total ]
            |> List.fold
                (fun slots number ->
                    if kept.Contains number then
                        PageNumber number :: slots
                    else
                        match slots with
                        | Gap :: _ -> slots
                        | _ -> Gap :: slots
                )
                []
            |> List.rev
