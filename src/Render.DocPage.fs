module Render.DocPage

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fulma
open Types
open Fable.Import

let private renderPage (menu : React.ReactElement option) (tocContent : string) (pageContent : string) =
    Columns.columns [ Columns.IsGapless ]
        [ Column.column [ Column.Width (Screen.Desktop, Column.Is2)
                          Column.Width (Screen.Touch, Column.Is3)
                          Column.Props [ Style [ OverflowY "auto"
                                                 Height "100%" ] ] ]
            [ div [ Style [ MaxHeight "100%" ] ]
                [ ofOption menu ] ]
          Column.column [ Column.Width (Screen.Desktop, Column.Is8)
                          Column.Width (Screen.Touch, Column.Is9)
                          Column.Props [ Style [ OverflowY "auto"
                                                 Height "100%"
                                                 ScrollBehavior "smooth" ] ] ]
            [ Section.section [ ]
                [ Content.content [ ]
                    [ div [ DangerouslySetInnerHTML { __html =  pageContent } ] [ ] ] ] ]
          Column.column [ Column.Width (Screen.Desktop, Column.Is2)
                          Column.Modifiers [ Modifier.IsHidden (Screen.Touch, true) ] ]
            [ div [ Style [ OverflowY "auto"
                            MaxHeight "100%"
                            Width "100%" ]
                    DangerouslySetInnerHTML { __html =  tocContent } ]
                [ ] ] ]

let private generateMenu (model : Model) (pageContext : PageContext) =
    match model.Config.MenuConfig with
    | Some menuConfig ->
        JS.console.log "has menu"
        let keys =
            menuConfig.keys() :?> JS.Iterable<string>
            |> JS.Array.from
            |> unbox<string array>

        keys
        |> Array.map (fun key ->
            let items =
                menuConfig.get(key)
                |> List.map (fun id ->
                    JS.console.log id
                    Menu.Item.li [ ]
                        [  ]
                )

            fragment [ ]
                [ div [ Key "" ]
                    [ ]
                  Menu.label [ ]
                    [ str key ]
                  Menu.list [ ]
                    items ]
        )
        |> Menu.menu [ Props [ Style [ MarginTop "3.25rem" ] ] ]
        |> Some
    | None ->
        JS.console.log "no menu"
        None

let toHtml (model : Model) (pageContext : PageContext) =
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

    renderPage
        (generateMenu model pageContext)
        pageContext.TableOfContent
        pageContext.Content
    |> Render.Common.basePage model pageContext.Attributes.Title
