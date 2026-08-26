namespace Nacara.Plugins.Internal

open Nacara.Core
open Nacara.Plugins

/// <summary>What a fence asked for, and whether the compiler could honour it.</summary>
[<RequireQualifiedAccess>]
module internal LiveExampleFences =

    /// <summary>Report a fence naming a target the compiler would refuse.</summary>
    let check =
        {
            Name = "live-example-target"
            Check =
                fun context ->
                    let meta = context.Block.Meta.Unknown

                    if List.contains "live" meta then
                        for token in meta do
                            if token.StartsWith "target=" then
                                let named = token.Substring(7).Trim('"', '\'')

                                if (LiveExampleTarget.tryParse named).IsNone then
                                    let spellings = String.concat ", " LiveExampleTarget.spellings

                                    let diagnostic =
                                        Diagnostic.error
                                            "unknown-target"
                                            $"No target called '%s{named}'"
                                        |> Diagnostic.withHint $"Fable compiles to %s{spellings}"

                                    match context.Source with
                                    | Some file ->
                                        context.Diagnostics.Add(
                                            diagnostic |> Diagnostic.at file context.Line 1
                                        )
                                    | None -> context.Diagnostics.Add diagnostic
        }
