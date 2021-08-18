// ts2fable 0.8.0
module rec Ws
open System
open Fable.Core
open Fable.Core.JS
open Node

type Error = System.Exception
type Symbol = obj

type EventEmitter = Events.EventEmitter
type Agent = Http.Agent
type ClientRequest = Http.ClientRequest<obj>
type ClientRequestArgs = obj //Http.ClientRequestArgs
type IncomingMessage = Http.IncomingMessage
type OutgoingHttpHeaders = obj //Http.OutgoingHttpHeaders
type HTTPServer = Http.Server
type HTTPSServer = Https.Server
type Socket = Net.Socket
type Duplex = Stream.Duplex<obj,obj>
type DuplexOptions = Stream.DuplexOptions<obj>
type SecureContextOptions = Tls.SecureContextOptions
type URL = Url.URL
type ZlibOptions = obj //Zlib.ZlibOptions
let [<Import("*","ws")>] webSocket: WebSocket.IExports = jsNative

type [<AllowNullLiteral>] IExports =
    abstract WebSocket: WebSocketStatic

type [<AllowNullLiteral>] WebSocket =
    inherit EventEmitter
    abstract binaryType: WebSocketBinaryType with get, set
    abstract bufferedAmount: float
    abstract extensions: string
    abstract protocol: string
    /// The current state of the connection
    abstract readyState: obj
    abstract url: string
    /// The connection is not yet open.
    abstract CONNECTING: obj with get, set
    /// The connection is open and ready to communicate.
    abstract OPEN: obj with get, set
    /// The connection is in the process of closing.
    abstract CLOSING: obj with get, set
    /// The connection is closed.
    abstract CLOSED: obj with get, set
    abstract onopen: (WebSocket.OpenEvent -> unit) with get, set
    abstract onerror: (WebSocket.ErrorEvent -> unit) with get, set
    abstract onclose: (WebSocket.CloseEvent -> unit) with get, set
    abstract onmessage: (WebSocket.MessageEvent -> unit) with get, set
    abstract close: ?code: float * ?data: string -> unit
    abstract ping: ?data: obj * ?mask: bool * ?cb: (Error -> unit) -> unit
    abstract pong: ?data: obj * ?mask: bool * ?cb: (Error -> unit) -> unit
    abstract send: ?data: obj * ?cb: ((Error) option -> unit) -> unit
    abstract send: ?data: obj * options: WebSocketSendOptions * ?cb: ((Error) option -> unit) -> unit
    abstract terminate: unit -> unit
    [<Emit "$0.addEventListener('message',$1,$2)">] abstract addEventListener_message: cb: (WebSocketAddEventListener_message -> unit) * ?options: WebSocket.EventListenerOptions -> unit
    [<Emit "$0.addEventListener('close',$1,$2)">] abstract addEventListener_close: cb: (WebSocketAddEventListener_close -> unit) * ?options: WebSocket.EventListenerOptions -> unit
    [<Emit "$0.addEventListener('error',$1,$2)">] abstract addEventListener_error: cb: (WebSocketAddEventListener_error -> unit) * ?options: WebSocket.EventListenerOptions -> unit
    [<Emit "$0.addEventListener('open',$1,$2)">] abstract addEventListener_open: cb: (WebSocketAddEventListener_open -> unit) * ?options: WebSocket.EventListenerOptions -> unit
    abstract addEventListener: method: string * listener: (unit -> unit) * ?options: WebSocket.EventListenerOptions -> unit
    [<Emit "$0.removeEventListener('message',$1)">] abstract removeEventListener_message: ?cb: (WebSocketAddEventListener_message -> unit) -> unit
    [<Emit "$0.removeEventListener('close',$1)">] abstract removeEventListener_close: ?cb: (WebSocketAddEventListener_close -> unit) -> unit
    [<Emit "$0.removeEventListener('error',$1)">] abstract removeEventListener_error: ?cb: (WebSocketAddEventListener_error -> unit) -> unit
    [<Emit "$0.removeEventListener('open',$1)">] abstract removeEventListener_open: ?cb: (WebSocketAddEventListener_open -> unit) -> unit
    abstract removeEventListener: method: string * ?listener: (unit -> unit) -> unit
    [<Emit "$0.on('close',$1)">] abstract on_close: listener: (WebSocket -> float -> string -> unit) -> WebSocket
    [<Emit "$0.on('error',$1)">] abstract on_error: listener: (WebSocket -> Error -> unit) -> WebSocket
    [<Emit "$0.on('upgrade',$1)">] abstract on_upgrade: listener: (WebSocket -> IncomingMessage -> unit) -> WebSocket
    [<Emit "$0.on('message',$1)">] abstract on_message: listener: (WebSocket -> WebSocket.Data -> unit) -> WebSocket
    [<Emit "$0.on('open',$1)">] abstract on_open: listener: (WebSocket -> unit) -> WebSocket
    abstract on: ``event``: WebSocketOnEvent * listener: (WebSocket -> Buffer -> unit) -> WebSocket
    [<Emit "$0.on('unexpected-response',$1)">] abstract ``on_unexpected-response``: listener: (WebSocket -> ClientRequest -> IncomingMessage -> unit) -> WebSocket
    // abstract on: ``event``: U2<string, Symbol> * listener: (WebSocket -> ResizeArray<obj option> -> unit) -> WebSocket
    abstract on: ``event``: string * listener: (WebSocket -> ResizeArray<obj option> -> unit) -> WebSocket
    abstract on: ``event``: Symbol * listener: (WebSocket -> ResizeArray<obj option> -> unit) -> WebSocket
    [<Emit "$0.once('close',$1)">] abstract once_close: listener: (WebSocket -> float -> string -> unit) -> WebSocket
    [<Emit "$0.once('error',$1)">] abstract once_error: listener: (WebSocket -> Error -> unit) -> WebSocket
    [<Emit "$0.once('upgrade',$1)">] abstract once_upgrade: listener: (WebSocket -> IncomingMessage -> unit) -> WebSocket
    [<Emit "$0.once('message',$1)">] abstract once_message: listener: (WebSocket -> WebSocket.Data -> unit) -> WebSocket
    [<Emit "$0.once('open',$1)">] abstract once_open: listener: (WebSocket -> unit) -> WebSocket
    abstract once: ``event``: WebSocketOnceEvent * listener: (WebSocket -> Buffer -> unit) -> WebSocket
    [<Emit "$0.once('unexpected-response',$1)">] abstract ``once_unexpected-response``: listener: (WebSocket -> ClientRequest -> IncomingMessage -> unit) -> WebSocket
    abstract once: ``event``: U2<string, Symbol> * listener: (WebSocket -> ResizeArray<obj option> -> unit) -> WebSocket
    [<Emit "$0.off('close',$1)">] abstract off_close: listener: (WebSocket -> float -> string -> unit) -> WebSocket
    [<Emit "$0.off('error',$1)">] abstract off_error: listener: (WebSocket -> Error -> unit) -> WebSocket
    [<Emit "$0.off('upgrade',$1)">] abstract off_upgrade: listener: (WebSocket -> IncomingMessage -> unit) -> WebSocket
    [<Emit "$0.off('message',$1)">] abstract off_message: listener: (WebSocket -> WebSocket.Data -> unit) -> WebSocket
    [<Emit "$0.off('open',$1)">] abstract off_open: listener: (WebSocket -> unit) -> WebSocket
    abstract off: ``event``: WebSocketOffEvent * listener: (WebSocket -> Buffer -> unit) -> WebSocket
    [<Emit "$0.off('unexpected-response',$1)">] abstract ``off_unexpected-response``: listener: (WebSocket -> ClientRequest -> IncomingMessage -> unit) -> WebSocket
    abstract off: ``event``: U2<string, Symbol> * listener: (WebSocket -> ResizeArray<obj option> -> unit) -> WebSocket
    [<Emit "$0.addListener('close',$1)">] abstract addListener_close: listener: (float -> string -> unit) -> WebSocket
    [<Emit "$0.addListener('error',$1)">] abstract addListener_error: listener: (Error -> unit) -> WebSocket
    [<Emit "$0.addListener('upgrade',$1)">] abstract addListener_upgrade: listener: (IncomingMessage -> unit) -> WebSocket
    [<Emit "$0.addListener('message',$1)">] abstract addListener_message: listener: (WebSocket.Data -> unit) -> WebSocket
    [<Emit "$0.addListener('open',$1)">] abstract addListener_open: listener: (unit -> unit) -> WebSocket
    abstract addListener: ``event``: WebSocketAddListenerEvent * listener: (Buffer -> unit) -> WebSocket
    [<Emit "$0.addListener('unexpected-response',$1)">] abstract ``addListener_unexpected-response``: listener: (ClientRequest -> IncomingMessage -> unit) -> WebSocket
    abstract addListener: ``event``: U2<string, Symbol> * listener: (ResizeArray<obj option> -> unit) -> WebSocket
    [<Emit "$0.removeListener('close',$1)">] abstract removeListener_close: listener: (float -> string -> unit) -> WebSocket
    [<Emit "$0.removeListener('error',$1)">] abstract removeListener_error: listener: (Error -> unit) -> WebSocket
    [<Emit "$0.removeListener('upgrade',$1)">] abstract removeListener_upgrade: listener: (IncomingMessage -> unit) -> WebSocket
    [<Emit "$0.removeListener('message',$1)">] abstract removeListener_message: listener: (WebSocket.Data -> unit) -> WebSocket
    [<Emit "$0.removeListener('open',$1)">] abstract removeListener_open: listener: (unit -> unit) -> WebSocket
    abstract removeListener: ``event``: WebSocketRemoveListenerEvent * listener: (Buffer -> unit) -> WebSocket
    [<Emit "$0.removeListener('unexpected-response',$1)">] abstract ``removeListener_unexpected-response``: listener: (ClientRequest -> IncomingMessage -> unit) -> WebSocket
    abstract removeListener: ``event``: U2<string, Symbol> * listener: (ResizeArray<obj option> -> unit) -> WebSocket

