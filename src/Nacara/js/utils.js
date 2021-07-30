// <h2 id="primitives-decoders">
//     <a class="header-anchor" href="#primitives-decoders" aria-hidden="true">🔗</a> Primitives decoders</h2>

// const lightner = require('code-lightner');

// const md = require('markdown-it')({
//     html: true
// })
//     .use(require('./markdown-it-anchored'))
//     .use(require('./markdown-it-toc'));
//     .use(require('markdown-it-container'), 'warning', mdMessage("warning"))
//     .use(require('markdown-it-container'), 'info', mdMessage("info"))
//     .use(require('markdown-it-container'), 'success', mdMessage("success"))
//     .use(require('markdown-it-container'), 'danger', mdMessage("danger"));

export function unEscapeHtml(unsafe) {
    return unsafe
        .replace(/&amp;/g, "&")
        .replace(/&lt;/g, "<")
        .replace(/&gt;/g, ">")
        .replace(/&quot;/g, "\"")
        .replace(/&#039;/g, "'")
}

export function markdown(content, plugins) {
    let md = require('markdown-it')({
        html: true
    });
        // .use(require('./markdown-it-anchored'))
        // .use(require('./markdown-it-toc'));

    // Dynamically load the plugins
    // Should be possible to add a cache for the plugins
    // or for the whole md instance by generating a hash with all the plugins info
    plugins.forEach(plugin => {
        md = md.use(require(plugin.Path), ...plugin.Args)
    });

    return md.render(content);
}
