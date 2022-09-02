namespace Nacara

module Log =

    open System
    open Spectre.Console

    let info (msg : string) =
        AnsiConsole.MarkupLine($"{msg}")

    let error (msg : string) =
        AnsiConsole.MarkupLine($"[red]{msg}[/]")

    let warn (msg : string) =
        AnsiConsole.MarkupLine($"[yellow]{msg}[/]")

    let debug (msg : string) =
        AnsiConsole.MarkupLine($"[grey93]{msg}[/]")

    let success (msg : string) =
        AnsiConsole.MarkupLine($"[green]{msg}[/]")
