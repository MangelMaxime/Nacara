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

let standard = System.Func<Model, PageContext, JS.Promise<ReactElement>>(fun model pageContext ->
        Layout.Standard.Default.toHtml model pageContext
    )

let changelog = System.Func<Model, PageContext, JS.Promise<ReactElement>>(fun model pageContext ->
        Layout.Standard.Changelog.toHtml model pageContext
    )

let navbarOnly = System.Func<Model, PageContext, JS.Promise<ReactElement>>(fun model pageContext ->
        Layout.Standard.NavbarOnly.toHtml model pageContext
    )

let basePage = System.Func<Model, string option, ReactElement, ReactElement>(fun model pageTitle content ->
        Prelude.basePage model pageTitle content
    )

let processMarkdown = System.Func<Model, PageContext, JS.Promise<PageContext>>(fun model pageContext ->
        PageContext.processMarkdown model pageContext
    )
