module Render.DocPage

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fulma
open Types

let whitespace =
    span [ DangerouslySetInnerHTML { __html = " " } ]
        [ ]

let header =
    div [ Class "page-header" ]
        [ Hero.hero [ Hero.Color IsLight ]
            [ Hero.body [ Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                [ Heading.h2 [ Heading.IsSpaced ]
                    [ str "Fulma.Extensions.Wikiki.Switch" ]
                  Heading.p [ Heading.IsSubtitle
                              Heading.Is5
                              Heading.Modifiers [ Modifier.TextWeight TextWeight.Light ] ]
                    [ str "Wrapper on top of bulma-switch providing support for displaying the classic checkbox as a switch button" ] ] ] ]

let sidebarHeader pathPrefix =
    div [ Style [ Display "flex"
                  JustifyContent "center" ] ]
        [ Image.image [ Image.Is128x128 ]
            [ img [ Src (pathPrefix + "assets/logo_transparent.svg")
                    Style [ Height "100%" ] ] ] ]

let renderPage tocContent pageContent =
    Columns.columns [ Columns.IsGapless ]
        [ Column.column [ ]
            [ Section.section [ ]
                [ Content.content [ ]
                    [ div [ DangerouslySetInnerHTML { __html =  pageContent } ] [ ] ] ] ]
          Column.column [ Column.Width (Screen.All, Column.Is3)
                          Column.Modifiers [ Modifier.IsHidden (Screen.Touch, true) ]
                          Column.Props [ DangerouslySetInnerHTML { __html =  tocContent } ] ]
            [ ] ]

let renderBadge (href : string) (badgeUrl : string)=
    a [ Href href ]
        [ img [ Src badgeUrl ] ]

let badges =
    Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
        [ p [ ]
            [ renderBadge "" "https://badgen.net/badge/Version/1.0.0/green" ]
          p [ ]
            [ renderBadge "" "https://badgen.net/badge//npm/blue?icon=npm"
              whitespace
              renderBadge "" "https://badgen.net/badge//github/blue?icon=github" ] ]



let toHtml (model : Model) (pageContext : PageContext) =
    let pageTitle = Option.defaultValue "" pageContext.Attributes.Title
    let titleStr = pageTitle + " · " +  model.Config.Title

    let pathPrefix =
        let levelDiff =
            pageContext.Path
                .Replace(model.WorkingDirectory, "")
                .Split(char Fable.Import.Node.Exports.path.sep)
                |> Array.skip 1
                |> Array.length

        if levelDiff = 1 then
            ""
        else
            String.replicate (levelDiff - 1)  (".." + Fable.Import.Node.Exports.path.sep)

    renderPage pageContext.TableOfContent pageContext.Content
    |> Render.Common.basePage model
