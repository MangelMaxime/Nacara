#!/usr/bin/env node

import nodemon from 'nodemon';
import { runBuild, runClean, runServe } from './dist/Main.js';

import { fileURLToPath } from 'node:url'
import path from 'node:path';
import { info } from './dist/Nacara.Core/Log.js';
import yargs from 'yargs';
import { hideBin } from "yargs/helpers";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

yargs(hideBin(process.argv))
    .completion()
    .strict()
    .help()
    .alias("help", "h")
    .version()
    .command(
        "watch",
        "Start the development server",
        () => {},
        async () => {
            nodemon({
                script: path.join(__dirname, "./js/nodemon-watch.js"),
                args: process.argv.slice(2),
                watch: [
                    "nacara.config.json"
                ],
                delay: 200
            })
            // Inform the user that the application is restarting
            .on("restart", (x) => {
                info("Nacara configuration file changed, restarting Nacara...")
            })
            // Allow the user to kill the application with a single CTRL+C
            // Without this the user need to do it twice
            .once('quit', function () {
                process.exit();
            });
        }
    )
    .command(
        "clean",
        "Clean up generated files",
        () => {},
        runClean
    )
    .command(
        "serve",
        "Serve the website locally",
        () => {},
        runServe
    )
    .command(
        "build",
        "Build the website",
        () => {},
        runBuild
    )
    .argv
