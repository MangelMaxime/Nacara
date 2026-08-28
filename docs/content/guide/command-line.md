---
title: Command line
---

`Nacara.run` gives your site project its command line.

```bash frame="terminal"
dotnet run -- build      # build once
dotnet run -- watch      # build, serve, rebuild on change
dotnet run -- check      # build it all, write none of it
dotnet run -- clean      # delete the output directory and the .nacara cache
```

| Option | Effect |
|---|---|
| `--root <dir>` | Project root. Defaults to the directory of your site's project |
| `--port <n>` | Port used by `watch`. Defaults to 8080 |
| `--version <v>` | Deploy this build under a version prefix |
| `--strict` | Treat warnings as errors |
| `--verbose` | Log what the build is doing |

## Watch

`watch` rebuilds when content changes and reloads the page over server-sent events. It picks up
content changes in process, and only writes the files whose bytes actually changed, so the browser
does not reload for nothing.

Layouts and plugins are F# code, so changing them means recompiling. Let the SDK do it:

```bash frame="terminal"
dotnet watch --no-hot-reload run -- watch
```

`--no-hot-reload` is required: without it the SDK patches the running process instead of building
and starting the site again.

### Reaching it from another machine

`watch` listens on loopback only. Use `--host` to serve it on the network:

```bash frame="terminal"
dotnet run -- watch --host             # every interface
dotnet run -- watch --host 100.x.y.z   # one of them
```

Use `--host` on its own to reach the site from a phone on the same network, or over Tailscale.

## Check

`check` does everything `build` does except the last step: it renders every page, resolves every
link and anchor, runs every plugin, and writes nothing. Run it in CI:

```yaml title=".github/workflows/docs.yml"
- run: dotnet run --project docs -- check
```

It fails on errors, like `build` does. How much each finding matters is set where you configure it:
`StrictLinks` for a dead link, `WarnOnUndocumented` for an undocumented parameter, `Severity` for a
lint finding. `--strict` overrules all of them at once, on either command:

```bash frame="terminal"
dotnet run -- check --strict     # nothing questionable gets through
dotnet run -- build --strict     # the same, and the site is written
```

## What the build reports

A diagnostic says who raised it, what rule was broken, where, and what to do:

```text frame="terminal"
✗ content/guide/writing.md(2,1): error nacara/front-matter-invalid: Missing required field 'title' (at 'title')
    hint: A page has to match the collection's front matter type
```

The prefix names whoever raised it: `nacara` for the engine, the plugin's own name otherwise.

### Clicking through to the file

On a terminal the path is a link, and `path(line,column)` is the shape editors and CI parse, so
problem matchers keep working.

`file://` opens the file but not the line. Point it at your editor to land on the line as well:

```bash frame="terminal"
export NACARA_EDITOR_URL='vscode://file/{path}:{line}:{column}'
```

`{path}`, `{line}` and `{column}` are filled in. JetBrains editors take
`idea://open?file={path}&line={line}`.

Nothing is written when the output is redirected, so a log file or a CI transcript stays clean.
`NO_HYPERLINKS=1` turns them off on a terminal that shows the escapes rather than acting on them,
and `NO_COLOR` turns off colour the same way.
