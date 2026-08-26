namespace Nacara.Plugins

/// <summary>How a live snippet colours itself once the reader is editing it.</summary>
type LiveExampleHighlighting =
    /// <summary>The grammar the rest of the site is coloured with.</summary>
    /// <remarks>An edited snippet looks exactly like the block it replaced, because it is the same
    /// grammar, the same queries and the same classes. Costs the grammar: 728 KB, fetched with the
    /// compiler and only when a reader asks to run something.</remarks>
    | TreeSitterHighlighting
    /// <summary>The editor's own F# mode.</summary>
    /// <remarks>Already inside the editor, so it costs nothing to add. Colours a little differently
    /// from the static blocks around it, which shows only while someone is typing.</remarks>
    | DefaultHighlighting

/// <summary>Which tab a snippet opens on once it has run.</summary>
type LiveExampleTab =
    /// What the snippet drew.
    | ResultTab
    /// What it printed.
    | ConsoleTab
    /// What Fable made of it.
    | OutputTab

/// <summary>What Fable compiles a snippet to.</summary>
/// <remarks>
/// <para>Only JavaScript can run: it is the one language a browser has a runtime for. The rest are
/// compiled and shown - the console carries what the compiler said, and the output tab carries the
/// code, which is the thing worth looking at when the question is "what does F# become here".</para>
/// <para>The compiler refuses a language it does not know rather than falling back, so a target it
/// would refuse fails the build instead of reaching a reader.</para>
/// </remarks>
type LiveExampleTarget =
    | JavaScript
    | TypeScript
    | Python
    | Rust
    | Dart
    | Php
    | Erlang

/// <summary>What each target is called, and what it can do.</summary>
[<RequireQualifiedAccess>]
module LiveExampleTarget =

    /// <summary>Everything known about a target, in one place so nothing drifts.</summary>
    /// <remarks>
    /// <c>Language</c> is the string the compiler is sent; <c>Highlight</c> is the grammar the
    /// output is coloured with, which is the same name a fence would write for that language.
    /// </remarks>
    type Description =
        {
            /// What a fence writes, and what the browser is told.
            Name: string
            /// What the tab is labelled.
            Label: string
            /// What the compiler is sent.
            Language: string
            /// The grammar its output is coloured with.
            Highlight: string
            /// Whether a browser can run what comes out.
            Runs: bool
            /// The other spellings a fence may use.
            Aliases: string list
        }

    let private describe target =
        let description name label language highlight runs aliases =
            {
                Name = name
                Label = label
                Language = language
                Highlight = highlight
                Runs = runs
                Aliases = aliases
            }

        match target with
        | JavaScript ->
            description "javascript" "JavaScript" "JavaScript" "javascript" true [ "js" ]
        | TypeScript ->
            description "typescript" "TypeScript" "TypeScript" "typescript" false [ "ts" ]
        | Python -> description "python" "Python" "Python" "python" false [ "py" ]
        | Rust -> description "rust" "Rust" "Rust" "rust" false [ "rs" ]
        | Dart -> description "dart" "Dart" "Dart" "dart" false []
        | Php -> description "php" "PHP" "Php" "php" false []
        | Erlang -> description "erlang" "Erlang" "Erlang" "erlang" false [ "beam" ]

    /// <summary>Every target, in the order they are worth reading.</summary>
    let all =
        [
            JavaScript
            TypeScript
            Python
            Rust
            Dart
            Php
            Erlang
        ]

    /// <summary>The grammar name each target's output is coloured with.</summary>
    let languages =
        all |> List.map (fun target -> (describe target).Highlight) |> List.distinct

    /// <summary>What this target is called and what it can do.</summary>
    /// <param name="target">The target.</param>
    let description target = describe target

    /// <summary>The target a fence named, if it named one that exists.</summary>
    /// <param name="name">What was written after <c>target=</c>.</param>
    let tryParse (name: string) =
        let wanted = name.Trim().ToLowerInvariant()

        all
        |> List.tryFind (fun target ->
            let described = describe target
            described.Name = wanted || List.contains wanted described.Aliases
        )

    /// <summary>Every spelling a fence may write, for saying what was expected.</summary>
    let spellings =
        all
        |> List.collect (fun target ->
            let described = describe target
            described.Name :: described.Aliases
        )

/// <summary>Files put in front of a snippet, so it can use a library without repeating it.</summary>
/// <remarks>They are compiled together with the snippet rather than referenced as an assembly,
/// which is what lets the type-checker answer for them: hovering a function defined in a preset
/// gives its real signature, and its full name.</remarks>
type LiveExamplePreset =
    {
        /// What a fence writes to ask for it.
        Name: string
        /// F# files, relative to the project root, in compilation order.
        Files: string list
        /// <summary>A stylesheet for what its snippets draw.</summary>
        /// <remarks>Unset, the site's own is used - and if there is none either, the frame is left
        /// with the browser's defaults.</remarks>
        Css: string option
        /// <summary>The page its snippets are run inside.</summary>
        /// <remarks>Unset, the site's own is used. A full HTML document: the import map and the
        /// snippet are put into its head and its body, so whatever it lays out is there before the
        /// snippet runs.</remarks>
        Template: string option
        /// <summary>An F# project whose references and code a snippet can use.</summary>
        /// <remarks>Everything it references and everything it declares reaches the snippets: a
        /// site that wants to offer more than one package, or helpers of its own alongside them,
        /// writes a project instead of listing packages here.</remarks>
        Project: string option
        /// Whether a fence naming no preset gets this one.
        IsDefault: bool
    }

