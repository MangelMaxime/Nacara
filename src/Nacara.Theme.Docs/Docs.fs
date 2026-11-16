namespace Nacara.Theme.Docs

open Feliz.ViewEngine
open Nacara.Core
open Nacara.Theme
open Nacara.Theme.Core

/// <summary>A documentation collection over the shared front matter.</summary>
[<RequireQualifiedAccess>]
module Docs =

    /// <summary>A collection reading <see cref="T:Nacara.Theme.DocFrontMatter" /> with a decoder
    /// of your own, for a theme knowing layouts of its own.</summary>
    /// <param name="decoder">How the front matter is read - usually
    /// <see cref="M:Nacara.Theme.DocFrontMatter.decoderWith" />.</param>
    /// <param name="name">What the collection is called, which is also the directory its content
    /// is read from.</param>
    /// <param name="layout">What renders its pages.</param>
    let collectionWith
        (decoder: Decoder<DocFrontMatter>)
        (name: string)
        (layout: PageContext<DocFrontMatter> -> ReactElement)
        =
        Collection.create name decoder
        |> Collection.sourceAll name
        |> Collection.title _.Title
        |> Collection.order (fun frontMatter -> frontMatter.Order |> Option.defaultValue 0)
        |> Collection.toc (fun frontMatter ->
            match frontMatter.Toc with
            | Some(TocLevels range) -> Some range
            | Some TocOff
            | None -> None
        )
        |> Collection.layout layout

    /// <summary>A collection reading <see cref="T:Nacara.Theme.DocFrontMatter" />.</summary>
    /// <param name="name">What the collection is called, which is also the directory its content
    /// is read from.</param>
    /// <param name="layout">What renders its pages.</param>
    let collection (name: string) (layout: PageContext<DocFrontMatter> -> ReactElement) =
        collectionWith DocFrontMatter.decoder name layout
