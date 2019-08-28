/// WARNING:
/// This module isn't a complete port of the LiveServer.
/// Some feature might be missing
module LiveServer

open Fable.Core

type Middleware = Node.Http.ServerRequest -> Node.Http.ServerResponse -> System.Delegate -> unit

type Options =
    /// Set the server port. Defaults to 8080.
    abstract port: int with get, set
    /// Set the address to bind to. Defaults to 0.0.0.0 or process.env.IP.
    abstract host: string with get, set
    /// Set root directory that's being served. Defaults to cwd.
    abstract root: string with get, set
    /// When false, it won't load your browser by default.
    abstract ``open``: bool with get, set
    /// comma-separated string for paths to ignore
    abstract ignore: string with get, set
    /// When set, serve this file (server root relative) for every 404 (useful for single-page applications)
    abstract file: string with get, set
    /// Waits for all changes, before reloading. Defaults to 0 sec.
    abstract wait: int with get, set
    /// Mount directories onto a route, e.g. [['/components', './node_modules']].
    abstract mount: string array with get, set
    /// 0 = errors only, 1 = some, 2 = lots
    abstract logLevel: int with get, set
    /// Takes an array of Connect-compatible middleware that are injected into the server middleware stack
    abstract middleware: Middleware array with get, set

type [<AllowNullLiteral>] LiveServer =
    abstract start: ?options : Options -> Node.Http.Server
    [<Emit("$0.start($1)")>]
    abstract startHttps: ?options : Options -> Node.Https.Server
    abstract shutdown: unit -> unit

let [<Import("default","live-server")>] liveServer : LiveServer = jsNative
