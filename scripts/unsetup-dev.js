const shell = require("shelljs")

// We use a JavaScript script to execute thuse commands
// so it is cross fatal
// Otherwise, we could need bash/cmd file depending on the OS

shell.config.fatal = true // Throw on error

shell.exec("npm unlink nacara nacara-layout-standard")

shell.exec("npm unlink -g", {
    cwd: "src/Nacara"
})

shell.exec("npm unlink -g", {
    cwd: "src/Layouts/Standard"
})
