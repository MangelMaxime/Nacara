namespace Nacara.ApiGen.Render

open FSharp.Formatting.ApiDocs
open Markdown
open Giraffe.ViewEngine
open Nacara.ApiGen.CommentFormatter

module Union =

    let page
        (linkGenerator : string -> string)
        (apiDoc : ApiDocNamespace) =

        [
            InlineHtmlBlock (
                h2
                    [ _class "title is-3" ]
                    [ str apiDoc.Name ]
            )

        ]
