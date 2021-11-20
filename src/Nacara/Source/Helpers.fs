[<AutoOpen>]
module rec Global

open Fable.Core
open Fable.Core.JsInterop
open Node
open Nacara.Core.Types
open Thoth.Json
open System

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
    let INVALID_LAYOUT_SCRIPT = 5

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

let getPartialId (filePath : string) =
    let filePath = filePath.Replace("_partials/", "")

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


let initPageContextFromContent (fileContent : string) (fullFilePath : string) (relativeFilePath : string) =
    promise {
        let fm = FrontMatter.fm.Invoke(fileContent)

        let segments =
            path.normalize(relativeFilePath).Split(char path.sep)

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
                PageId = getPageId relativeFilePath
                RelativePath = relativeFilePath
                FullPath = fullFilePath
                Content = fm.body
                Layout = commonInfo.Layout
                Title = commonInfo.Title
                Section = section
                Attributes = fm.attributes
            }

        | Error errorMessage ->
            return Error $"One property is missing from %s{relativeFilePath}.\n%s{errorMessage}"
    }

let initPageContextFromFile (sourceFolder : string) (filePath : string) =
    promise {
        let fullFilePath =
            path.join(sourceFolder, path.sep, filePath)

        let! fileContent = File.read fullFilePath

        return! initPageContextFromContent fileContent fullFilePath filePath
    }

let (|MarkdownFile|JavaScriptFile|PartialFile|FsharpFile|SassFile|MenuFile|OtherFile|) (filePath : string) =
    let ext = path.extname(filePath)

    if filePath.StartsWith("_partials") then
        PartialFile
    else
        match ext.ToLower() with
        | ".md" -> MarkdownFile
        | ".js" ->
            JavaScriptFile
        | ".scss" | ".sass" -> SassFile
        | ".fs" | ".fsx" -> FsharpFile
        | _ ->
            if path.basename(filePath) = "menu.json" then
                MenuFile
            else
                OtherFile ext

let (|Js|Jsx|Other|) (filePath : string) =
    let ext = path.extname(filePath)

    match ext.ToLower() with
    | ".js" -> Js
    | ".jsx" -> Jsx
    | _ -> Other ext

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

module Server =

    open Glutinum.Express
    open Glutinum.ExpressServeStaticCore

    // Extends Http binding to accept variant with an express application
    type Http.IExports with
        [<Emit("$0.createServer($1,$2)")>]

        member __.createServer (expressApp : Express.Express) : Http.Server = jsNative

    let create (config : Config) =
        let app = express.express()

        if config.SiteMetadata.BaseUrl <> "/" then
            app.``use``(fun (req : Request) (res : Response) (next : NextFunction) ->
                let segments = req.url.Split('/').[1..]
                let sanitizeBaseUrl = config.SiteMetadata.BaseUrl.Replace("/", "")
                if segments.Length > 1 && segments.[0] = sanitizeBaseUrl then
                    let newUrl = System.String.Join("/", segments.[1..])
                    res.writeHead(
                        307,
                        {| Location = "http://" + req.headers?host + "/" + newUrl |}
                    )
                    res.``end``()
                else
                    next.Invoke()
            )

        let serveStaticRouter = express.``static``.Invoke(path.join(config.WorkingDirectory, config.Output)) :?> Router

        app.``use``(serveStaticRouter)

        http.createServer(app)

module String =

    let normalizeEndOfLine (text : string)=
        text.Replace("\r\n", "\n")

    let splitBy (c : char) (text : string) =
        text.Split(c)

    let splitLines (text : string) =
        text
        |> normalizeEndOfLine
        |> splitBy '\n'

    let toLower (text : string) =
        text.ToLower()

    let replace (oldValue : string) (newValue : string) (text : string) =
        text.Replace(oldValue, newValue)

    let append (value : string) (text : string) =
        text + value

    let prepend (value : string) (text : string) =
        value + text

    let join (sep : string) (values : string array) =
        String.Join(sep, values)

    let trimStartEmptyLines (text : string) =
        text
        |> splitLines
        |> Array.skipWhile String.IsNullOrEmpty
        |> join "\n"

    let trimEndEmptyLines (text : string) =
        // Could be optimized by using a for loop
        text
        |> splitLines
        |> Array.rev
        |> Array.skipWhile String.IsNullOrEmpty
        |> Array.rev
        |> join "\n"
