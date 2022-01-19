#!node

import yargs from 'yargs';
import { hideBin } from 'yargs/helpers';
import shell from 'shelljs';
import chalk from 'chalk';
import concurrently from 'concurrently';
import releaseNpm from './scripts/release-npm.js';
import releaseNuget from './scripts/release-nuget.js';
import { simpleSpawn } from './scripts/await-spawn.js';

const info = chalk.blueBright
const warn = chalk.yellow
const error = chalk.red
const success = chalk.green
const log = console.log

// Crash script on error
shell.config.fatal = true;

const spawnInNacaraCoreDir = async (command) => {
    await simpleSpawn(
        command,
        "./src/Nacara.Core"
    )
}

const spawnInNacaraDir = async (command) => {
    await simpleSpawn(
        command,
        "./src/Nacara"
    )
}

const spawnInNacaraLayoutDir = async (command) => {
    await simpleSpawn(
        command,
        "./src/Nacara.Layout.Standard"
    )
}

const setupDevHandler = async () => {
    await simpleSpawn("npm install")
    await simpleSpawn("dotnet tool restore")
    await spawnInNacaraDir("npm install")
    await spawnInNacaraLayoutDir("npm install")
    await spawnInNacaraDir("npm link")
    await spawnInNacaraLayoutDir("npm link")
    await simpleSpawn("npm link nacara nacara-layout-standard")
}

const unSetupDevHandler = async () => {
    await simpleSpawn("npm unlink nacara nacara-layout-standard")
    await spawnInNacaraDir("npm -g unlink")
    await spawnInNacaraLayoutDir("npm -g unlink")
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

    shell.rm("-rf", "./docs/reference")

    shell.rm("-rf", "./test-project/docs/reference")

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
            // Restart Nacara on file changes
            {
                command:
                    'npx nodemon \
                        --watch "./src/Nacara/dist" \
                        --watch "./src/Nacara.Layout.Standard/dist" \
                        --delay 150ms \
                        --exec "nacara watch"'
            },
            // Compile Nacara.Layout.Standard on file changes
            {
                command: "dotnet fable Source --outDir dist --watch --sourceMaps",
                cwd: "./src/Nacara.Layout.Standard"
            },
            // Compile Nacara on file changes
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
    log(info("Start building..."));
    await spawnInNacaraDir("dotnet fable Source --outDir dist");
    await spawnInNacaraLayoutDir("dotnet fable Source --outDir dist");
    log(success("Built!"));
}

const generateDocsHandler = async () => {
    await cleanHandler();
    await build();

    log(info("Generating docs..."));
    // We publish Nacara.Core to all the dll files available in a single folder
    // allowing for ApiGen to works
    await spawnInNacaraCoreDir("dotnet publish");
    log(info("Generate API reference..."))

    await simpleSpawn(
        'dotnet run -f net5.0 -- \
--project Nacara.Core \
-lib ../../Nacara.Core/bin/Debug/netstandard2.0/publish/ \
--output ../../../docs/ \
--base-url /Nacara/',
        "./src/Nacara.ApiGen/Source"
    )
    log(info("Generating docs files..."));
    shell.exec("npx nacara");

    log(success("Docs generated!"));
}

const releaseHandler = async () => {
    await testApiGenHandler();

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

    log(info("Run tests..."));

    if (argv.watch) {
        log(info("Build local version of Nacara to serve the generated files"));
        await build();

        concurrently(
            [
                {
                    command: 'dotnet watch publish',
                    cwd: "./src/Nacara.ApiGen/Tests/Project",
                    name: "Publish project",
                    prefixColor: "magenta"
                },
                {
                    command: `dotnet watch run -- \
--project TestProject \
-lib ../Tests/Project/bin/Debug/net5.0/publish \
--output ../../../test-project/docs \
--base-url /test-project/
                    `,
                    cwd: "./src/Nacara.ApiGen/Source",
                    name: "Generate API reference",
                    prefixColor: "cyan"
                },
                {
                    command: 'npx nacara watch',
                    cwd: "./test-project/",
                    name: "Nacara",
                    prefixColor: "green"
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
                --output test-project \
                --base-url /test-project/
            `
        )

    }

    log(success("Tests passed!"));
}

const testApiGenHandler = async (argv) => {
    await cleanHandler();

    if (argv.watch) {
        concurrently([
            // Publish the test project on changes
            {
                command: 'dotnet watch publish',
                cwd: "./src/Nacara.ApiGen/Tests/Project",
                name: "Publish project",
                prefixColor: "magenta"
            },
            {
                command: "dotnet watch",
                cwd: "./src/Nacara.ApiGen/Tests",
                name: "Test",
                prefixColor: "cyan"
            }
        ])
    } else {
        await simpleSpawn("dotnet publish", "./src/Nacara.ApiGen/Tests/Project");

        await simpleSpawn("dotnet run", "./src/Nacara.ApiGen/Tests");
    }
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
