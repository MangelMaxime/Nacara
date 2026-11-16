namespace Nacara.Core

open System

/// <summary>A term of a taxonomy, such as a tag, and the items filed under it.</summary>
type Term<'Item> =
    {
        /// As the items wrote it.
        Name: string
        /// <summary>As a route writes it, from <see cref="M:Nacara.Core.Slug.create" />.</summary>
        Slug: string
        /// In the order they were given.
        Items: 'Item list
    }

[<RequireQualifiedAccess>]
module Taxonomy =

    /// <example>
    /// <code lang="fsharp">
    /// Collection.pagesOf posts pages |> Taxonomy.group _.FrontMatter.Tags
    /// </code>
    /// </example>
    let group (termsOf: 'Item -> string list) (items: 'Item list) : Term<'Item> list =
        items
        |> List.collect (fun item ->
            termsOf item
            |> List.distinctBy Slug.create
            |> List.map (fun name -> Slug.create name, name, item)
        )
        |> List.groupBy (fun (slug, _, _) -> slug)
        |> List.map (fun (slug, filed) ->
            let _, name, _ = List.head filed

            {
                Name = name
                Slug = slug
                Items = filed |> List.map (fun (_, _, item) -> item)
            }
        )
        |> List.sortWith (fun a b ->
            String.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)
        )
