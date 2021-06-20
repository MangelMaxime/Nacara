[<AutoOpen>]
module rec Global

open Fable.React
open Fable.React.Props
open Fable.Core
open Fable.Core.JsInterop

[<Emit("require($0)")>]
let require<'T> (modulePath : string) : 'T = jsNative

let isNotNull (o : 'T) =
    not (isNull o)

let getFileId (sourceDir : string) (pageContext : Types.PageContext) =
    match pageContext.Attributes.Id with
    | Some id -> id
    | None ->
        let extensionPos = pageContext.Path.LastIndexOf('.')

        pageContext.Path
            .Substring(0, extensionPos) // Remove extension
            .Substring((sourceDir + Node.Api.path.sep).Length)
            .Replace("\\", "/") // Remove the source directory info


let generateUrl (config : Types.Config) (pageContext : Types.PageContext) =
    (pageContext.Path.Substring((config.Source + Node.Api.path.sep).Length)
        |> Directory.join config.BaseUrl
        |> File.changeExtension "html").Replace("\\", "/")

let highlightCode (lightnerConfig : JS.Map<string, CodeLightner.Config>) (text : string) =
    let codeBlockRegex =
        // Regex("""<pre\b[^>]*><code class="language-([^"]*)">(.*?)<\/code><\/pre>""", RegexOptions.Multiline ||| RegexOptions.Singleline)
        JS.Constructors.RegExp.Create("""<pre\b(?!class="skip-code-lightner-grammar-not-found")><code class="language-([^"]*)">(.*?)<\/code><\/pre>""", "gms")

    let rec apply (text : string) =
        promise {
            let m = codeBlockRegex.Match text
            if m.Success then
                let wholeText = m.Groups.[0].Value
                let lang = m.Groups.[1].Value

                let codeText =
                    m.Groups.[2].Value
                    |> Helpers.unEscapeHTML
                    // Escape single `$` caracter otherwise vscode-textmaste inject
                    // source code at `$` place.
                    |> (fun (str : string) -> str.Replace("$", "$$"))

                let grammarConfig = lightnerConfig.get lang

                if not (isNull (box grammarConfig)) then
                    let! formattedText = CodeLightner.lighten grammarConfig codeText
                    return! text.Replace(wholeText, formattedText)
                            |> apply
                else
                    Log.warnFn "No grammar found for language: `%s`" lang
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

module PageContext =

    let processCodeHighlights (lightnerConfig : JS.Map<string, CodeLightner.Config>) (pageContext : Types.PageContext) =
        promise {
            let! highlightedText = highlightCode lightnerConfig pageContext.Content
            return
                { pageContext with
                    Content = highlightedText
                }
        }

    let processMarkdown (model : Types.Model) (pageContext : Types.PageContext) =
        { pageContext with
            Content =
                Helpers.markdown pageContext.Content model.Config.Plugins.Markdown
        }
        |> processCodeHighlights model.LightnerCache

module Helpers =

    let markdown (_markdownString : string) (_plugins : Types.MarkdownPlugin array) : string = importMember "./../js/utils.js"

    /// Resolves a path to prevent using location of target JS file
    /// Note the function is inline so `__dirname` will belong to the calling file
    let inline resolve (path: string) =
        Node.Api.path.resolve(Node.Api.__dirname, path)

    /// Parses a React element invoking ReactDOMServer.renderToString
    let parseReact (el: ReactElement) =
        ReactDomServer.renderToString el

    /// Parses a React element invoking ReactDOMServer.renderToStaticMarkup
    let parseReactStatic (el: ReactElement) =
        ReactDomServer.renderToStaticMarkup el

    let unEscapeHTML (unsafe : string) =
        unsafe
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"")
            .Replace("&#039;", "'")


    // type DangerousInnerHtml =
    //     { __html : string }

    // let htmlFromMarkdown str =
    //     promise {
    //         let! html = makeHtml str
    //         return div [ DangerouslySetInnerHTML { __html = html } ] [ ]
    //     }

    // let contentFromMarkdown str =
    //     promise {
    //         let! html = makeHtml str
    //         return Content.content [ Content.Props [ DangerouslySetInnerHTML { __html = html } ] ]
    //             [ ]
    //     }

    let whitespace =
        span [ DangerouslySetInnerHTML { __html = " " } ]
            [ ]
