const path = require("path");
const runScript = require("fable-splitter/dist/run").default;

let nodemonStarted = false;
const isWatch = process.argv.indexOf("--watch") !== -1 || process.argv.indexOf("-w") !== -1;

module.exports = {
    entry: path.join(__dirname, "./Docs.fsproj"),
    outDir: path.join(__dirname, "./../dist"),
    babel: {
        sourceMaps: true
    },
    // cli: {
    //     path: "../Fable/src/Fable.Cli"
    // },
    onCompiled() {
        if (!nodemonStarted) {
            if (isWatch) {
                nodemonStarted = true;
                runScript("./node_modules/.bin/nodemon", ["cli.js", "--", "--watch", "--server"])
            }
        }
    }
};
