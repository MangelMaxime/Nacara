export default function (baseUrl) {

    return function (req, res, next) {
        if (req.url.startsWith(baseUrl)) {
            res.writeHead(301, {
                Location: "http://" + req.headers.host + "/" + req.url.substring(baseUrl.length)
            });
            res.end();
        } else {
            next();
        }
    }
}
