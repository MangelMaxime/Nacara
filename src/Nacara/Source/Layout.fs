module Layout

#nowarn "52"

open Nacara.Core.Types
open Fable.Core
open Fable.Core.JsInterop
open Node

let private babel : obj =
    import "default" "@babel/core"

let load (config : Config) (layoutPath : string) : JS.Promise<LayoutInfo> =
    promise {
        // The path is relative, so load it relatively from the CWD
        if layoutPath.StartsWith("./") then
            let fullPath =
                path.join(config.WorkingDirectory, layoutPath)

            match layoutPath with
            | Js ->
                return! importDynamic fullPath

            | Jsx ->
                let! res = babel?transformFileAsync(fullPath)

                let destination =
                    path.join(config.WorkingDirectory, ".nacara", layoutPath)
                    |> File.changeExtension "js"

                do! File.write destination res?code

                return! importDynamic destination

            | Other _ ->
                Log.error $"Local layouts scripts must be JavaScript or JSX files: %s{layoutPath}"
                ``process``.exit(ExitCode.INVALID_LAYOUT_SCRIPT)
                return failwith "Make the compiler happy, but should already have existed"

        // The path is not relative, require it as an npm module
        else
            return! importDynamic layoutPath
    }
    |> Promise.map (fun (layoutModule : LayoutInterface) ->
        layoutModule.``default``
    )
