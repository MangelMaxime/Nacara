namespace Nacara.Theme.Core

open Feliz.ViewEngine
open Nacara.Core
open Nacara.Theme

[<RequireQualifiedAccess>]
module ThemeCore =

    /// <summary>Register what every theme needs, documentation or not: the code-block renderer and
    /// the behaviours script.</summary>
    /// <param name="registry">What the theme is contributing to.</param>
    let configureBase (registry: Registry) =
        registry
        |> Registry.extra (NacaraCodeBlockRenderer() :> ICodeBlockRenderer)
        |> Registry.asset (Behaviours.asset ())
