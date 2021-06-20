module Export

open Fable.Core.JsInterop
open Types
open Fable.React
open Fable.Core

// We use importSideEffects so the files are included in the output by fable-splitter
// importSideEffects "./../../Nacara/js/markdown-it-anchored.js"
// importSideEffects "./../../Nacara/js/markdown-it-toc.js"

// type IExport =
//     abstract Default : Model * PageContext -> JS.Promise<ReactElement>
//     abstract Changelog : Model * PageContext -> JS.Promise<ReactElement>

// exportDefault
//     {
//         new IExport with
//             member __.Default (model, pageContext) =
//                 Layout.Standard.Default.toHtml model pageContext

//             member __.Changelog (model, pageContext) =
//                 Layout.Standard.Changelog.toHtml model pageContext
//     }

// Top module function are not mangled by Fable 3, so we can use them for export
// This avoid a warning from rollup when using `exportDefault`

type LayoutFunc = System.Func<Model, PageContext, JS.Promise<ReactElement>>

[<NoComparison>]
type LayoutExport =
    {
        RenderFunc : LayoutFunc
        ScriptDependencies : string list
        LayoutName : string
    }

let standard =
    {
        RenderFunc =
            System.Func<Model, PageContext, JS.Promise<ReactElement>>(fun model pageContext ->
                Layout.Standard.Default.toHtml model pageContext
            )
        ScriptDependencies =
            [
                Node.Api.path.join(__SOURCE_DIRECTORY__, "scripts/menu.js")
            ]
        LayoutName = "nacara-standard"
    }

let changelog =
    {
        RenderFunc =
            System.Func<Model, PageContext, JS.Promise<ReactElement>>(fun model pageContext ->
                Layout.Standard.Changelog.toHtml model pageContext
            )
        ScriptDependencies = [ ]
        LayoutName = "nacara-changelog"
    }
