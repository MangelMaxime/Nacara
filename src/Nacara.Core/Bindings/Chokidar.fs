module Chokidar

open Fable.Core

module Events =
    let [<Literal>] Add = "add"
    let [<Literal>] AddDir = "addDir"
    let [<Literal>] Change = "change"
    let [<Literal>] Unlink = "unlink"
    let [<Literal>] UnlinkDir = "unlinkDir"
    let [<Literal>] Ready = "ready"
    let [<Literal>] Raw = "raw"
    let [<Literal>] Error = "error"
    let [<Literal>] All = "all"

type [<AllowNullLiteral>] AwaitWriteFinishOptions =
    /// Amount of time in milliseconds for a file size to remain constant before emitting its event.
    abstract stabilityThreshold: float with get, set
    /// File size polling interval.
    abstract pollInterval: float with get, set

type [<AllowNullLiteral>] IOptions =
    /// (regexp or function) files to be ignored. This function or regexp is tested against the whole path, not just filename.
    /// If it is a function with two arguments, it gets called twice per path - once with a single argument (the path), second time with two arguments (the path and the fs.Stats object of that path).
    abstract ignored: obj with get, set
    /// (default: false). Indicates whether the process should continue to run as long as files are being watched.
    abstract persistent: bool with get, set
    /// (default: false). Indicates whether to watch files that don't have read permissions.
    abstract ignorePermissionErrors: bool with get, set
    /// (default: false). Indicates whether chokidar should ignore the initial add events or not.
    abstract ignoreInitial: bool with get, set
    /// (default: 100). Interval of file system polling.
    abstract interval: float with get, set
    /// (default: 300). Interval of file system polling for binary files (see extensions in src/is-binary).
    abstract binaryInterval: float with get, set
    /// (default: false on Windows, true on Linux and OS X). Whether to use fs.watchFile (backed by polling), or fs.watch. If polling leads to high CPU utilization, consider setting this to false.
    abstract usePolling: bool with get, set
    /// (default: true on OS X). Whether to use the fsevents watching interface if available. When set to true explicitly and fsevents is available this supercedes the usePolling setting. When set to false on OS X, usePolling: true becomes the default.
    abstract useFsEvents: bool with get, set
    /// (default: true). When false, only the symlinks themselves will be watched for changes instead of following the link references and bubbling events through the link's path.
    abstract followSymlinks: bool with get, set
    /// (default: false). If set to true then the strings passed to .watch() and .add() are treated as literal path names, even if they look like globs.
    abstract disableGlobbing: bool with get, set
    /// can be set to an object in order to adjust timing params:
    abstract awaitWriteFinish: U2<AwaitWriteFinishOptions, bool> with get, set

type [<AllowNullLiteral>] FSWatcher =
    abstract add: fileDirOrGlob: string -> unit
    abstract add: filesDirsOrGlobs: string array -> unit
    abstract unwatch: fileDirOrGlob: string -> unit
    abstract unwatch: filesDirsOrGlobs: string array -> unit
    /// Listen for an FS event. Available events: add, addDir, change, unlink, unlinkDir, error. Additionally all is available which gets emitted for every non-error event.
    abstract on: ``event``: string * clb: (string -> string -> unit) -> unit
    abstract on: ``event``: string * clb: (exn -> unit) -> unit
    /// Removes all listeners from watched files.
    abstract close: unit -> unit
    abstract options: IOptions with get, set

type [<AllowNullLiteral>] IExports =
    /// takes paths to be watched recursively and options
    abstract watch: path: string * ?options: IOptions -> FSWatcher
    abstract watch: paths: string array * ?options: IOptions -> FSWatcher

let [<Import("default","chokidar")>] chokidar : IExports = jsNative
