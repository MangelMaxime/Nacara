const path = require("path");

module.exports = {
    entry: path.join(__dirname, "./Nacara.Layouts.Standard.fsproj"),
    outDir: path.join(__dirname, "./../../../dist/layouts/standard"),
    babel: {
        plugins: ["@babel/plugin-transform-modules-commonjs"],
    }
};
