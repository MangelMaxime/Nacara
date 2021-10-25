module File

open System.IO
open System.Text

let write
    (docsRoot : string)
    (fileName : string)
    (sb : StringBuilder) =

    let filePath = Path.Combine(docsRoot, fileName)

    // Ensure that the directory exists
    Directory.CreateDirectory(Path.GetDirectoryName(filePath))
    |> ignore

    use file = new StreamWriter(filePath)
    file.Write(sb.ToString())
