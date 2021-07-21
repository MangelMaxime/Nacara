module rec Node

open Fable.Core
open Fable.Core.JsInterop

type Node.Fs.IExports with
    [<Emit("$0.copyFileSync($1...)")>]
    member __.copyFileSync (src: string, dest : string, ?mode : int) =
        jsNative

module Directory =

    let moveUp (path : string) =
        path.Split(char Node.Api.path.sep)
        |> Array.skip 1
        |> String.concat Node.Api.path.sep

    let join (pathA : string) (pathB : string) =
        Node.Api.path.join(pathA, pathB)

    let exists (dir : string) =
        Promise.create (fun resolve reject ->
            Node.Api.fs.exists((U2.Case1 dir), (fun res ->
                resolve res
            ))
        )

    let create (dir : string) =
        let options = createObj [
            "recursive" ==> true
        ]

        Promise.create (fun resolve reject ->
            Node.Api.fs?mkdir(dir, options, (fun (err : Node.Base.ErrnoException option) ->
                match err with
                | Some err -> reject (err :?> System.Exception)
                | None -> resolve ()
            ))
        )

    let ensure (dir: string) =
        promise {
            match! exists dir with
            | true -> return ()
            | false ->
                return! create dir
        }

    let dirname (dir : string) =
        Node.Api.path.dirname(dir)

    let getFiles (isRecursive : bool) (dir : string) =
        Promise.create (fun resolve reject ->
            Node.Api.fs.readdir(U2.Case1 dir, (fun (err: Node.Base.ErrnoException option) (files : ResizeArray<string>) ->
                match err with
                | Some err ->
                    reject (err :?> System.Exception)
                | None ->
                    files.ToArray()
                    |> Array.map (fun file ->
                        file, File.statsSync (Directory.join dir file)
                    )
                    |> Array.map (fun (filePath, fileInfo) ->
                        if fileInfo.isDirectory() then
                            // If recursive then get the files from the others sub dirs
                            if isRecursive then
                                promise {
                                    let! files = getFiles true (Directory.join dir filePath)
                                    return files
                                            |> List.map (Directory.join filePath)
                                }
                            else
                            // Else, we return an empty list and this will have the effect
                            // of removing the directory from the final list
                                promise {
                                    return [ ]
                                }
                        else
                            promise {
                                return [ filePath ]
                            }
                    )
                    |> Promise.all
                    |> Promise.map (fun directories ->
                        Array.reduce (fun a b ->
                            a @ b
                        ) directories
                    )
                    |> Promise.map (fun files ->
                        resolve files
                    )
                    |> ignore
            ))
        )

module File =
    let changeExtension (extention : string) (path : string) =
        let extensionPos = path.LastIndexOf('.')
        path.Substring(0, extensionPos + 1) + extention

    let read (path: string) =
        Promise.create (fun resolve reject ->
            Node.Api.fs.readFile(path, (fun err buffer ->
                match err with
                | Some err -> reject (err :?> System.Exception)
                | None -> resolve (buffer.ToString())
            ))
        )

    let readSync (path: string) =
        Node.Api.fs.readFileSync(path).ToString()

    let write (path: string) (content: string) =
        promise {
            do! path |> Directory.dirname |> Directory.ensure
            return!
                Promise.create (fun resolve reject ->
                    Node.Api.fs.writeFile(path, content, (fun res ->
                        match res with
                        | Some res -> reject (res :?> System.Exception)
                        | None -> resolve ()
                    ))
                )
        }

    let copy (source: string) (destination: string) =
        promise {
            do! destination |> Directory.dirname |> Directory.ensure
            return!
                Promise.create (fun resolve reject ->
                    Node.Api.fs?copyFile$(source, destination, fun res ->
                        match res with
                        | Some res -> reject res
                        | None -> resolve ()
                    )
                )
        }

    let exist (path : string) =
        Promise.create (fun resolve reject ->
            Node.Api.fs.exists(U2.Case1 path, (fun res ->
                resolve res
            ))
        )

    let existSync (path : string) =
        Node.Api.fs.existsSync(U2.Case1 path)

    let absolutePath (dir : string) =
        Node.Api.path.resolve(dir)

    let stats (path : string) =
        Promise.create (fun resolve reject ->
            Node.Api.fs.stat(U2.Case1 path, (fun (err: Node.Base.ErrnoException option) (stats : Node.Fs.Stats) ->
                match err with
                | Some err ->
                    reject (err :?> System.Exception)
                    null
                | None ->
                    resolve stats
                    null
            ))
        )

    let statsSync (path : string) : Node.Fs.Stats =
        Node.Api.fs.statSync(U2.Case1 path)
