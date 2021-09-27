const refreshCSS = () => {
    var sheets = [].slice.call(document.getElementsByTagName("link"));
    var head = document.getElementsByTagName("head")[0];
    for (var i = 0; i < sheets.length; ++i) {
        var elem = sheets[i];
        // Remove current sheet
        head.removeChild(elem);

        var rel = elem.rel;
        // Add a query parameter to the URL to force a refresh (cache buster)
        if (elem.href && typeof rel != "string" || rel.length == 0 || rel.toLowerCase() == "stylesheet") {
            var url = elem.href.replace(/(&|\?)_cacheBuster=\d+/, '');
            elem.href = url + (url.indexOf('?') >= 0 ? '&' : '?') + '_cacheBuster=' + (new Date().valueOf());
        }
        // Add the new sheet
        head.appendChild(elem);
    }
}

var protocol = window.location.protocol === 'http:' ? 'ws://' : 'wss://';
var address = protocol + window.location.host + window.location.pathname;

const connect = () => {
    var socket = new WebSocket(address);
    let connected = false;

    socket.onmessage = function (msg) {
        var data = JSON.parse(msg.data);

        if (data.type === 'reload') {
            // Reload is for all the page
            if (data.page == null) {
                window.location.reload();
            } else {
                const targetedPage = data.page;

                const currentPage =
                    location.pathname.replace("/", "");

                // Reload only if the targeted page is the current page
                if (currentPage.startsWith(targetedPage)) {
                    window.location.reload();
                }
            }
        }
        else if (data.type === 'refreshCSS') {
            refreshCSS();
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
            console.log("Reconnecting to Nacara server...");
            connect();
        }, 5000);
    }
}

connect();
