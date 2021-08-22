module Page.Changelog

open Nacara.Core.Types
open System
open System.Text.RegularExpressions
open Fable.Core.JsInterop
open Thoth.Json
open Fable.Core
open Node
open Feliz
open Feliz.Bulma

type Attributes =
    {
        ChangelogPath : string
    }

    static member Decoder =
        Decode.object (fun get ->
            {
                ChangelogPath = get.Required.Field "changelog_path" Decode.string
            }
        )


let slugify (_s: string): string = importDefault "slugify"

let renderVersion (versionText : string) (date : DateTime option) =
    let dateText =
        match date with
        | Some date ->
            Date.Format.localFormat Date.Local.englishUK "MMM yyyy" date
        | None -> ""

    let slug = slugify versionText

    Html.li [
        prop.className "changelog-list-item is-version"
        prop.children [
            Html.a [
                prop.href ("#" + slug)
                prop.children [
                    // This is the element used as an anchor
                    // We make it appear a bit higher so the tag isn't squash against the navbar
                    Html.span [
                        prop.id slug
                        prop.style [
                            style.paddingTop (length.rem 4.25)
                        ]
                    ]

                    Bulma.tag [
                        color.isPrimary
                        tag.isLarge
                        text.hasTextWeightBold
                        prop.text versionText
                    ]
                ]
            ]

            Bulma.text.span [
                prop.className "release-date"
                text.isUppercase
                text.hasTextWeightBold
                size.isSize5
                prop.text dateText
            ]
        ]
    ]

type ChangelogParser.Types.CategoryType with
    member this.Color
        with get () =
            match this with
            | ChangelogParser.Types.CategoryType.Added -> color.isSuccess
            | ChangelogParser.Types.CategoryType.Changed -> color.isInfo
            | ChangelogParser.Types.CategoryType.Deprecated -> color.isWarning
            | ChangelogParser.Types.CategoryType.Removed -> color.isDanger
            | ChangelogParser.Types.CategoryType.Fixed -> color.isInfo
            | ChangelogParser.Types.CategoryType.Security -> color.isInfo
            | ChangelogParser.Types.CategoryType.Unknown _ -> color.isInfo

let private renderCategoryBody
    (markdownToHml : string -> JS.Promise<string>)
    (category : ChangelogParser.Types.CategoryType)
    (body : ChangelogParser.Types.CategoryBody) =
        promise {
            let removeParagraphMarkup (text : string) =
                match Regex.Match(text.Trim(), "^<p>(.*)</p>$") with
                | m when m.Success ->
                    m.Groups.[1].Value
                | _ -> text

            let textToHtml (text : string) =
                markdownToHml text
                // |> Promise.map removeParagraphMarkup

            match body with
            | ChangelogParser.Types.CategoryBody.ListItem content ->
                let! htmlText = textToHtml content

                return Html.li [
                    prop.className "changelog-list-item"
                    prop.children [
                        Bulma.tag [
                            category.Color
                            tag.isMedium
                            text.hasTextWeightBold
                            prop.text category.Text
                        ]

                        Html.div [
                            prop.className "changelog-list-item-text"

                            prop.children [
                                Html.span [
                                    prop.dangerouslySetInnerHTML htmlText
                                ]
                            ]
                        ]
                    ]
                ]

            | ChangelogParser.Types.CategoryBody.Text content ->
                let! htmlText =
                    content
                    |> ChangelogParser.splitLines
                    // Remove one level of indentation from the content
                    // This is to make markdown understands that this is not a quoted paragraph as we don't provide it the whole text
                    // and so it doesn't know that the line is indented under a list item
                    |> Array.map (fun line ->
                        if line.StartsWith("    ") then
                            line.Substring(3)
                        else
                            line
                    )
                    |> Array.toList
                    |> String.concat "\n"
                    |> textToHtml

                return Html.li [
                    prop.className "changelog-list-item"
                    prop.children [
                        Html.div [
                            prop.className "changelog-details"
                            prop.dangerouslySetInnerHTML htmlText
                        ]
                    ]
                ]
        }

let renderChangelogItems
    (markdownToHml : string -> JS.Promise<string>)
    (items : ChangelogParser.Types.Version list) =

    items
    |> List.map (fun version ->
        match version.Version with
        | Some versionText ->
            promise {
                let! categoriesHtml =
                    version.Categories
                    |> Map.toList
                    |> List.map (fun (categoryType, bodyItems) ->
                        promise {
                            let! bodyItemsHtml =
                                bodyItems
                                |> List.map (fun body ->
                                    renderCategoryBody markdownToHml categoryType body
                                )
                                |> Promise.all

                            return
                                // Use fragment instead of `ofArray` to avoid having to set a `Key` on each children
                                React.fragment bodyItemsHtml

                        }
                    )
                    |> Promise.all

                return
                    React.fragment [
                            yield renderVersion versionText version.Date
                            yield! categoriesHtml
                    ]
            }

        | None ->
            Promise.lift null
    )

let private changelogContainer (changelogItems : ReactElement array) =
    Bulma.section [
        Bulma.content [
            Html.section [
                prop.className "changelog"
                prop.children [
                    Html.ul [
                        prop.className "changelog-list"
                        prop.children changelogItems
                    ]
                ]
            ]
        ]
    ]

let render (rendererContext : RendererContext) (pageContext : PageContext) =
    promise {
        match Decode.fromValue "$" Attributes.Decoder pageContext.Attributes with
        | Ok pageAttributes ->
            let changelogPath =
                path.join(
                    rendererContext.Config.SourceFolder,        // 1. Source folder
                    path.dirname(pageContext.RelativePath),     // 2. Take the relative path of the changelog.md file
                    pageAttributes.ChangelogPath                // 3. Join the path provided by the user which is relative from the file located in the source folder
                )

            let! changelogContent =
                JS.Constructors.Promise.Create(fun resolve reject ->
                    Node.Api.fs.readFile(changelogPath, (fun err buffer ->
                        match err with
                        | Some err -> reject (err :?> System.Exception)
                        | None -> resolve (buffer.ToString())
                    ))
                )

            match ChangelogParser.parse changelogContent with
            | Ok changelog ->
                let! changelogItems =
                    renderChangelogItems rendererContext.MarkdownToHtml changelog.Versions
                    |> Promise.all

                return Minimal.render {
                        Config = rendererContext.Config
                        Section = pageContext.Section
                        TitleOpt = pageContext.Title
                        Content =
                            WithMenuOrToc.render {
                                Config = rendererContext.Config
                                SectionMenu = rendererContext.SectionMenu
                                Pages = rendererContext.Pages
                                PageContext = pageContext
                                PageHtml = "" // Pass no content to the `render` function, as there is no interesting information in it for the changelog
                                RenderMenu = true
                                PageContent = changelogContainer changelogItems
                            }
                    }

            | Error errorMessage ->
                return failwith errorMessage

        | Error errorMessage ->
            return failwith errorMessage
    }
