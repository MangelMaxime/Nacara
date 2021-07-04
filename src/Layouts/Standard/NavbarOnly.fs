namespace Layout.Standard
open System.Text.RegularExpressions

module NavbarOnly =

    open Fable.React
    open Fable.React.Props
    open Types
    open Feliz
    open Thoth.Json

    let private renderPage (pageContent : string) =
        div [ DangerouslySetInnerHTML { __html =  pageContent } ] [ ]

    let private addMenuScript (content : ReactElement) =
        fragment [ ]
            [
                content

                Html.script [
                    prop.src "/static/nacara_internals/menu.js"
                ]
            ]

    let toHtml (model : Model) (pageContext : PageContext) =
        promise {
            let! pageContext =
                pageContext
                |> PageContext.processMarkdown model
                // |> Promise.bind Prelude.processTableOfContent

            let tocInformation =
                TableOfContentParser.parse pageContext

            let titleDecoder =
                Decode.optional "title" Decode.string

            match Decode.fromValue "$" titleDecoder pageContext.FrontMatter with
            | Ok titleOpt ->
                return
                    renderPage pageContext.Content
                    |> addMenuScript
                    |> Prelude.basePage model titleOpt
            | Error errorMessage ->
                return failwith errorMessage
        }
