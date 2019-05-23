const path = require("path");
const runScript = require("fable-splitter/dist/run").default;

let nodemonStarted = false;

module.exports = {
    entry: path.join(__dirname, "./Docs.fsproj"),
    outDir: path.join(__dirname, "./../dist"),
    babel: {
        plugins: ["@babel/plugin-transform-modules-commonjs"],
    },
    onCompiled() {
        if (!nodemonStarted) {
            nodemonStarted = true;
            runScript("./node_modules/.bin/nodemon", ["cli.js"])
        }
    }
};
