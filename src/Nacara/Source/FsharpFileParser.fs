module FsharpFileParser

open System
open System.Text.RegularExpressions

let private frontMatterPattern =
    """\(\*(\*)+\n?(?<front_matter>---.*---)\n(\*)+\)"""

let rec processFile (result : string list) (lines : string list) =
    match lines with
    | currentLine :: tail ->
        let trimedLine = currentLine.Trim()

        if trimedLine.StartsWith("(*** hide ***)") then
            // Skip the lines as long as we don't encounter
            // another hide instruction or markdown block comment
            let newLines =
                tail
                |> List.skipWhile (fun line ->
                    not (line.Trim().StartsWith("(**"))
                )

            processFile result newLines

        else if trimedLine.StartsWith("(**") then

            let firstLine = trimedLine.[3..].Trim()

            // Take all lines until we encounter the end of the markdown block comment
            let markdownLines =
                tail
                |> List.takeWhile (fun line ->
                    not (line.TrimEnd().EndsWith("*)"))
                )

            // Remove the line we processed as markdown
            // from the lines left to process
            let rest =
                tail
                |> List.skip markdownLines.Length

            let newResult =
                result @ firstLine :: markdownLines

            processFile newResult rest

        else

            // Take all the code lines
            let codeLines =
                tail
                |> List.takeWhile (fun line ->
                    not (line.Trim().StartsWith("(**"))
                )

            // Remove the line we processed as code
            // from the lines left to process
            let rest =
                tail
                |> List.skip codeLines.Length

            let codeBlockLines =
                let codeLines =
                    codeLines
                    // Remove the empty lines at the begging of the capture block
                    // Those empty lines are not meaningful
                    |> List.skipWhile String.IsNullOrEmpty
                    // Remove the empty lines at the end of the capture block
                    // Those empty lines are not meaningful
                    |> List.rev
                    |> List.skipWhile String.IsNullOrEmpty
                    |> List.rev

                // If we don't have any code lines left do nothing
                if List.isEmpty codeLines then
                    []
                // Otherwise, create a markdown code block
                else
                    [
                        "```fs"
                        yield! codeLines
                        "```"
                    ]

            processFile (result @ codeBlockLines) rest

    // All the lines have been processed
    | [] ->
        result

let tryParse (fileContent : string) =
    let m = Regex.Match(fileContent, frontMatterPattern, RegexOptions.Singleline)

    if m.Success then
        // Extract the front matter section
        let frontMatter = m.Groups.["front_matter"].Value

        // Remove the front matter section from the file
        let fileContent =
            fileContent.Substring(m.Index + m.Length)
            |> String.splitLines
            |> Array.toList
            |> processFile []
            |> (fun lines ->
                String.Join("\n", lines)
            )

        frontMatter
        + Environment.NewLine
        + fileContent
        |> Some

    else
        None
