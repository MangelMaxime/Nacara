namespace Nacara.Plugins.Internal

open System
open System.IO
open System.Reflection
open System.Runtime.InteropServices

/// <summary>A node of a parse tree, as the C library hands it over.</summary>
[<Struct; StructLayout(LayoutKind.Sequential)>]
type TSNode =
    val mutable context0: uint32
    val mutable context1: uint32
    val mutable context2: uint32
    val mutable context3: uint32
    val mutable id: nativeint
    val mutable tree: nativeint

[<Struct; StructLayout(LayoutKind.Sequential)>]
type TSWasmError =
    val mutable kind: int
    val mutable message: nativeint

[<Struct; StructLayout(LayoutKind.Sequential)>]
type TSQueryCapture =
    val mutable node: TSNode
    val mutable index: uint32

[<Struct; StructLayout(LayoutKind.Sequential)>]
type TSQueryMatch =
    val mutable id: uint32
    val mutable patternIndex: uint16
    val mutable captureCount: uint16
    val mutable captures: nativeint

/// <summary>One step of a query predicate: a name, a capture it names, or a literal.</summary>
[<Struct; StructLayout(LayoutKind.Sequential)>]
type TSQueryPredicateStep =
    val mutable kind: uint32
    val mutable valueId: uint32

/// <summary>
/// The C library, as far as this plugin needs it.
/// </summary>
/// <remarks>
/// Expects a core built with <c>TREE_SITTER_FEATURE_WASM</c> and wasmtime beside it, wherever
/// <c>TreeSitterOptions.RuntimePath</c> says they are. No published .NET binding carries one.
/// </remarks>
module Native =

    [<Literal>]
    let core = "tree-sitter"

    [<Literal>]
    let runtime = "wasmtime"

    [<DllImport(runtime)>]
    extern nativeint wasm_engine_new()

    [<DllImport(core)>]
    extern nativeint ts_wasm_store_new(nativeint engine, TSWasmError& error)

    [<DllImport(core)>]
    extern nativeint ts_wasm_store_load_language(
        nativeint store,
        string name,
        byte[] wasm,
        uint32 length,
        TSWasmError& error
    )

    [<DllImport(core)>]
    extern nativeint ts_parser_new()

    [<DllImport(core)>]
    extern void ts_parser_delete(nativeint parser)

    [<DllImport(core)>]
    extern void ts_parser_set_wasm_store(nativeint parser, nativeint store)

    [<DllImport(core)>]
    extern bool ts_parser_set_language(nativeint parser, nativeint language)

    [<DllImport(core)>]
    extern nativeint ts_parser_parse_string(
        nativeint parser,
        nativeint oldTree,
        byte[] source,
        uint32 length
    )

    [<DllImport(core)>]
    extern void ts_tree_delete(nativeint tree)

    [<DllImport(core)>]
    extern TSNode ts_tree_root_node(nativeint tree)

    [<DllImport(core)>]
    extern uint32 ts_node_start_byte(TSNode node)

    [<DllImport(core)>]
    extern uint32 ts_node_end_byte(TSNode node)

    /// <summary>Whether the parser had to guess anywhere inside this node.</summary>
    [<DllImport(core)>]
    [<return: MarshalAs(UnmanagedType.I1)>]
    extern bool ts_node_has_error(TSNode node)

    [<DllImport(core)>]
    extern nativeint ts_query_new(
        nativeint language,
        byte[] source,
        uint32 length,
        uint32& errorOffset,
        int& errorType
    )

    [<DllImport(core)>]
    extern nativeint ts_query_cursor_new()

    [<DllImport(core)>]
    extern void ts_query_cursor_delete(nativeint cursor)

    [<DllImport(core)>]
    extern void ts_query_cursor_exec(nativeint cursor, nativeint query, TSNode node)

    [<DllImport(core)>]
    extern bool ts_query_cursor_next_match(nativeint cursor, TSQueryMatch& found)

    [<DllImport(core)>]
    extern nativeint ts_query_capture_name_for_id(nativeint query, uint32 index, uint32& length)

    [<DllImport(core)>]
    extern nativeint ts_query_string_value_for_id(nativeint query, uint32 index, uint32& length)

    [<DllImport(core)>]
    extern nativeint ts_query_predicates_for_pattern(
        nativeint query,
        uint32 patternIndex,
        uint32& stepCount
    )

    /// <summary>Where the two native libraries are, when they are not where the loader looks.</summary>
    let mutable private directory: string option = None

    let private candidates (name: string) =
        [
            $"lib%s{name}.so"
            $"lib%s{name}.dylib"
            $"%s{name}.dll"
            $"lib%s{name}-wasm.so"
            $"lib%s{name}-wasm.dylib"
            $"%s{name}-wasm.dll"
        ]

    let private resolve (name: string) (assembly: Assembly) (path: DllImportSearchPath Nullable) =
        match directory with
        | Some where ->
            candidates name
            |> List.map (fun candidate -> Path.Combine(where, candidate))
            |> List.tryFind File.Exists
            |> Option.map NativeLibrary.Load
            |> Option.defaultValue 0n
        | None -> 0n

    /// <summary>Look for the native libraries in this directory before anywhere else.</summary>
    let mutable private registered = false
    let private gate = obj ()

    let lookIn (where: string) =
        // A resolver can be set once for an assembly; setting it twice throws.
        lock
            gate
            (fun () ->
                if directory <> Some where then
                    directory <- Some where

                    if not registered then
                        NativeLibrary.SetDllImportResolver(
                            Assembly.GetExecutingAssembly(),
                            resolve
                        )

                        registered <- true

                    candidates runtime
                    |> List.map (fun candidate -> Path.Combine(where, candidate))
                    |> List.tryFind File.Exists
                    |> Option.iter (NativeLibrary.Load >> ignore)
            )
