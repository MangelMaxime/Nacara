/// Addition interop functions
module Interop

open Fable.Core.JsInterop
open Node

/// <summary>
/// Dynamically load a module from an absolute path or relative path or an NPM package.
///
/// The function will take care of transforming the importPath if needed
/// </summary>
/// <param name="cwd">Current working directory to use when transforming a relative path</param>
/// <param name="importPath">Path to import, it can be absolute, relative or an NPM package</param>
/// <typeparam name="'a"></typeparam>
/// <returns>A promise loading the provided module</returns>
let importDynamic (cwd : string) (importPath : string) =
    let importPath =
        // This is an absolute path, we need to prefix it with "file://" for Windows
        if path.isAbsolute importPath then
            path.join("file://", importPath)
        // This is a relative path, we need to compute the absolute path and prefix it with "file://" for Windows
        else if importPath.StartsWith("./") then
            path.join("file://", cwd, importPath)
        // This is a package path, nothing to do
        else
            importPath

    importDynamic importPath
