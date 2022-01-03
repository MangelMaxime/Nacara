#!node

import yargs from 'yargs';
import { hideBin } from 'yargs/helpers';
import shell from 'shelljs';
import chalk from 'chalk';
import concurrently from 'concurrently';
import releaseNpm from './scripts/release-npm.js';
import releaseNuget from './scripts/release-nuget.js';

const info = chalk.blueBright
const warn = chalk.yellow
const error = chalk.red
const success = chalk.green
const log = console.log

// Crash script on error
shell.config.fatal = true;

const shellExecNacaraCoreDir = (command) => {
    shell.exec(
        command,
        {
            cwd: "./src/Nacara.Core"
        }
    )
}

const shellExecNacaraDir = (command) => {
    shell.exec(
        command,
        {
            cwd: "./src/Nacara"
        }
    )
}

const shellExecNacaraLayoutDir = (command) => {
    shell.exec(
        command,
        {
            cwd: "./src/Nacara.Layout.Standard"
        }
    )
}

const shellExecNacaraApiGenTestsDir = (command) => {
    shell.exec(
        command,
        {
            cwd: "./src/Nacara.ApiGen/Tests"
        }
    )
}

const setupDevHandler = async () => {
    shell.exec("npm install")
    shell.exec("dotnet tool restore")
    shellExecNacaraDir("npm install")
    shellExecNacaraLayoutDir("npm install")
    shellExecNacaraDir("npm link")
    shellExecNacaraLayoutDir("npm link")
    shell.exec("npm link nacara nacara-layout-standard")
}

const unSetupDevHandler = async () => {
    shell.exec("npm unlink nacara nacara-layout-standard")
    shellExecNacaraDir("npm -g unlink")
    shellExecNacaraLayoutDir("npm -g unlink")
}

const cleanHandler = async () => {
    log(info("Cleaning..."));
    log(info("Cleaning Nacara..."));
    shell.rm("-rf", "./src/Nacara/dist")
    shell.rm("-rf", "./src/Nacara/Source/bin")
    shell.rm("-rf", "./src/Nacara/Source/obj")

    log(info("Cleaning Nacara.Layout.Standard..."));
    shell.rm("-rf", "./src/Nacara.Layout.Standard/dist")
    shell.rm("-rf", "./src/Nacara.Layout.Standard/Source/bin")
    shell.rm("-rf", "./src/Nacara.Layout.Standard/Source/obj")

    log(info("Cleaning Nacara.Core..."));
    shell.rm("-rf", "./src/Nacara.Core/dist")
    shell.rm("-rf", "./src/Nacara.Core/Source/bin")
    shell.rm("-rf", "./src/Nacara.Core/Source/obj")

    log(info("Cleaning Nacara.ApiGen..."));
    shell.rm("-rf", "./src/Nacara.ApiGen/**/bin")
    shell.rm("-rf", "./src/Nacara.ApiGen/**/obj")

    shell.rm("-rf", "./temp")

    log(success("Cleaned!"));
}

const watchNacaraHandler = async () => {
    await cleanHandler();

    log(info("Start local development..."));

    log(info("Setup directories for the watch"));
    shell.mkdir("-p", "./src/Nacara/dist");
    shell.mkdir("-p", "./src/Nacara.Layout.Standard/dist");

    log(info("Start watching..."))

    concurrently(
        [
            {
                command:
                    'npx nodemon \
                        --watch "./src/Nacara/dist" \
                        --watch "./src/Nacara.Layout.Standard/dist" \
                        --delay 150ms \
                        --exec "nacara watch"'
            },
            {
                command: "dotnet fable Source --outDir dist --watch --sourceMaps",
                cwd: "./src/Nacara.Layout.Standard"
            },
            {
                command: "dotnet fable Source --outDir dist --watch --sourceMaps",
                cwd: "./src/Nacara"
            }
        ],
        {
            prefix: "none"
        }
    )
}

const build = async () => {
    await cleanHandler();

    log(info("Start building..."));
    shellExecNacaraDir("dotnet fable Source --outDir dist");
    shellExecNacaraLayoutDir("dotnet fable Source --outDir dist");
    log(success("Built!"));
}

