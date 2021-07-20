[<AutoOpen>]
module rec Global

open Fable.React
open Fable.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Node
open Types
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

    module OfPromise =

        /// Command to dispatch the `promise` result
        let exec
            (task: 'a -> Fable.Core.JS.Promise<_>)
            (arg:'a) =

            let bind dispatch =
                (task arg)
                |> Promise.start

            [ bind ]

//
//[<Emit("require($0)")>]
//let require<'T> (modulePath : string) : 'T = jsNative

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

module List =
    // Copied from F# plus
    // https://github.com/fsprojects/FSharpPlus/blob/327cdfcff9d7a209bf934218a5067301ef44e35d/src/FSharpPlus/Extensions/List.fs#L133-133

    /// <summary>
    /// Creates two lists by applying the mapping function to each element in the list
    /// and classifying the transformed values depending on whether they were wrapped with Choice1Of2 or Choice2Of2.
    /// </summary>
    /// <returns>
    /// A tuple with both resulting lists.
    /// </returns>
    let partitionMap (mapping: 'T -> Choice<'T1,'T2>) (source: list<'T>) =
        let rec loop ((acc1, acc2) as acc) = function
            | [] -> acc
            | x::xs ->
                match mapping x with
                | Choice1Of2 x -> loop (x::acc1, acc2) xs
                | Choice2Of2 x -> loop (acc1, x::acc2) xs
        loop ([], []) (List.rev source)

module Array =
    // Copied from F# plus
    //https://github.com/fsprojects/FSharpPlus/blob/327cdfcff9d7a209bf934218a5067301ef44e35d/src/FSharpPlus/Extensions/Array.fs#L100-100

    /// <summary>
    /// Creates two arrays by applying the mapper function to each element in the array
    /// and classifies the transformed values depending on whether they were wrapped with Choice1Of2 or Choice2Of2.
    /// </summary>
    /// <returns>
    /// A tuple with both resulting arrays.
    /// </returns>
    let partitionMap (mapper: 'T -> Choice<'T1,'T2>) (source: array<'T>) =
        let (x, y) = ResizeArray (), ResizeArray ()
        Array.iter (mapper >> function Choice1Of2 e -> x.Add e | Choice2Of2 e -> y.Add e) source
        x.ToArray (), y.ToArray ()

//
//module PageContext =
//
//    let processCodeHighlights (lightnerConfig : JS.Map<string, CodeLightner.Config>) (pageContext : Types.PageContext) =
//        promise {
//            let! highlightedText = highlightCode lightnerConfig pageContext.Content
//            return
//                { pageContext with
//                    Content = highlightedText
//                }
//        }
//
//    let processMarkdown (model : Types.Model) (pageContext : Types.PageContext) =
//        { pageContext with
//            Content =
//                Helpers.markdown pageContext.Content model.Config.Plugins.Markdown
//        }
//        |> processCodeHighlights model.LightnerCache
//
//module Helpers =
//
//    let markdown (_markdownString : string) (_plugins : Types.MarkdownPlugin array) : string = importMember "./../js/utils.js"
//
//    /// Resolves a path to prevent using location of target JS file
//    /// Note the function is inline so `__dirname` will belong to the calling file
//    let inline resolve (path: string) =
//        Node.Api.path.resolve(Node.Api.__dirname, path)
//
//    /// Parses a React element invoking ReactDOMServer.renderToString
//    let parseReact (el: ReactElement) =
//        ReactDomServer.renderToString el
//
//    /// Parses a React element invoking ReactDOMServer.renderToStaticMarkup
//    let parseReactStatic (el: ReactElement) =
//        ReactDomServer.renderToStaticMarkup el
//


//
//    // type DangerousInnerHtml =
//    //     { __html : string }
//
//    // let htmlFromMarkdown str =
//    //     promise {
//    //         let! html = makeHtml str
//    //         return div [ DangerouslySetInnerHTML { __html = html } ] [ ]
//    //     }
//
//    // let contentFromMarkdown str =
//    //     promise {
//    //         let! html = makeHtml str
//    //         return Content.content [ Content.Props [ DangerouslySetInnerHTML { __html = html } ] ]
//    //             [ ]
//    //     }
//
//    let whitespace =
//        span [ DangerouslySetInnerHTML { __html = " " } ]
//            [ ]

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
