namespace Layout.Standard

module Changelog =

    open Fulma
    open Types
    open System
    open System.Text.RegularExpressions
    open Fable.React
    open Fable.React.Props
    open Fable.Core.JsInterop
    open Thoth.Json
    open Fable.Core

    let slugify (_s: string): string = importDefault "slugify"

    let renderVersion (versionText : string) (date : DateTime option) =
        let dateText =
            match date with
            | Some date ->
                Date.Format.localFormat Date.Local.englishUK "MMM yyyy" date
            | None -> ""

        let slug = slugify versionText

        li [ Class "changelog-list-item is-version" ]
            [ a [ Href ("#" + slug) ]
                [ // This is the element used as an anchor
                  // We make it appear a bit highter so the tag isn't squash against the navbar
                  span [ Id slug
                         Style [ Visibility "hidden"
                                 MarginTop "-1rem"
                                 Position PositionOptions.Absolute ] ]
                    [ str "#" ]
                  Tag.tag [ Tag.Color IsPrimary
                            Tag.Size IsLarge
                            Tag.Modifiers [ Modifier.TextWeight TextWeight.Bold ] ]
                    [ str versionText ] ]
              Text.span [ CustomClass "release-date"
                          Modifiers [ Modifier.TextTransform TextTransform.UpperCase
                                      Modifier.TextWeight TextWeight.Bold
                                      Modifier.TextSize (Screen.All, TextSize.Is5) ] ]
                [ str dateText ] ]

    type ChangelogParser.Types.CategoryType with
        member this.Color
            with get () =
                match this with
                | ChangelogParser.Types.CategoryType.Added -> IsSuccess
                | ChangelogParser.Types.CategoryType.Changed -> IsInfo
                | ChangelogParser.Types.CategoryType.Deprecated -> IsWarning
                | ChangelogParser.Types.CategoryType.Removed -> IsDanger
                | ChangelogParser.Types.CategoryType.Fixed -> IsInfo
                | ChangelogParser.Types.CategoryType.Security -> IsInfo
                | ChangelogParser.Types.CategoryType.Unkown _ -> IsInfo

    let private renderCategoryBody
        (lightnerConfig : JS.Map<string, CodeLightner.Config>)
        (category : ChangelogParser.Types.CategoryType)
        (body : ChangelogParser.Types.CategoryBody) =

            let removeParagraphMarkup (text : string) =
                match Regex.Match(text.Trim(), "^<p>(.*)</p>$") with
                | m when m.Success ->
                    m.Groups.[1].Value
                | _ -> text

            let textToHtml (text : string) =
                Helpers.markdown text [||]
                |> highlightCode lightnerConfig
                |> Promise.map removeParagraphMarkup

            match body with
            | ChangelogParser.Types.CategoryBody.ListItem text ->
                promise {
                    let! htmlText = textToHtml text

                    return
                        li [ Class "changelog-list-item" ]
                            [ Tag.tag [ Tag.Color category.Color
                                        Tag.Size IsMedium
                                        Tag.Modifiers [ Modifier.TextWeight TextWeight.Bold ] ]
                                [ str category.Text ]
                              div [ Class "changelog-list-item-text" ]
                                [ span [ DangerouslySetInnerHTML { __html = htmlText } ]
                                    [ ] ] ]
                }

            | ChangelogParser.Types.CategoryBody.Text text ->
                promise {
                    let! htmlText = textToHtml text

                    return
                        li [ Class "changelog-list-item" ]
                           [ div [ Class "changelog-details"
                                   DangerouslySetInnerHTML { __html = htmlText } ]
                                        [ ] ]
                }

    let renderChangelogItems
        (model : Model)
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
                                        renderCategoryBody model.LightnerCache categoryType body
                                    )
                                    |> Promise.all

                                return
                                    // Use fragment instead of `ofArray` to avoid having to set a `Key` on each children
                                    fragment [ ]
                                        bodyItemsHtml
                            }
                        )
                        |> Promise.all

                    return
                        fragment [ ]
                            [
                                yield renderVersion versionText version.Date
                                yield! categoriesHtml
                            ]
                }

            | None ->
                Promise.lift nothing
        )

    let toHtml (model : Model) (pageContext : PageContext) =
        promise {
            let getChangelogPath =
                Decode.field "changelog_path" Decode.string

            match Decode.fromValue "$.extra" getChangelogPath pageContext.Attributes.Extra with
            | Ok relativePath ->
                let changelogPath = Directory.join pageContext.Path relativePath
                let! changelogContent = File.read changelogPath

                match ChangelogParser.parse changelogContent with
                | Ok changelog ->
                    let! changelogItems =
                        renderChangelogItems model changelog.Versions
                        |> Promise.all

                    return
                        Columns.columns [ ]
                            [
                                Column.column
                                    [
                                        Column.Width (Screen.All, Column.Is8)
                                        Column.Offset (Screen.All, Column.Is2)
                                    ]
                                    [
                                        Content.content [ ]
                                            [
                                                section [ Class "changelog" ]
                                                    [ ul [ Class "changelog-list" ]
                                                changelogItems ]
                                            ]
                                    ]
                            ]
                        |> Prelude.basePage model pageContext.Attributes.Title
                | Error msg ->
                    return failwith msg
            | Error msg ->
                return failwith msg
        }
