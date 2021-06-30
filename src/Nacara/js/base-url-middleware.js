export default function (baseUrl) {

    return function (req, res, next) {
        var segments = req.url.split("/").splice(1);
        const sanitizeBaseUrl = baseUrl.replace(/\//g, "")
        if (segments.length > 1 && segments[0] === sanitizeBaseUrl) {
            var newUrl = segments.splice(1).join("/");
            res.writeHead(307, {
                Location: "http://" + req.headers.host + "/" + newUrl
            });
            res.end();
        } else {
            next();
        }
    }
}