/// <summary>What a preset is made of, one line at a time.</summary>
/// <remarks>
/// Built as a value and handed to <see cref="M:Nacara.Plugins.LiveExample.preset" />, rather than
/// declared and then added to by name: there is no name to mistype, and nothing that quietly does
/// nothing when you do.
/// </remarks>
[<RequireQualifiedAccess>]
module LiveExamplePreset =

    /// <summary>A preset a fence can ask for.</summary>
    /// <param name="name">What a fence writes: <c>preset=name</c>.</param>
    let create name =
        {
            Name = name
            Files = []
            Css = None
            Template = None
            Project = None
            IsDefault = false
        }

    /// <summary>The F# put in front of a snippet, in the order it compiles.</summary>
    /// <remarks>Small is the point: these are type-checked on every compile, so a preset is the
    /// <c>open</c> lines and a helper or two rather than a library.</remarks>
    /// <param name="value">Files relative to the project root.</param>
    /// <param name="preset">The preset so far.</param>
    let files value (preset: LiveExamplePreset) =
        { preset with
            Files = value
        }

    /// <summary>Give it an F# project, and everything in it reaches the snippets.</summary>
    /// <remarks>
    /// <para>The project is compiled once, ahead of time, with every package it references - so a
    /// site can offer a set of libraries and its own helpers together, and maintain them as an
    /// ordinary project that its own build type-checks.</para>
    /// <para>It is rebuilt when anything in it changes, with the Fable the compiler in the browser
    /// was built from - fetched for the purpose, so nothing has to be installed.</para>
    /// </remarks>
    /// <param name="value">The <c>.fsproj</c>, relative to the project root.</param>
    /// <param name="preset">The preset so far.</param>
    let project value (preset: LiveExamplePreset) =
        { preset with
            Project = Some value
        }

    /// <summary>A stylesheet for what its snippets draw.</summary>
    /// <remarks>Inside the frame only: it never reaches the page around it.</remarks>
    /// <param name="value">A CSS file, relative to the project root.</param>
    /// <param name="preset">The preset so far.</param>
    let css value (preset: LiveExamplePreset) =
        { preset with
            Css = Some value
        }

    /// <summary>The page its snippets run inside.</summary>
    /// <remarks>A full HTML document. The import map and the snippet are put into its head and its
    /// body, so whatever it lays out is there before the snippet runs.</remarks>
    /// <param name="value">An HTML file, relative to the project root.</param>
    /// <param name="preset">The preset so far.</param>
    let template value (preset: LiveExamplePreset) =
        { preset with
            Template = Some value
        }

    /// <summary>The one a fence naming no preset gets.</summary>
    /// <remarks>Say it on one preset. Two of them is a build error, because there is no sensible
    /// way to choose between them and guessing would be the wrong library on a page.</remarks>
    /// <param name="preset">The preset so far.</param>
    let asDefault (preset: LiveExamplePreset) =
        { preset with
            IsDefault = true
        }

/// <summary>Options for <see cref="T:Nacara.Plugins.LiveExample" />.</summary>
type LiveExampleOptions =
    {
        /// The presets a fence can name.
        Presets: LiveExamplePreset list
        /// <summary>The preset used by a fence that names none.</summary>
        /// <remarks>Unset means a bare snippet compiles on its own, which is what a site with one
        /// library and one prelude usually does not want.</remarks>
        /// How an edited snippet colours itself.
        Highlighting: LiveExampleHighlighting
        /// Which build of the Fable compiler snippets are compiled by.
        Fable: FableRelease
        /// <summary>Which tab a snippet opens on once it has run.</summary>
        /// <remarks>Unset, it opens on the console, or on the result when the snippet drew
        /// something and printed nothing. A snippet that failed to compile always opens on the
        /// console, whatever this says: the errors are there.</remarks>
        Tab: LiveExampleTab option
        /// <summary>What a snippet is compiled to when its fence does not say.</summary>
        /// <remarks>Unset means JavaScript, the one target a reader's browser can run.</remarks>
        Target: LiveExampleTarget option
        /// <summary>A stylesheet for what snippets draw, for presets that name none.</summary>
        Css: string option
        /// <summary>The page snippets are run inside, for presets that name none.</summary>
        Template: string option
        /// <summary>Whether a snippet shows what its compiles cost.</summary>
        /// <remarks>A tab of timings, one row per run, kept across runs so a change can be
        /// compared with what came before it. For writing a site rather than reading one.</remarks>
        Stats: bool
        /// <summary>The Fable CLI used to precompile a preset's packages.</summary>
        /// <remarks>Unset means a package is compiled with every snippet instead, which works and
        /// costs the reader a second or two on the first run of a page.</remarks>
        FableTool: string option
        /// <summary>Grammars for colouring output this site's targets produce.</summary>
        /// <remarks>The highlighting plugin ships the languages a documentation site is written in,
        /// which is not the same set as the languages Fable emits. Name a grammar here and the
        /// output tab for that target is coloured; name none and it is shown as plain text.</remarks>
        OutputGrammars: TreeSitterGrammar list
    }