const generateDocsHandler = async () => {
    await build();

    log(info("Generating docs..."));
    // We publish Nacara.Core to all the dll files available in a single folder
    // allowing for ApiGen to works
    shellExecNacaraCoreDir("dotnet publish");
    log(info("Generate API reference..."))
    shell.exec('dotnet run -f net5.0 -- \
--project Nacara.Core \
-lib ../../Nacara.Core/bin/Debug/netstandard2.0/publish/ \
--output ../../../docs/ \
--base-url /Nacara/',
        {
            cwd: "./src/Nacara.ApiGen/Source"
        }
    )
    log(info("Generating docs files..."));
    shell.exec("npx nacara");

    log(success("Docs generated!"));
}

const releaseHandler = async () => {
    // Remove .fable/.gitignore files otherwise NPM doesn't publish that directory
    shell.rm("-rf", "./src/Nacara/dist/.fable/.gitignore")
    shell.rm("-rf", "./src/Nacara/dist/fable_modules/.gitignore")
    shell.rm("-rf", "./src/Nacara.Layout.Standard/dist/.fable/.gitignore")
    shell.rm("-rf", "./src/Nacara.Layout.Standard/dist/fable_modules/.gitignore")
    // Publish the packages
    releaseNpm("./src/Nacara")
    releaseNpm("./src/Nacara.Layout.Standard")
    releaseNpm("./src/Nacara.Create")
    releaseNuget("./src/Nacara.Core", "Nacara.Core.fsproj")
    releaseNuget("./src/Nacara.ApiGen", "Source/Nacara.ApiGen.fsproj")
}

const publishDocsHandler = async () => {
    await releaseHandler();

    log(info("Publishing docs..."));
    shell.exec("npx gh-pages --dist docs_deploy")

    log(success("Docs published!"));
}

const runTestApiGenAgainstTestProjectHandler = async (argv) => {
    await cleanHandler();

    if (argv.watch) {


    concurrently(
        [
            {
                command: 'dotnet watch publish',
                cwd: "./src/Nacara.ApiGen/Tests/Project",
                name: "Publish project",
                prefixColor: "magenta"
            },
            {
                command: `
                    dotnet watch run --project src/Nacara.ApiGen/Source/Nacara.ApiGen.fsproj -- \
                        --project TestProject \
                        -lib ../Tests/Project/bin/Debug/net5.0/publish \
                        --output ../../../temp \
                        --base-url /test-project/
                    `,
                name: "Generate API reference",
                prefixColor: "cyan"
            }
        ]
    )

    } else {

        shell.exec(
            "dotnet publish",
            {
                cwd: "./src/Nacara.ApiGen/Tests/Project"
            }
        )

        shell.exec(`
            dotnet run --project src/Nacara.ApiGen/Source/Nacara.ApiGen.fsproj -- \
                --project TestProject \
                -lib src/Nacara.ApiGen/Tests/Project/bin/Debug/net5.0/publish \
                --output temp \
                --base-url /test-project/
            `
        )

    }
}

const testApiGenHandler = async () => {
    throw "Not implemented yet";
}

yargs(hideBin(process.argv))
    .completion()
    .strict()
    .help()
    .alias("help", "h")
    .command(
        "setup-dev",
        "Setup npm link for local development",
        () => { },
        setupDevHandler
    )
    .command(
        "unsetup-dev",
        "Unsetup npm link for local development",
        () => { },
        unSetupDevHandler
    )
    .command(
        "clean",
        "Clean all build artifacts",
        () => { },
        cleanHandler
    )
    .command(
        "watch-nacara",
        "Start Nacara generator in watch mode",
        () => { },
        watchNacaraHandler
    )
    .command(
        "generate-docs",
        "Generate Nacara documentation",
        () => { },
        generateDocsHandler
    )
    .command(
        "publish-docs",
        "Release packages and publish Nacara documentation",
        () => { },
        publishDocsHandler
    )
    .command(
        "run-api-gen-against-test-project",
        "Launch API gen against the test project. Useful, when you want to be able to read Markdown files directly instead of the Test AST",
        (argv) => {
            argv
                .options(
                    "watch",
                    {
                        alias: "w",
                        describe: "Re-start on file changes.",
                        type: "boolean",
                        default: false
                    }
                )
        },
        runTestApiGenAgainstTestProjectHandler
    )
    .command(
        "test-api-gen",
        "Launch api-gen tests",
        (argv) => {
            argv
                .options(
                    "watch",
                    {
                        alias: "w",
                        describe: "Re-start on file changes",
                        type: "boolean",
                        default: false
                    }
                )
        },
        testApiGenHandler
    )
    .command(
        "release",
        "Release packages",
        () => { },
        releaseHandler
    )
    .version(false)
    .argv
