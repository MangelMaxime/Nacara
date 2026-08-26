namespace Nacara.Plugins

open System
open System.Collections.Concurrent
open TextMateSharp.Grammars
open TextMateSharp.Registry
open Nacara.Core
open Nacara.Plugins.Internal

/// <summary>Options of the highlighting plugin.</summary>
/// <remarks>None yet: what it colours is decided by the grammars it ships, and a language it does
/// not know is reported by the engine, which knows the page the fence is on.</remarks>
type TextMateOptions =
    {
        Unused: unit
    }

[<RequireQualifiedAccess>]
module TextMate =

    let defaults =
        {
            Unused = ()
        }

    /// <summary>
    /// A highlighter backed by TextMate grammars.
    /// </summary>
    type TextMateHighlighter(options: TextMateOptions) =
        let registryOptions = RegistryOptions(ThemeName.LightPlus)
        let registry = Registry(registryOptions)
        let grammars = ConcurrentDictionary<string, IGrammar option>()

        let loadGate = obj ()

        let scopeOf (language: string) =
            registryOptions.GetAvailableLanguages()
            |> Seq.tryFind (fun candidate ->
                candidate.Id = language
                || (not (isNull candidate.Aliases) && candidate.Aliases |> Seq.contains language)
                || (not (isNull candidate.Extensions)
                    && candidate.Extensions |> Seq.contains ("." + language))
            )
            |> Option.bind (fun language ->
                match registryOptions.GetScopeByLanguageId language.Id with
                | null -> None
                | scope -> Some scope
            )

        let findGrammar (language: string) =
            grammars.GetOrAdd(
                language.ToLowerInvariant(),
                fun language ->
                    lock
                        loadGate
                        (fun () ->
                            scopeOf language
                            |> Option.bind (fun scope ->
                                match registry.LoadGrammar scope with
                                | null -> None
                                | grammar -> Some grammar
                            )
                        )
            )

        let tokenize (grammar: IGrammar) (lines: string array) =
            let mutable state: IStateStack = null

            lines
            |> Array.map (fun line ->
                let result = grammar.TokenizeLine(line, state, TimeSpan.FromSeconds 2.)
                state <- result.RuleStack

                result.Tokens
                |> Seq.map (fun token ->
                    let start = min token.StartIndex line.Length
                    let stop = min token.EndIndex line.Length

                    {
                        Text = line.Substring(start, max 0 (stop - start))
                        ClassName = Scopes.classify token.Scopes
                    }
                )
                |> Seq.filter (fun token -> token.Text <> "")
                |> List.ofSeq
            )
            |> List.ofArray

        interface IHighlighter with
            member _.Name = "textmate"

            member _.Highlight(language, code) =
                match language with
                | None -> None
                | Some language ->
                    match findGrammar language with
                    | None -> None
                    | Some grammar ->
                        let lines = code.Replace("\r\n", "\n").TrimEnd('\n').Split('\n')

                        // A grammar compiles its patterns lazily while tokenizing, so one cannot be shared across threads.
                        lock grammar (fun () -> Some(tokenize grammar lines))

    type private TextMatePlugin(options: TextMateOptions) =
        let highlighter = lazy (TextMateHighlighter(options) :> IHighlighter)

        interface IPlugin with
            member _.Name = "highlight-textmate"

            member _.Configure registry =
                registry |> Registry.extra highlighter.Value

    /// <summary>Syntax highlighting, with its default options.</summary>
    let create () = TextMatePlugin(defaults) :> IPlugin

    /// <summary>Syntax highlighting, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use. Anything it leaves alone keeps its default.</param>
    let createWith (configure: TextMateOptions -> TextMateOptions) =
        TextMatePlugin(configure defaults) :> IPlugin

    /// <summary>Add syntax highlighting to a site.</summary>
    let register (site: Site) = Site.plugin (create ()) site

    /// <summary>Add syntax highlighting to a site, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use. Anything it leaves alone keeps its default.</param>
    /// <param name="site">The site being described.</param>
    let registerWith (configure: TextMateOptions -> TextMateOptions) (site: Site) =
        Site.plugin (createWith configure) site
