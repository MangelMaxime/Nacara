namespace Nacara.Core

open System
open System.IO
open System.Net
open System.Text
open System.Threading
open System.Collections.Concurrent

/// <summary>
/// The development server.
/// </summary>
/// <remarks>The reload script is injected into HTML responses on the way out, so nothing about
/// the built output differs between <c>build</c> and <c>serve</c>.</remarks>
type DevServer(root: AbsolutePath, basePath: string, host: string, port: int) =
    let listener = new HttpListener()
    let clients = ConcurrentDictionary<Guid, StreamWriter>()
    let cancellation = new CancellationTokenSource()

    let reloadScript =
        """<script>
(() => {
  // The browser retries a dropped EventSource on its own, so the stream is left open across restarts.
  let interrupted = false;
  const source = new EventSource("/__nacara/reload");

  source.onmessage = () => location.reload();

  source.onopen = () => {
    if (interrupted) location.reload();
  };

  source.onerror = () => {
    interrupted = true;
  };
})();
</script>"""

    let contentType (extension: string) =
        match extension.ToLowerInvariant() with
        | ".html" -> "text/html; charset=utf-8"
        | ".css" -> "text/css; charset=utf-8"
        | ".js" -> "text/javascript; charset=utf-8"
        | ".json" -> "application/json; charset=utf-8"
        | ".svg" -> "image/svg+xml"
        | ".png" -> "image/png"
        | ".jpg"
        | ".jpeg" -> "image/jpeg"
        | ".gif" -> "image/gif"
        | ".webp" -> "image/webp"
        | ".avif" -> "image/avif"
        | ".ico" -> "image/x-icon"
        | ".woff" -> "font/woff"
        | ".woff2" -> "font/woff2"
        | ".wasm" -> "application/wasm"
        | ".txt" -> "text/plain; charset=utf-8"
        | ".xml" -> "application/xml; charset=utf-8"
        | _ -> "application/octet-stream"

    let normalizedBase = "/" + basePath.Trim('/')

    /// Resolve a request path to a file, the way a static host would.
    let resolve (requestPath: string) =
        let relative =
            if
                normalizedBase <> "/"
                && requestPath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase)
            then
                requestPath.Substring(normalizedBase.Length)
            else
                requestPath

        let relative = Uri.UnescapeDataString(relative).TrimStart('/')

        let candidates =
            if relative = "" then
                [ "index.html" ]
            elif Path.HasExtension relative then
                [ relative ]
            else
                [
                    relative + "/index.html"
                    relative + ".html"
                ]

        candidates
        |> List.map (fun candidate -> AbsolutePath.combine root [ candidate ])
        |> List.tryFind (fun path -> File.Exists(AbsolutePath.value path))

    let sendReload () =
        for entry in clients do
            try
                entry.Value.Write "data: reload\n\n"
                entry.Value.Flush()
            with _ ->
                clients.TryRemove entry.Key |> ignore

    let handle (context: HttpListenerContext) =
        async {
            let response = context.Response

            try
                if context.Request.Url.AbsolutePath.EndsWith "/__nacara/reload" then
                    response.ContentType <- "text/event-stream"
                    response.Headers.Add("Cache-Control", "no-cache")
                    response.SendChunked <- true
                    let writer = new StreamWriter(response.OutputStream, UTF8Encoding false)
                    // The browser's own default retry is three seconds.
                    writer.Write "retry: 400\n\n"
                    writer.Write ": connected\n\n"
                    writer.Flush()
                    clients[Guid.NewGuid()] <- writer
                else
                    match resolve context.Request.Url.AbsolutePath with
                    | Some path ->
                        let extension = AbsolutePath.extension path
                        response.ContentType <- contentType extension

                        if extension = ".html" then
                            let html = File.ReadAllText(AbsolutePath.value path)

                            let withReload =
                                if html.Contains "</body>" then
                                    html.Replace("</body>", reloadScript + "</body>")
                                else
                                    html + reloadScript

                            let bytes = Encoding.UTF8.GetBytes withReload
                            response.ContentLength64 <- int64 bytes.Length
                            response.OutputStream.Write(bytes, 0, bytes.Length)
                        else
                            let bytes = File.ReadAllBytes(AbsolutePath.value path)
                            response.ContentLength64 <- int64 bytes.Length
                            response.OutputStream.Write(bytes, 0, bytes.Length)

                        response.Close()
                    | None ->
                        response.StatusCode <- 404
                        response.ContentType <- "text/html; charset=utf-8"

                        let body =
                            match resolve "/404.html" with
                            | Some page -> File.ReadAllText(AbsolutePath.value page)
                            | None ->
                                [
                                    "<!doctype html>"
                                    """<meta charset="utf-8">"""
                                    "<title>404</title>"
                                    """<body style="font-family:system-ui;padding:2rem">"""
                                    "<h1>404</h1>"
                                    $"<p>Nothing is built at <code>%s{context.Request.Url.AbsolutePath}</code>.</p>"
                                    "</body>"
                                ]
                                |> String.concat ""

                        let withReload =
                            if body.Contains "</body>" then
                                body.Replace("</body>", reloadScript + "</body>")
                            else
                                body + reloadScript

                        let bytes = Encoding.UTF8.GetBytes withReload
                        response.ContentLength64 <- int64 bytes.Length
                        response.OutputStream.Write(bytes, 0, bytes.Length)
                        response.Close()
            with _ ->
                try
                    response.Abort()
                with _ ->
                    ()
        }

    member _.Url =
        let path =
            if normalizedBase = "/" then
                "/"
            else
                normalizedBase + "/"

        let name =
            if host = "+" || host = "*" || host = "0.0.0.0" then
                "localhost"
            else
                host

        $"http://%s{name}:%i{port}%s{path}"

    member _.Start() =
        listener.Prefixes.Add $"http://%s{host}:%i{port}/"
        listener.Start()

        async {
            while not cancellation.IsCancellationRequested do
                let! context = listener.GetContextAsync() |> Async.AwaitTask
                Async.Start(handle context, cancellation.Token)
        }
        |> fun loop -> Async.Start(loop, cancellation.Token)

    /// <summary>Tell every open page to reload.</summary>
    member _.NotifyReload() = sendReload ()

    interface IDisposable with
        member _.Dispose() =
            cancellation.Cancel()

            for entry in clients do
                try
                    entry.Value.Dispose()
                with _ ->
                    ()

            listener.Close()
