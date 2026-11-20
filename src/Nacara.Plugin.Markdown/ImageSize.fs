namespace Nacara.Plugins

open System
open System.IO
open System.Text
open System.Text.RegularExpressions

/// <summary>The size of an image, read from the start of its file.</summary>
/// <remarks>PNG, GIF, JPEG, WebP, AVIF and SVG. A JPEG rotated by its EXIF orientation is given
/// as it is displayed.</remarks>
[<RequireQualifiedAccess>]
module ImageSize =

    let private u16be (bytes: byte array) index =
        (int bytes[index] <<< 8) ||| int bytes[index + 1]

    let private u32be (bytes: byte array) index =
        (int bytes[index] <<< 24)
        ||| (int bytes[index + 1] <<< 16)
        ||| (int bytes[index + 2] <<< 8)
        ||| int bytes[index + 3]

    let private u16le (bytes: byte array) index =
        int bytes[index] ||| (int bytes[index + 1] <<< 8)

    let private u24le (bytes: byte array) index =
        int bytes[index]
        ||| (int bytes[index + 1] <<< 8)
        ||| (int bytes[index + 2] <<< 16)

    let private ascii (bytes: byte array) index count =
        if bytes.Length >= index + count then
            Encoding.ASCII.GetString(bytes, index, count)
        else
            ""

    let private head (path: string) (count: int) =
        use stream = File.OpenRead path
        let buffer = Array.zeroCreate<byte> count
        let read = stream.ReadAtLeast(buffer, count, false)
        Array.truncate read buffer

    let private png (bytes: byte array) =
        if bytes.Length >= 24 && ascii bytes 1 3 = "PNG" && ascii bytes 12 4 = "IHDR" then
            Some(u32be bytes 16, u32be bytes 20)
        else
            None

    let private gif (bytes: byte array) =
        if bytes.Length >= 10 && ascii bytes 0 3 = "GIF" then
            Some(u16le bytes 6, u16le bytes 8)
        else
            None

    let private webp (bytes: byte array) =
        if bytes.Length >= 30 && ascii bytes 0 4 = "RIFF" && ascii bytes 8 4 = "WEBP" then
            match ascii bytes 12 4 with
            | "VP8 " -> Some(u16le bytes 26 &&& 0x3FFF, u16le bytes 28 &&& 0x3FFF)
            | "VP8L" ->
                let bits = u24le bytes 21 ||| (int bytes[24] <<< 24)
                Some((bits &&& 0x3FFF) + 1, ((bits >>> 14) &&& 0x3FFF) + 1)
            | "VP8X" -> Some(u24le bytes 24 + 1, u24le bytes 27 + 1)
            | _ -> None
        else
            None

    let private avif (bytes: byte array) =
        if
            ascii bytes 4 4 = "ftyp"
            && (ascii bytes 8 4 = "avif" || ascii bytes 8 4 = "avis")
        then
            let index = Encoding.ASCII.GetString(bytes).IndexOf "ispe"

            if index >= 0 && bytes.Length >= index + 16 then
                Some(u32be bytes (index + 8), u32be bytes (index + 12))
            else
                None
        else
            None

    let private quarterTurned (exif: byte array) =
        if ascii exif 0 4 <> "Exif" || exif.Length < 14 then
            false
        else
            let tiff = 6
            let little = ascii exif tiff 2 = "II"

            let u16 index =
                if little then
                    u16le exif index
                else
                    u16be exif index

            let u32 index =
                if little then
                    u16le exif index ||| (u16le exif (index + 2) <<< 16)
                else
                    u32be exif index

            let directory = tiff + u32 (tiff + 4)

            if exif.Length < directory + 2 then
                false
            else
                let entries = u16 directory

                seq { 0 .. entries - 1 }
                |> Seq.map (fun entry -> directory + 2 + entry * 12)
                |> Seq.takeWhile (fun offset -> exif.Length >= offset + 10)
                |> Seq.tryFind (fun offset -> u16 offset = 0x0112)
                |> Option.exists (fun offset -> u16 (offset + 8) >= 5)

    let private jpeg (path: string) =
        use stream = File.OpenRead path

        let byte () = stream.ReadByte()

        let u16 () =
            let high = byte ()
            let low = byte ()
            (high <<< 8) ||| low

        if byte () <> 0xFF || byte () <> 0xD8 then
            None
        else
            let mutable turned = false
            let mutable size = None
            let mutable reading = true

            while reading do
                let mutable marker = byte ()

                while marker <> -1 && marker <> 0xFF do
                    marker <- byte ()

                while marker = 0xFF do
                    marker <- byte ()

                match marker with
                | -1
                | 0xD9
                | 0xDA -> reading <- false
                | 0x01 -> ()
                | marker when marker >= 0xD0 && marker <= 0xD7 -> ()
                | marker ->
                    let length = u16 () - 2

                    if
                        marker >= 0xC0
                        && marker <= 0xCF
                        && marker <> 0xC4
                        && marker <> 0xC8
                        && marker <> 0xCC
                    then
                        byte () |> ignore
                        let height = u16 ()
                        let width = u16 ()
                        size <- Some(width, height)
                        reading <- false
                    elif marker = 0xE1 && length > 0 then
                        let exif = Array.zeroCreate<byte> length
                        stream.ReadExactly(exif, 0, length)
                        turned <- turned || quarterTurned exif
                    else
                        stream.Seek(int64 length, SeekOrigin.Current) |> ignore

            size
            |> Option.map (fun (width, height) ->
                if turned then
                    height, width
                else
                    width, height
            )

    let private svg (path: string) =
        let text = Encoding.UTF8.GetString(head path 16384)
        let tag = Regex.Match(text, @"<svg\b[^>]*>", RegexOptions.IgnoreCase)

        let attribute name =
            let found = Regex.Match(tag.Value, $@"\s{name}\s*=\s*[""']([^""']*)[""']")

            if found.Success then
                Some found.Groups[1].Value
            else
                None

        let length (value: string) =
            let found = Regex.Match(value.Trim(), @"^([0-9]*\.?[0-9]+)(px)?$")

            if found.Success then
                Some(int (Math.Round(float found.Groups[1].Value)))
            else
                None

        if not tag.Success then
            None
        else
            match
                attribute "width" |> Option.bind length, attribute "height" |> Option.bind length
            with
            | Some width, Some height -> Some(width, height)
            | _ ->
                attribute "viewBox"
                |> Option.bind (fun box ->
                    match
                        box.Split(
                            [|
                                ' '
                                ','
                            |],
                            StringSplitOptions.RemoveEmptyEntries
                        )
                    with
                    | [| _; _; width; height |] ->
                        match Double.TryParse width, Double.TryParse height with
                        | (true, width), (true, height) ->
                            Some(int (Math.Round width), int (Math.Round height))
                        | _ -> None
                    | _ -> None
                )

    /// <summary>The width and height of an image, in pixels.</summary>
    /// <param name="path">The image file.</param>
    let read (path: string) =
        try
            if not (File.Exists path) then
                None
            else
                match (Path.GetExtension path).ToLowerInvariant() with
                | ".svg" -> svg path
                | ".jpg"
                | ".jpeg" -> jpeg path
                | _ ->
                    let bytes = head path 4096

                    png bytes
                    |> Option.orElse (gif bytes)
                    |> Option.orElse (webp bytes)
                    |> Option.orElse (avif bytes)
            |> Option.filter (fun (width, height) -> width > 0 && height > 0)
        with
        | :? IOException
        | :? UnauthorizedAccessException -> None
