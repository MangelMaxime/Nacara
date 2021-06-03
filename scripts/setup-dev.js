const shell = require("shelljs")

// We use a JavaScript script to execute thuse commands
// so it is cross fatal
// Otherwise, we could need bash/cmd file depending on the OS

shell.config.fatal = true // Throw on error

shell.exec("npm link", {
    cwd: "src/Nacara"
})

shell.exec("npm link", {
    cwd: "src/Layouts/Standard"
})

shell.exec("npm link nacara nacara-layout-standard")
