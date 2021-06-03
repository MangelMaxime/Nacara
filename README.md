# Nacara

Nacara is a static documentation generator.

## Development

This chapter contains the instructions for building Nacara and generating its documentation.

### Requirements

> **For Linux**
>
> If on Linux, you should install the following packages on your system before procedding:
>
> ```bash
> # Debian based
> sudo apt-get install make gcc g++
> ```

Make sure to have these programs installed before proceeding.

- Node v14+
- Yarn v1.22+
- .NET SDK 5.0.202+

### Tools

- fake-cli
- paket

To have access to tools, you must run this command:

```bash
dotnet tool restore
```

Then, the tools can be run with either of these commands:

```bash
dotnet fake
dotnet paket

# or
dotnet tool run fake
dotnet tool run paket
```

### Build

Currently there are two ways of building the program to run, with FAKE or with Yarn/NPM.

#### Yarn (Recommended)

You will need three terminals open.

1. In the first terminal run this command:

```bash
yarn nacara-watch
```

2. In the second terminal run this command:

```bash
yarn layouts-standard-watch
```

3. When the first two commands have compiled and are waiting for changes, run this command to start the dev server:


```bash
yarn nodemon
```

#### FAKE (`build.fsx`)

If this is your first time building Nacara, you need to run this command first:

```bash
yarn build
```

Then, you can run this command which will watch for changes and start the dev server:

```bash
dotnet fake build -t watch
```

> If you don't run the first command, the second command will complain about `dist/Main.js` missing.

*ℹ Nodemon will restart several time while Fable is compiling the files, after the initial compilation it will only restart when you modify your code*


