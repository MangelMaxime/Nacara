namespace Nacara.Core

/// <summary>Writing direction of a locale.</summary>
type TextDirection =
    | LeftToRight
    | RightToLeft

    member this.HtmlValue =
        match this with
        | LeftToRight -> "ltr"
        | RightToLeft -> "rtl"

/// <summary>
/// A content locale.
/// </summary>
/// <remarks>
/// Exactly one locale of a site is the <em>root</em> locale: its pages are served without a
/// locale prefix (<c>/docs/guide/</c>), every other locale is prefixed (<c>/fr/docs/guide/</c>).
/// </remarks>
type Locale =
    {
        /// BCP-47 code, for example <c>en</c>, <c>fr</c> or <c>zh-CN</c>.
        Code: string
        /// Name shown to readers in the language picker.
        Label: string
        Direction: TextDirection
        IsRoot: bool
    }

[<RequireQualifiedAccess>]
module Locale =

    let private make isRoot (code: string) =
        {
            Code = code
            Label = code
            Direction = LeftToRight
            IsRoot = isRoot
        }

    /// <summary>The locale served from the base URL, with no prefix of its own.</summary>
    /// <param name="code">A language tag - <c>en</c>, <c>pt-BR</c>. Exactly one locale of a site
    /// is the root one.</param>
    let root code = make true code

    /// <summary>A locale served under a prefix of its code.</summary>
    /// <param name="code">A language tag - <c>fr</c>, <c>pt-BR</c> - which becomes the first
    /// segment of every route of that language.</param>
    let other code = make false code

    /// <summary>What the language picker calls it.</summary>
    /// <param name="label">The language's own name for itself: <c>Français</c>, not
    /// <c>French</c>.</param>
    /// <param name="locale">The locale being described.</param>
    let withLabel label (locale: Locale) =
        { locale with
            Label = label
        }

    /// <summary>The language reads right to left, and its pages say so.</summary>
    /// <param name="locale">The locale being described.</param>
    let rightToLeft (locale: Locale) =
        { locale with
            Direction = RightToLeft
        }

    /// <summary>What the locale contributes to a route: its code, or nothing at the root.</summary>
    /// <param name="locale">The locale to place.</param>
    let segments (locale: Locale) =
        if locale.IsRoot then
            []
        else
            [ locale.Code ]
