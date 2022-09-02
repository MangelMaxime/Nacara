namespace Nacara.Core

module LiveReload =

    let javascriptCode =
        """
// Store the page_y in the URL
// Restore the page_y from the URL
// Replace the URL without the page_y query to make it transparent to the user

// Support refresh of CSS without reloading the page
var protocol = window.location.protocol === 'http:' ? 'ws://' : 'wss://';
var address = protocol + window.location.host + "/live-reload";

const connect = () => {
    var socket = new WebSocket(address);
    let connected = false;

    socket.onmessage = function (msg) {
        // var data = JSON.parse(msg.data);
        if (msg == "reload-page") {
            window.location.reload();
        } else if (msg == "refresh-css") {
            console.log("TODO: refresh CSS");
        } else {
            console.error("Unknown live-reload action: " + msg);
        }
    };

    socket.onopen = function () {
        connected = true;
        console.log("Connected to Nacara server...")
        console.log("Page will be updated when a file changes")
    }

    socket.onclose = function () {
        if (connected) {
            console.error("Disconnected from Nacara server...")
        }
        else {
            console.error("Could not connect to Nacara server...")
        }

        setTimeout(() => {
            console.clear();
            console.log("Reconnecting to Nacara server...");
            connect();
        }, 1000);
    }
}

connect();
            """
