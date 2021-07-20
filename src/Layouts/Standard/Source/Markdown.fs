module Markdown

open Types
open Fable.Core.JsInterop

let private messageBlock (level : string) =
    {|
        validate = fun (info : string) ->
            info.Trim().StartsWith(level)

        render = fun tokens idx ->
            // Opening tag
            if tokens?(idx)?nesting = 1 then
                let fullText : string = tokens?(idx)?info

                let titlePart = fullText.Trim().Substring(level.Length)

                if System.String.IsNullOrEmpty titlePart then
                    $"""<article class="message is-%s{level}">
                    <div class="message-body">"""
                else
                    $"""<article class="message is-%s{level}">
                    <div class="message-header">
                        <p>%s{titlePart}</p>
                    </div>
                    <div class="message-body">"""

            // Closing tag
            else
                "</div>\n</article>\n"
    |}

let addMessageBlockPlugin (level : string) (md : MarkdownIt) =
    emitJsExpr
        (md, level, messageBlock level)
        """
$0.use(require("markdown-it-container"), $1, $2)
        """


let configure (md : MarkdownIt) =
    md
    |> addMessageBlockPlugin "primary"
    |> addMessageBlockPlugin "info"
    |> addMessageBlockPlugin "success"
    |> addMessageBlockPlugin "warning"
    |> addMessageBlockPlugin "danger"
