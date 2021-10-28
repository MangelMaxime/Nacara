---
title: Introduction
layout: standard
---

:::info{title="Information"}

Nacara.ApiGen is in early beta.

It can already be used to produce API documentation, but changes in the generated files are expected in future version and it is possible that not every F# types are supported yet.

:::

## Installation

Nacara.ApiGen is a tool for generating API documentation from your code.

`nacara-layout-standard` is the package providing the standard layout when working with Nacara.

```bash
dotnet new tool-manifest # if you are setting up this repo
dotnet tool install Nacara.ApiGen
```

## Usage

`nacara-apigen` generateds markdonw files that can be consume by `nacara` to generate the `.html` files.

**Example**

```bash
# First prepare the DLLs and XML files
dotnet publish

# Run nacara-apigen
dotnet nacara-apigen \
    --project MyProject \
    -lib srcbin/Debug/netstandard2.0/publish \
    --output docs \
    --base-url /my-site/
```

:::info
Remember to have set `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in your fsproj file to have the XML file generated.
:::

### CLI Arguments

Righ now `nacara-apigen` doesn't know yet how to read your `nacara.config.json` so you need to pass everything via CLI arguments:

- `--project` or `-p`: The name of your project
- `--lib-directory` or `-lib`: The source dierectory where your dlls and xml files are located
- `--base-url`: Base URL for your site. The same as you configured in your `nacara.config.json` file
- `--output` or `-o`: The output directory

In order to works, `nacara-apigen` needs to have access to all your project dlls and xml files., the easiest way to get them is to run `dotnet publish` before invoking `dotnet nacara-apigen` using the generated directory as source.

### SCSS variables

`$nacara-api-keyword-color`

Default: `#a626a4`

Control the color of the keywords.

---

`$nacara-api-type-color`

Default: `#c18401`

Control the color of the types.

---
