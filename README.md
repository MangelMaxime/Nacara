# Nacara

Nacara is a simple static documentation generator extensible using F# via [Fable](https://fable.io) or JavaScript.

## Development

### Requirements

Make sure to have these programs installed before proceeding.

- Node v14+
- Yarn v1.22+
- .NET SDK 5.0.202+
- Make 4.3

If you can't or don't want to install the requirements on your computer, you can use Gitpod which provide a ready to use environment directly in your browser.

**For Linux**

If on Linux, you should install the following packages on your system before proceeding:

```bash
# Debian based
sudo apt-get install make gcc g++
```

**For Windows**

You need to have make installed on your system. The easiest way to install make is by using [chocolatey](https://chocolatey.org/).

1. Install [chocolatey](https://chocolatey.org/install)
2. Install `make` via `choco install make`

It is planned to add a `make.bat` file in the future but for now it will do.

### Makefile

This project use a Makefile to handle the build automation here are the main targets

- `setup-dev`:

    Install of the dependencies and configure the npm link.

    Useful, when you want to test a local build of Nacara in another project or to develop it.

    <u>Note:</u> You should run this target each time you updates the NPM dependencies

- `unsetup-dev`:

    Revert the npm link

- `watch`:

    Watch files changes for compilations and run a local version of Nacara for development

- `release`:

    Release the different NPM and NuGet packages

- `publish`:

    Execute release and publish a new version of the docs site
