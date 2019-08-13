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

module Helpers =

    let markdown (_:string) : string = importMember "./js/utils.js"

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

    let partition (xs: 'T[]) (f: 'T->Choice<'TO1, 'TO2>) =
        let o1 = ResizeArray()
        let o2 = ResizeArray()
        for x in xs do
            match f x with
            | Choice1Of2 x -> o1.Add(x)
            | Choice2Of2 x -> o2.Add(x)
        o1.ToArray(), o2.ToArray()

    let resultPartition xs =
        partition xs (function Ok x -> Choice1Of2 x | Error x -> Choice2Of2 x)
