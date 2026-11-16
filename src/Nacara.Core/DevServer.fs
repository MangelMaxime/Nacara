namespace Nacara.Core

open System
open System.IO
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading
open System.Collections.Concurrent

/// <summary>
/// The development server.
/// </summary>
/// <remarks>The reload script is injected into HTML responses on the way out, so nothing about
/// the built output differs between <c>build</c> and <c>serve</c>.</remarks>
type DevServer(root: AbsolutePath, basePath: string, host: string, port: int) =

    let mutable listener = new HttpListener()
    let mutable servedPort = port
    let clients = ConcurrentDictionary<Guid, StreamWriter>()
    let cancellation = new CancellationTokenSource()

    let mutable problems = "[]"

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

  const style = `
    :host { all: initial; }
    .panel { position: fixed; inset: auto 12px 12px 12px; max-height: 60vh; overflow: auto; z-index: 2147483647;
      background: #1b1b1f; color: #e8e8ea; border: 1px solid #3a3a40; border-radius: 8px; padding: 12px 16px;
      font: 13px/1.5 ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; box-shadow: 0 8px 32px rgb(0 0 0 / 0.4); }
    .head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px;
      font-family: system-ui, sans-serif; font-weight: 600; }
    .close, .pill { font: inherit; cursor: pointer; border: 1px solid #3a3a40; border-radius: 6px;
      background: #2a2a30; color: inherit; padding: 2px 8px; }
    .pill { position: fixed; right: 12px; bottom: 12px; z-index: 2147483647; font: 600 12px system-ui, sans-serif; }
    .item { padding: 8px 0; border-top: 1px solid #2f2f35; }
    .item:first-of-type { border-top: 0; }
    .error .label { color: #ff7b72; } .warning .label { color: #e3b341; } .info .label { color: #79c0ff; }
    .label { font-weight: 700; } .where { color: #9a9aa3; } a.where { text-decoration: underline; } .message { white-space: pre-wrap; }
    .hint { color: #9a9aa3; white-space: pre-wrap; margin-top: 2px; }`;

  const element = (tag, className, text) => {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (text) node.textContent = text;
    return node;
  };

  const show = (items) => {
    if (items.length === 0) return;

    const host = document.createElement("nacara-problems");
    const root = host.attachShadow({ mode: "open" });
    root.append(element("style", "", style));

    const errors = items.filter((item) => item.severity === "error").length;
    const warnings = items.length - errors;
    const count = (n, word) => `${n} ${word}${n === 1 ? "" : "s"}`;
    const title = [errors && count(errors, "error"), warnings && count(warnings, "warning")]
      .filter(Boolean)
      .join(", ");

    const panel = element("section", "panel");
    panel.setAttribute("role", "alert");

    const head = element("div", "head", `The last build reported ${title}`);
    const close = element("button", "close", "Close");
    head.append(close);
    panel.append(head);

    for (const item of items) {
      const row = element("div", `item ${item.severity}`);
      const first = element("div");
      first.append(element("span", "label", `${item.severity} ${item.code}`));
      if (item.location) {
        const where = element(item.editor ? "a" : "span", "where", item.location);
        if (item.editor) where.href = item.editor;
        first.append(" ", where);
      }
      row.append(first, element("div", "message", item.message));
      if (item.hint) row.append(element("div", "hint", item.hint));
      panel.append(row);
    }

    const pill = element("button", "pill", title);
    const open = (shown) => {
      panel.hidden = !shown;
      pill.hidden = shown;
    };

    close.onclick = () => open(false);
    pill.onclick = () => open(true);
    document.addEventListener("keydown", (event) => {
      if (event.key === "Escape") open(false);
    });

    root.append(panel, pill);
    open(errors > 0);
    document.body.append(host);
  };

  fetch("/__nacara/problems", { cache: "no-store" })
    .then((answer) => answer.json())
    .then(show)
    .catch(() => {});
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
                if context.Request.Url.AbsolutePath.EndsWith "/__nacara/problems" then
                    response.ContentType <- "application/json; charset=utf-8"
                    response.Headers.Add("Cache-Control", "no-store")
                    let bytes = Encoding.UTF8.GetBytes problems
                    response.ContentLength64 <- int64 bytes.Length
                    response.OutputStream.Write(bytes, 0, bytes.Length)
                    response.Close()
                elif context.Request.Url.AbsolutePath.EndsWith "/__nacara/reload" then
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

    /// <summary>Whether anything already holds the port, on either loopback.</summary>
    /// <remarks>A prefix binds only the address its host resolves to.</remarks>
    let taken (candidate: int) =
        host = "localhost"
        && [
            IPAddress.Loopback
            if Socket.OSSupportsIPv6 then
                IPAddress.IPv6Loopback
           ]
           |> List.exists (fun address ->
               try
                   use socket =
                       new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)

                   socket.Bind(IPEndPoint(address, candidate))
                   false
               with :? SocketException ->
                   true
           )

    /// <summary>How many ports are tried, counting the one asked for.</summary>
    static member PortAttempts = 10

    /// <summary>The port it is listening on.</summary>
    member _.Port = servedPort

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

        $"http://%s{name}:%i{servedPort}%s{path}"

    member _.Start() =
        let rec listen (candidate: int) =
            let last = candidate >= port + DevServer.PortAttempts - 1

            if taken candidate then
                if last then
                    raise (HttpListenerException(int SocketError.AddressAlreadyInUse))
                else
                    listen (candidate + 1)
            else

                listener <- new HttpListener()
                listener.Prefixes.Add $"http://%s{host}:%i{candidate}/"

                try
                    listener.Start()
                    servedPort <- candidate
                with :? HttpListenerException when not last ->
                    listener.Close()
                    listen (candidate + 1)

        listen port

        async {
            while not cancellation.IsCancellationRequested do
                let! context = listener.GetContextAsync() |> Async.AwaitTask
                Async.Start(handle context, cancellation.Token)
        }
        |> fun loop -> Async.Start(loop, cancellation.Token)

    /// <summary>Tell every open page to reload.</summary>
    member _.NotifyReload() = sendReload ()

    /// <summary>What the last build reported, shown over every page it serves.</summary>
    /// <param name="projectRoot">What the locations are written relative to.</param>
    /// <param name="diagnostics">What the build reported.</param>
    member _.ShowProblems(projectRoot: AbsolutePath, diagnostics: Diagnostic list) =
        let severity =
            function
            | Severity.Error -> "error"
            | Severity.Warning -> "warning"
            | Severity.Information -> "info"

        problems <-
            diagnostics
            |> List.filter (fun diagnostic -> diagnostic.Severity <> Severity.Information)
            |> List.sortBy (fun diagnostic -> diagnostic.Severity <> Severity.Error)
            |> List.map (fun diagnostic ->
                {|
                    severity = severity diagnostic.Severity
                    code = diagnostic.Code
                    message = diagnostic.Message
                    hint = diagnostic.Hint |> Option.toObj
                    location =
                        diagnostic.Span
                        |> Option.map (fun span ->
                            let file =
                                Path.GetRelativePath(
                                    AbsolutePath.value projectRoot,
                                    AbsolutePath.value span.File
                                )

                            $"%s{file}(%i{span.Line},%i{span.Column})"
                        )
                        |> Option.toObj
                    editor = diagnostic.Span |> Option.bind Log.editorUrl |> Option.toObj
                |}
            )
            |> System.Text.Json.JsonSerializer.Serialize

    interface IDisposable with
        member _.Dispose() =
            cancellation.Cancel()

            for entry in clients do
                try
                    entry.Value.Dispose()
                with _ ->
                    ()

            listener.Close()