type [<AllowNullLiteral>] WebSocketSendOptions =
    abstract mask: bool option with get, set
    abstract binary: bool option with get, set
    abstract compress: bool option with get, set
    abstract fin: bool option with get, set

type [<StringEnum>] [<RequireQualifiedAccess>] WebSocketOnEvent =
    | Ping
    | Pong

type [<StringEnum>] [<RequireQualifiedAccess>] WebSocketOnceEvent =
    | Ping
    | Pong

type [<StringEnum>] [<RequireQualifiedAccess>] WebSocketOffEvent =
    | Ping
    | Pong

type [<StringEnum>] [<RequireQualifiedAccess>] WebSocketAddListenerEvent =
    | Ping
    | Pong

type [<StringEnum>] [<RequireQualifiedAccess>] WebSocketRemoveListenerEvent =
    | Ping
    | Pong

type [<AllowNullLiteral>] WebSocketStatic =
    /// The connection is not yet open.
    abstract CONNECTING: obj with get, set
    /// The connection is open and ready to communicate.
    abstract OPEN: obj with get, set
    /// The connection is in the process of closing.
    abstract CLOSING: obj with get, set
    /// The connection is closed.
    abstract CLOSED: obj with get, set
    [<EmitConstructor>] abstract Create: address: U2<string, URL> * ?options: U2<WebSocket.ClientOptions, ClientRequestArgs> -> WebSocket
    [<EmitConstructor>] abstract Create: address: U2<string, URL> * ?protocols: U2<string, ResizeArray<string>> * ?options: U2<WebSocket.ClientOptions, ClientRequestArgs> -> WebSocket

