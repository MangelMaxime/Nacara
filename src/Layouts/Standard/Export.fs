module Export

open Fable.Core.JsInterop
open Types
open Fable.React
open Fable.Core

// We use importSideEffects so the files are included in the output by fable-splitter
importSideEffects "./../../Nacara/js/markdown-it-anchored.js"
importSideEffects "./../../Nacara/js/markdown-it-toc.js"

type IExport =
    abstract Default : Model * PageContext -> JS.Promise<ReactElement>
    abstract Changelog : Model * PageContext -> JS.Promise<ReactElement>

exportDefault
    {
        new IExport with
            member __.Default (model, pageContext) = Default.toHtml model pageContext
            member __.Changelog (model, pageContext) = Changelog.toHtml model pageContext
    }
