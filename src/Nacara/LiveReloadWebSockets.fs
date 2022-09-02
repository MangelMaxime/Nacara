namespace Nacara.Server

module LiveReloadWebSockets =

    open System
    open System.Text
    open System.Threading
    open System.Threading.Tasks
    open System.Net.WebSockets
    open Microsoft.AspNetCore.Http

    let mutable private sockets = list<WebSocket>.Empty
    let private addSocket sockets socket = socket :: sockets

    let private removeSocket sockets socket =
        sockets
        |> List.choose (fun currentSocket ->
            if currentSocket <> socket then Some currentSocket else None
        )

    type Msg =
        | RefreshCSS
        | ReloadPage

    let private sendMessage =
        fun (socket: WebSocket) (message: Msg) ->
            task {
                let messageText =
                    match message with
                    | RefreshCSS -> "refresh-css"
                    | ReloadPage -> "reload-page"

                let buffer = Encoding.UTF8.GetBytes(messageText)
                let segment = new ArraySegment<byte>(buffer)

                if socket.State = WebSocketState.Open then
                    do!
                        socket.SendAsync(
                            segment,
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None
                        )
                else
                    sockets <- removeSocket sockets socket
            }

    let private broadcastMessage =
        fun message ->
            task {
                for socket in sockets do
                    try
                        do! sendMessage socket message
                    with _ ->
                        sockets <- removeSocket sockets socket
            }

    let notifyClientsToReload () =
        broadcastMessage ReloadPage
        |> Async.AwaitTask
        |> Async.StartImmediate

    let notifyClientsToRefreshCSS () =
        broadcastMessage RefreshCSS
        |> Async.AwaitTask
        |> Async.StartImmediate

    type LiveReloadWebSocketMiddleware(next: RequestDelegate) =
        member __.Invoke(ctx: HttpContext) =
            async {
                if ctx.Request.Path = PathString("/live-reload") then
                    match ctx.WebSockets.IsWebSocketRequest with
                    | true ->
                        let! webSocket =
                            ctx.WebSockets.AcceptWebSocketAsync()
                            |> Async.AwaitTask

                        sockets <- addSocket sockets webSocket
                        let buffer: byte[] = Array.zeroCreate 4096

                        let! _ =
                            webSocket.ReceiveAsync(
                                new ArraySegment<byte>(buffer),
                                CancellationToken.None
                            )
                            |> Async.AwaitTask

                        ()
                    | false -> ctx.Response.StatusCode <- 400
                else
                    do!
                        next.Invoke(ctx)
                        |> (Async.AwaitIAsyncResult >> Async.Ignore)
            }
            |> Async.StartAsTask
            :> Task
