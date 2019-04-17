[<AutoOpen>]
module rec Global

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import

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
            .Substring((sourceDir + "/").Length) // Remove the source directory info

let generateUrl (config : Types.Config) (pageContext : Types.PageContext) =
    pageContext.Path.Replace(config.Source + "/", "")
    |> Directory.join config.BaseUrl
    |> File.changeExtension "html"

module Helpers =

    open Fable.Import.Node
    open Fable.Import.Node.Globals

    let markdown (_:string) : string = importMember "./js/utils.js"

    /// Resolves a path to prevent using location of target JS file
    /// Note the function is inline so `__dirname` will belong to the calling file
    let inline resolve (path: string) =
        Exports.path.resolve(__dirname, path)

    /// Parses a React element invoking ReactDOMServer.renderToString
    let parseReact (el: React.ReactElement) =
        ReactDomServer.renderToString el

    /// Parses a React element invoking ReactDOMServer.renderToStaticMarkup
    let parseReactStatic (el: React.ReactElement) =
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
