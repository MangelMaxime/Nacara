namespace Nacara.Plugins.Internal

/// <summary>
/// Maps TextMate scopes onto Nacara's token classes.
/// </summary>
/// <remarks>
/// Classes, never colours: one rendering serves every theme, light and dark switch at runtime with
/// no rebuild, and a theme is plain CSS variables over this vocabulary.
/// </remarks>
[<RequireQualifiedAccess>]
module Scopes =

    /// Longest prefix wins, so "entity.name.function" beats "entity.name".
    let private table =
        [
            "comment", "tok-comment"
            "string", "tok-string"
            "constant.numeric", "tok-number"
            "constant.character.escape", "tok-escape"
            "constant.language", "tok-constant"
            "constant", "tok-constant"
            "keyword.operator", "tok-operator"
            "keyword", "tok-keyword"
            "storage.type", "tok-keyword"
            "storage.modifier", "tok-keyword"
            "storage", "tok-keyword"
            "entity.name.function", "tok-function"
            "entity.name.type", "tok-type"
            "entity.name.class", "tok-type"
            "entity.name.namespace", "tok-namespace"
            "entity.name.tag", "tok-tag"
            "entity.other.attribute-name", "tok-attribute"
            "entity.name", "tok-type"
            "support.function", "tok-function"
            "support.class", "tok-type"
            "support.type", "tok-type"
            "support.constant", "tok-constant"
            "support.variable", "tok-variable"
            "variable.parameter", "tok-parameter"
            "variable.language", "tok-keyword"
            "variable", "tok-variable"
            "meta.preprocessor", "tok-preprocessor"
            "punctuation.definition.tag", "tok-tag"
            "punctuation", "tok-punctuation"
            "invalid", "tok-invalid"
            "markup.bold", "tok-bold"
            "markup.italic", "tok-italic"
            "markup.heading", "tok-heading"
            "markup.inserted", "tok-inserted"
            "markup.deleted", "tok-deleted"
        ]
        |> List.sortByDescending (fun (scope, _) -> scope.Length)

    /// <summary>The class for a token, from the most specific scope TextMate gave it.</summary>
    /// <param name="scopes">The scopes covering the token, outermost first, as the grammar
    /// assigned them - <c>source.fsharp</c>, <c>keyword.control.fsharp</c>.</param>
    /// <returns>One of the theme's token classes, or nothing when no scope maps to one - which is
    /// how a token stays the colour of ordinary text.</returns>
    let classify (scopes: string seq) =
        scopes
        // TextMate lists scopes outermost first; the last one is the most specific.
        |> Seq.rev
        |> Seq.tryPick (fun scope ->
            table
            |> List.tryPick (fun (prefix, className) ->
                if scope = prefix || scope.StartsWith(prefix + ".") then
                    Some className
                else
                    None
            )
        )
