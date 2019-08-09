module Render.DocPage

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fulma
open Fable.FontAwesome
open Types
open Fable.Import

let private renderPage (menu : React.ReactElement option) (tocContent : string) (pageContent : string) =
    Columns.columns [ Columns.IsGapless ]
        [ Column.column [ Column.Width (Screen.Desktop, Column.Is2)
                          Column.Width (Screen.Tablet, Column.Is3)
                          Column.Width (Screen.Mobile, Column.IsFull)
                          Column.Modifiers [ Modifier.IsHidden (Screen.Mobile, true) ]
                          Column.CustomClass "full-height-scrollable-content"
                          Column.Props [ Id "toc-column"
                                        //  Style [ OverflowY "auto"
                                        //          Height "100%" ]
                                       ] ]
            [ ofOption menu ]
          Column.column [ Column.Width (Screen.Desktop, Column.Is8)
                          Column.Width (Screen.Tablet, Column.Is9)
                          Column.Width (Screen.Mobile, Column.IsFull)
                          Column.CustomClass "full-height-scrollable-content"
                          Column.Props [ Id "content-column"
                                         Style [ //OverflowY "auto"
                                                 //Height "100%"
                                                 // We need to set ScrollBehavior via style so the polyfill can work
                                                 ScrollBehavior "smooth" ] ] ]
            [ Section.section [ ]
                [ Content.content [ ]
                    [ div [ DangerouslySetInnerHTML { __html =  pageContent } ] [ ] ] ] ]
          Column.column [ Column.Width (Screen.Desktop, Column.Is2)
                          Column.Modifiers [ Modifier.IsHidden (Screen.Tablet, true) ]
                          Column.CustomClass "full-height-scrollable-content" ]
            [ div [ DangerouslySetInnerHTML { __html =  tocContent } ]
                [ ] ]

          Fa.stack [ Fa.Stack.Size Fa.Fa2x
                     Fa.Stack.CustomClass "is-hidden-tablet"
                     Fa.Stack.Props [ Id "toc-toggle"
                                      Style [ Position "fixed"
                                              Bottom "0"
                                              Right "0"
                                              Margin "10px" ] ] ]
            [ Fa.i [ Fa.Regular.Circle
                     Fa.Stack2x ] [ ]
              Fa.i [ Fa.Solid.Sync
                     Fa.Stack1x ] [ ] ]
        ]

let private generateMenu (model : Model) (pageContext : PageContext) =
    match model.Config.MenuConfig with
    | Some menuConfig ->
        let keys =
            menuConfig.keys() :?> JS.Iterable<string>
            |> JS.Array.from
            |> unbox<string array>

        keys
        |> Array.map (fun key ->
            let items =
                menuConfig.get(key)
                |> List.map (function
                    | MenuItem pageId ->
                        match Map.tryFind pageId model.DocFiles with
                        | Some pageInfo ->
                            Menu.Item.li [ Menu.Item.Props [ Data("menu-id", pageId) ]
                                           generateUrl model.Config pageInfo
                                           |> Menu.Item.Href ]
                                [ str pageInfo.Attributes.Title ]
                        | None ->
                            Log.error "Unable to find the file: %s" pageId
                            nothing
                    | MenuList menuListInfo ->
                        let keys =
                            menuListInfo.keys() :?> JS.Iterable<string>
                            |> JS.Array.from
                            |> unbox<string array>

                        keys
                        |> Array.map (fun key ->
                            let subMenu =
                                menuListInfo.get(key)
                                |> List.map (function
                                    | MenuItem pageId ->
                                        match Map.tryFind pageId model.DocFiles with
                                        | Some pageInfo ->
                                            Menu.Item.li [ Menu.Item.Props [ Data("menu-id", pageId) ]
                                                           generateUrl model.Config pageInfo
                                                           |> Menu.Item.Href ]
                                                [ str pageInfo.Attributes.Title ]
                                        | None ->
                                            Log.error "Unable to find the file: %s" pageId
                                            nothing
                                    | MenuList x ->
                                        Log.warn "Nacara only support only 2 level deep menus. The following menu is too deep:\n%A" x
                                        nothing
                                )
                                |> ul [ ]

                            li [ ]
                                [ a [ Class "menu-group" ]
                                    [ span [ ]
                                        [ str key ]
                                      Icon.icon [ ]
                                        [ Fa.i [ Fa.Solid.AngleRight
                                                 Fa.Size Fa.FaLarge ]
                                            [ ] ] ]
                                  subMenu ]

                        )
                        |> Array.toList
                        |> Menu.list [ ]
                )

            fragment [ ]
                [ Menu.label [ ]
                    [ str key ]
                  Menu.list [ ]
                    items ]
        )
        |> Menu.menu [ ]
        |> Some
    | None ->
        JS.console.log "no menu"
        None

let addJavaScriptConfig (model : Model) (pageContext : PageContext) (pageContent : React.ReactElement) =
    fragment [ ]
        [ pageContent
          script [ Type "text/javascript"
                   DangerouslySetInnerHTML { __html = sprintf
                """
nacara.pageId = '%s';
                """
                (getFileId model.Config.Source pageContext) } ]
            [ ] ]

let toHtml (model : Model) (pageContext : PageContext) =
    renderPage
        (generateMenu model pageContext)
        pageContext.TableOfContent
        pageContext.Content
    |> addJavaScriptConfig model pageContext
    |> Render.Common.basePage model pageContext.Attributes.Title
