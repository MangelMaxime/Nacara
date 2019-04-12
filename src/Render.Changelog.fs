module Render.Changelog

open Fulma
open Types
open System
open System.Text.RegularExpressions
open Fable.Helpers.React
open Fable.Helpers.React.Props

let renderVersion (versionText : string) (date : DateTime option) =
    let dateText =
        match date with
        | Some date ->
            Date.Format.localFormat Date.Local.englishUK "MMM yyyy" date
        | None -> ""

    let slug = StringJS.S.Invoke(versionText).toString()

    li [ Class "changelog-list-item is-version" ]
        [ a [ Href ("#" + slug) ]
            [ // This is the element used as an anchor
              // We make it appear a bit highter so the tag isn't squash against the navbar
              span [ Id slug
                     Style [ Visibility "hidden"
                             MarginTop "-1rem"
                             Position "absolute" ] ]
                [ str "#" ]
              Tag.tag [ Tag.Color IsPrimary
                        Tag.Size IsLarge
                        Tag.Modifiers [ Modifier.TextWeight TextWeight.Bold ] ]
                [ str versionText ] ]
          Text.span [ CustomClass "release-date"
                      Modifiers [ Modifier.TextTransform TextTransform.UpperCase
                                  Modifier.TextWeight TextWeight.Bold
                                  Modifier.TextSize (Screen.All, TextSize.Is5) ] ]
            [ str dateText ]
          div [ Class "changelog-details" ]
            [ ] ]

type Changelog.Types.CategoryType with
    member this.Color
        with get () =
            match this with
            | Changelog.Types.CategoryType.Added -> IsSuccess
            | Changelog.Types.CategoryType.Changed -> IsInfo
            | Changelog.Types.CategoryType.Deprecated -> IsWarning
            | Changelog.Types.CategoryType.Removed -> IsDanger
            | Changelog.Types.CategoryType.Fixed -> IsInfo
            | Changelog.Types.CategoryType.Security -> IsInfo
            | Changelog.Types.CategoryType.Unkown _ -> IsInfo

type Changelog.Types.CategoryBody with
    member this.ToHtml(category : Changelog.Types.CategoryType) =
        let removeParagraphMarkup (text : string) =
            match Regex.Match(text.Trim(), "^<p>(.*)</p>$") with
            | m when m.Success ->
                m.Groups.[1].Value
            | _ -> text

        match this with
        | Changelog.Types.CategoryBody.ListItem text ->
            let htmlText =
                Helpers.markdown text
                |> removeParagraphMarkup

            li [ Class "changelog-list-item" ]
                [ Tag.tag [ Tag.Color category.Color
                            Tag.Size IsMedium
                            Tag.Modifiers [ Modifier.TextWeight TextWeight.Bold ] ]
                    [ str category.Text ]
                  div [ Class "changelog-list-item-text" ]
                    [ span [ DangerouslySetInnerHTML { __html = htmlText } ]
                        [ ] ]
                  div [ Class "changelog-details" ]
                    [ ] ]
        | Changelog.Types.CategoryBody.Text text ->
            let htmlText = Helpers.markdown text
            li [ Class "changelog-list-item" ]
                [ div [ Class "changelog-details"
                        DangerouslySetInnerHTML { __html = htmlText } ]
                            [ ] ]


let toHtml (model : Model) (changelog : Changelog.Types.Changelog) =
    let changelogItems =
        changelog.Versions
        |> List.map (fun version ->
            match version.Version with
            | Some versionText ->
                fragment [ ]
                    [ yield renderVersion versionText version.Date
                      for category in version.Categories do
                        yield!
                            category.Value
                            |> List.map (fun body ->
                                body.ToHtml(category.Key)
                            ) ]

            | None -> nothing
        )

    Columns.columns [ ]
        [ Column.column [ Column.Width (Screen.All, Column.Is9) ]
            [ Content.content [ ]
                [ section [ Class "changelog" ]
                    [ ul [ Class "changelog-list" ]
                        changelogItems ] ] ] ]
    |> Render.Common.basePage model "Changelog"