module WebSocket =

    type [<AllowNullLiteral>] IExports =
        abstract Server: ServerStatic
        abstract createWebSocketStream: websocket: WebSocket * ?options: DuplexOptions -> Duplex

    /// Data represents the message payload received over the WebSocket.
    type Data =
        U4<string, Buffer, ArrayBuffer, ResizeArray<Buffer>>

    /// CertMeta represents the accepted types for certificate & key data.
    type CertMeta =
        U4<string, ResizeArray<string>, Buffer, ResizeArray<Buffer>>

    /// VerifyClientCallbackSync is a synchronous callback used to inspect the
    /// incoming message. The return value (boolean) of the function determines
    /// whether or not to accept the handshake.
    type [<AllowNullLiteral>] VerifyClientCallbackSync =
        /// VerifyClientCallbackSync is a synchronous callback used to inspect the
        /// incoming message. The return value (boolean) of the function determines
        /// whether or not to accept the handshake.
        [<Emit "$0($1...)">] abstract Invoke: info: VerifyClientCallbackSyncInvokeInfo -> bool

    type [<AllowNullLiteral>] VerifyClientCallbackSyncInvokeInfo =
        abstract origin: string with get, set
        abstract secure: bool with get, set
        abstract req: IncomingMessage with get, set

    /// VerifyClientCallbackAsync is an asynchronous callback used to inspect the
    /// incoming message. The return value (boolean) of the function determines
    /// whether or not to accept the handshake.
    type [<AllowNullLiteral>] VerifyClientCallbackAsync =
        /// VerifyClientCallbackAsync is an asynchronous callback used to inspect the
        /// incoming message. The return value (boolean) of the function determines
        /// whether or not to accept the handshake.
        [<Emit "$0($1...)">] abstract Invoke: info: VerifyClientCallbackAsyncInvokeInfo * callback: (bool -> (float) option -> (string) option -> (OutgoingHttpHeaders) option -> unit) -> unit

    type [<AllowNullLiteral>] VerifyClientCallbackAsyncInvokeInfo =
        abstract origin: string with get, set
        abstract secure: bool with get, set
        abstract req: IncomingMessage with get, set

    type [<AllowNullLiteral>] ClientOptions =
        inherit SecureContextOptions
        abstract protocol: string option with get, set
        abstract followRedirects: bool option with get, set
        abstract handshakeTimeout: float option with get, set
        abstract maxRedirects: float option with get, set
        abstract perMessageDeflate: U2<bool, PerMessageDeflateOptions> option with get, set
        abstract localAddress: string option with get, set
        abstract protocolVersion: float option with get, set
        abstract headers: ClientOptionsHeaders option with get, set
        abstract origin: string option with get, set
        abstract agent: Agent option with get, set
        abstract host: string option with get, set
        abstract family: float option with get, set
        abstract checkServerIdentity: servername: string * cert: CertMeta -> bool
        abstract rejectUnauthorized: bool option with get, set
        abstract maxPayload: float option with get, set

    type [<AllowNullLiteral>] PerMessageDeflateOptions =
        abstract serverNoContextTakeover: bool option with get, set
        abstract clientNoContextTakeover: bool option with get, set
        abstract serverMaxWindowBits: float option with get, set
        abstract clientMaxWindowBits: float option with get, set
        abstract zlibDeflateOptions: PerMessageDeflateOptionsZlibDeflateOptions option with get, set
        abstract zlibInflateOptions: ZlibOptions option with get, set
        abstract threshold: float option with get, set
        abstract concurrencyLimit: float option with get, set

    type [<AllowNullLiteral>] OpenEvent =
        abstract ``type``: string with get, set
        abstract target: WebSocket with get, set

    type [<AllowNullLiteral>] ErrorEvent =
        abstract error: obj option with get, set
        abstract message: string with get, set
        abstract ``type``: string with get, set
        abstract target: WebSocket with get, set

    type [<AllowNullLiteral>] CloseEvent =
        abstract wasClean: bool with get, set
        abstract code: float with get, set
        abstract reason: string with get, set
        abstract ``type``: string with get, set
        abstract target: WebSocket with get, set

    type [<AllowNullLiteral>] MessageEvent =
        abstract data: Data with get, set
        abstract ``type``: string with get, set
        abstract target: WebSocket with get, set

    type [<AllowNullLiteral>] EventListenerOptions =
        abstract once: bool option with get, set

    type [<AllowNullLiteral>] ServerOptions =
        abstract host: string with get, set
        abstract port: float with get, set
        abstract backlog: float with get, set
        abstract server: U2<HTTPServer, HTTPSServer> with get, set
        abstract verifyClient: U2<VerifyClientCallbackAsync, VerifyClientCallbackSync> with get, set
        abstract handleProtocols: obj with get, set
        abstract path: string with get, set
        abstract noServer: bool with get, set
        abstract clientTracking: bool with get, set
        abstract perMessageDeflate: U2<bool, PerMessageDeflateOptions> with get, set
        abstract maxPayload: float with get, set

    type [<AllowNullLiteral>] AddressInfo =
        abstract address: string with get, set
        abstract family: string with get, set
        abstract port: float with get, set

    type [<AllowNullLiteral>] Server =
        inherit EventEmitter
        abstract options: ServerOptions with get, set
        abstract path: string with get, set
        abstract clients: Set<WebSocket> with get, set
        abstract address: unit -> U2<AddressInfo, string>
        abstract close: ?cb: ((Error) option -> unit) -> unit
        abstract handleUpgrade: request: IncomingMessage * socket: Socket * upgradeHead: Buffer * callback: (WebSocket -> IncomingMessage -> unit) -> unit
        abstract shouldHandle: request: IncomingMessage -> U2<bool, Promise<bool>>
        [<Emit "$0.on('connection',$1)">] abstract on_connection: cb: (Server -> WebSocket -> IncomingMessage -> unit) -> Server
        [<Emit "$0.on('error',$1)">] abstract on_error: cb: (Server -> Error -> unit) -> Server
        [<Emit "$0.on('headers',$1)">] abstract on_headers: cb: (Server -> ResizeArray<string> -> IncomingMessage -> unit) -> Server
        abstract on: ``event``: ServerOnEvent * cb: (Server -> unit) -> Server
        // abstract on: ``event``: U2<string, Symbol> * listener: (Server -> ResizeArray<obj option> -> unit) -> Server
        abstract on: ``event``: string * listener: (Server -> ResizeArray<obj option> -> unit) -> Server
        abstract on: ``event``: Symbol * listener: (Server -> ResizeArray<obj option> -> unit) -> Server
        [<Emit "$0.once('connection',$1)">] abstract once_connection: cb: (Server -> WebSocket -> IncomingMessage -> unit) -> Server
        [<Emit "$0.once('error',$1)">] abstract once_error: cb: (Server -> Error -> unit) -> Server
        [<Emit "$0.once('headers',$1)">] abstract once_headers: cb: (Server -> ResizeArray<string> -> IncomingMessage -> unit) -> Server
        abstract once: ``event``: ServerOnceEvent * cb: (Server -> unit) -> Server
        abstract once: ``event``: U2<string, Symbol> * listener: (ResizeArray<obj option> -> unit) -> Server
        [<Emit "$0.off('connection',$1)">] abstract off_connection: cb: (Server -> WebSocket -> IncomingMessage -> unit) -> Server
        [<Emit "$0.off('error',$1)">] abstract off_error: cb: (Server -> Error -> unit) -> Server
        [<Emit "$0.off('headers',$1)">] abstract off_headers: cb: (Server -> ResizeArray<string> -> IncomingMessage -> unit) -> Server
        abstract off: ``event``: ServerOffEvent * cb: (Server -> unit) -> Server
        abstract off: ``event``: U2<string, Symbol> * listener: (Server -> ResizeArray<obj option> -> unit) -> Server
        [<Emit "$0.addListener('connection',$1)">] abstract addListener_connection: cb: (WebSocket -> IncomingMessage -> unit) -> Server
        [<Emit "$0.addListener('error',$1)">] abstract addListener_error: cb: (Error -> unit) -> Server
        [<Emit "$0.addListener('headers',$1)">] abstract addListener_headers: cb: (ResizeArray<string> -> IncomingMessage -> unit) -> Server
        abstract addListener: ``event``: ServerAddListenerEvent * cb: (unit -> unit) -> Server
        abstract addListener: ``event``: U2<string, Symbol> * listener: (ResizeArray<obj option> -> unit) -> Server
        [<Emit "$0.removeListener('connection',$1)">] abstract removeListener_connection: cb: (WebSocket -> unit) -> Server
        [<Emit "$0.removeListener('error',$1)">] abstract removeListener_error: cb: (Error -> unit) -> Server
        [<Emit "$0.removeListener('headers',$1)">] abstract removeListener_headers: cb: (ResizeArray<string> -> IncomingMessage -> unit) -> Server
        abstract removeListener: ``event``: ServerRemoveListenerEvent * cb: (unit -> unit) -> Server
        abstract removeListener: ``event``: U2<string, Symbol> * listener: (ResizeArray<obj option> -> unit) -> Server

    type [<StringEnum>] [<RequireQualifiedAccess>] ServerOnEvent =
        | Close
        | Listening

    type [<StringEnum>] [<RequireQualifiedAccess>] ServerOnceEvent =
        | Close
        | Listening

    type [<StringEnum>] [<RequireQualifiedAccess>] ServerOffEvent =
        | Close
        | Listening

    type [<StringEnum>] [<RequireQualifiedAccess>] ServerAddListenerEvent =
        | Close
        | Listening

    type [<StringEnum>] [<RequireQualifiedAccess>] ServerRemoveListenerEvent =
        | Close
        | Listening

    type [<AllowNullLiteral>] ServerStatic =
        [<EmitConstructor>] abstract Create: ?options: ServerOptions * ?callback: (unit -> unit) -> Server

    type [<AllowNullLiteral>] ClientOptionsHeaders =
        [<EmitIndexer>] abstract Item: key: string -> string with get, set

    type [<AllowNullLiteral>] PerMessageDeflateOptionsZlibDeflateOptions =
        abstract flush: float option with get, set
        abstract finishFlush: float option with get, set
        abstract chunkSize: float option with get, set
        abstract windowBits: float option with get, set
        abstract level: float option with get, set
        abstract memLevel: float option with get, set
        abstract strategy: float option with get, set
        abstract dictionary: U3<Buffer, ResizeArray<Buffer>, DataView> option with get, set
        abstract info: bool option with get, set

type [<StringEnum>] [<RequireQualifiedAccess>] WebSocketBinaryType =
    | Nodebuffer
    | Arraybuffer
    | Fragments

type [<AllowNullLiteral>] WebSocketAddEventListener_message =
    abstract data: obj option with get, set
    abstract ``type``: string with get, set
    abstract target: WebSocket with get, set

type [<AllowNullLiteral>] WebSocketAddEventListener_close =
    abstract wasClean: bool with get, set
    abstract code: float with get, set
    abstract reason: string with get, set
    abstract target: WebSocket with get, set

type [<AllowNullLiteral>] WebSocketAddEventListener_error =
    abstract error: obj option with get, set
    abstract message: obj option with get, set
    abstract ``type``: string with get, set
    abstract target: WebSocket with get, set

type [<AllowNullLiteral>] WebSocketAddEventListener_open =
    abstract target: WebSocket with get, set
