[<AutoOpen>]
module rec Global

open Fable.Core
open Fable.Core.JsInterop
open Node
open Nacara.Core.Types
open Thoth.Json

[<RequireQualifiedAccess>]
module ExitCode =

    [<Literal>]
    let OK = 0

    [<Literal>]
    let MISSING_CONFIG_FILE = 1

    [<Literal>]
    let INVALID_CONFIG_FILE = 2

    [<Literal>]
    let INVALID_MARKDOWN_FILE_IN_BUILD_MODE = 3

    [<Literal>]
    let COMPLETED_WITH_ERROR = 4

    [<Literal>]
    let FAILED_TO_LOAD_BABEL = 5

[<RequireQualifiedAccess>]
module Cmd =

    open Elmish

    module OfFunc =

        let exec (task: 'a -> _) (arg: 'a) : Cmd<'msg> =
            let bind dispatch =
                try
                    task arg
                    |> ignore
                with x ->
                    ()
            [ bind ]

let isNotNull (o : 'T) =
   not (isNull o)

let getPageId (filePath : string) =
    let extensionPos = filePath.LastIndexOf('.')

    filePath
        .Substring(0, extensionPos) // Remove extension
        .Replace("\\", "/") // Normalize segments separator

let unEscapeHTML (unsafe : string) =
    unsafe
        .Replace("&amp;", "&")
        .Replace("&lt;", "<")
        .Replace("&gt;", ">")
        .Replace("&quot;", "\"")
        .Replace("&#039;", "'")

let highlightCode (lightnerConfig : JS.Map<string, CodeLightner.Config>) (text : string) =
    let codeBlockRegex =
        JS.Constructors.RegExp.Create("""<pre\b(?!class="skip-code-lightner-grammar-not-found")><code class="language-([^"]*)">(.*?)<\/code><\/pre>""", "gms")

    let rec apply (text : string) =
        promise {
            let m = codeBlockRegex.Match text
            if m.Success then
                let wholeText = m.Groups.[0].Value
                let lang = m.Groups.[1].Value

                let codeText =
                    m.Groups.[2].Value
                    |> unEscapeHTML
                    // Escape single `$` character otherwise vscode-textmate inject
                    // source code at `$` place.
                    |> (fun (str : string) -> str.Replace("$", "$$"))

                let grammarConfig = lightnerConfig.get lang

                if not (isNull (box grammarConfig)) then
                    let! formattedText = CodeLightner.lighten grammarConfig codeText
                    return! text.Replace(wholeText, formattedText)
                            |> apply
                else
                    Log.warn $"No grammar found for language: `%s{lang}`"
                    let replacement =
                        wholeText.Replace("<pre", """<pre class="skip-code-lightner-grammar-not-found" """)

                    return! text.Replace(wholeText, replacement)
                            |> apply
            else
                return text
        }

    promise {
        return! apply text
    }

let markdownToHtml (lightnerConfig : JS.Map<string, CodeLightner.Config>) (markdownText : string) =
    let md =
        emitJsExpr ()
            """require('markdown-it')({
    html: true
})
.use(require('./../js/markdown-it-anchored'))
            """

    promise {
        let htmlText = md?render(markdownText)

        return! highlightCode lightnerConfig htmlText
    }

let markdownToHtmlWithPlugins (lightnerConfig : JS.Map<string, CodeLightner.Config>) (init : MarkdownIt -> MarkdownIt) (markdownText : string) =
    let md : MarkdownIt =
        emitJsExpr ()
            """require('markdown-it')({
    html: true
})
.use(require('./../js/markdown-it-anchored'))
            """

    let md = init md

    promise {
        let htmlText = md.render(markdownText)

        return! highlightCode lightnerConfig htmlText
    }

let initPageContext (sourceFolder : string) (filePath : string) =
    promise {
        let fullFilePath =
            path.join(sourceFolder, path.sep, filePath)

        let! fileContent = File.read fullFilePath
        let fm = FrontMatter.fm.Invoke(fileContent)

        let segments =
            path.normalize(filePath).Split(char path.sep)

        let section =
            // Menu.json is at the root level of the sourceFolder let's make its section empty for now
            if segments.Length = 1 then
                ""
            else
                segments.[0]

        let commonInfoDecoder =
            Decode.object (fun get ->
                {|
                    Layout = get.Required.Field "layout" Decode.string
                    Title = get.Optional.Field "title" Decode.string
                |}
            )

        match Decode.fromValue "$" commonInfoDecoder fm.attributes with
        | Ok commonInfo ->
            return Ok {
                PageId = getPageId filePath
                RelativePath = filePath
                FullPath = fullFilePath
                Content = fm.body
                Layout = commonInfo.Layout
                Title = commonInfo.Title
                Section = section
                Attributes = fm.attributes
            }

        | Error errorMessage ->
            return Error $"One property is missing from %s{filePath}.\n%s{errorMessage}"
    }

let (|MarkdownFile|JavaScriptFile|SassFile|MenuFile|OtherFile|) (filePath : string) =
    let ext = path.extname(filePath)

    match ext.ToLower() with
    | ".md" -> MarkdownFile
    | ".js" -> JavaScriptFile
    | ".scss" | ".sass" -> SassFile
    | _ ->
        if path.basename(filePath) = "menu.json" then
            MenuFile
        else
            OtherFile ext

let initMenuFiles (sourceFolder : string) (filePath : string) =
    promise {
        let fullFilePath =
            path.join(sourceFolder, path.sep, filePath)

        let! fileContent = File.read fullFilePath

        let segments =
            path.normalize(filePath).Split(char path.sep)

        let section =
            // Menu.json is at the root level of the sourceFolder let's make its section empty for now
            if segments.Length = 1 then
                ""
            else
                segments.[0]

        match Decode.fromString Menu.decoder fileContent with
        | Ok items ->

            return Ok {
                Section = section
                Items = items
            }

        | Error errorMessage ->
            return Error $"Error while reading %s{filePath}\n%s{errorMessage}"
    }
