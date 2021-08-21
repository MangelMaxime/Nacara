module Serve

open Nacara.Core.Types

let serve (config : Config) =

    let server = Server.create config

    server.listen(config.ServerPort, fun () ->
        Log.success $"Serving {config.SourceFolder} at: http://localhost:%i{config.ServerPort}"
    )
    |> ignore
