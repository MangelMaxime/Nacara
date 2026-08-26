module EasyBuild.Workspace

open EasyBuild.FileSystemProvider

[<Literal>]
let root = __SOURCE_DIRECTORY__ + "/../"

type Workspace = AbsoluteFileSystem<root>

type VirtualWorkspace =
    VirtualFileSystem<
        root,
        """
src/
    Nacara.Plugin.Highlight.TreeSitter/
        runtimes/
"""
     >
