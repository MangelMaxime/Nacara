// <h2 id="primitives-decoders">
//     <a class="header-anchor" href="#primitives-decoders" aria-hidden="true">🔗</a> Primitives decoders</h2>

const lightner = require('code-lightner');

const mdMessage = (level) => {

    return {
        validate: function (params) {
            return params.trim() === level;
        },

        render: function (tokens, idx) {
            if (tokens[idx].nesting === 1) {
                // opening tag
                return `<article class="message is-${level}">
                <div class="message-body">`;


            } else {
                // closing tag
                return '</div>\n</article>\n';
            }
        }
    }
}

const md = require('markdown-it')({
    html: true
})
    .use(require('./markdown-it-anchored'))
    .use(require('./markdown-it-toc'))
    .use(require('markdown-it-container'), 'warning', mdMessage("warning"))
    .use(require('markdown-it-container'), 'info', mdMessage("info"))
    .use(require('markdown-it-container'), 'success', mdMessage("success"))
    .use(require('markdown-it-container'), 'danger', mdMessage("danger"));

const startTag = `<pre><code class="language-fsharp">`;
const endTag = `</code></pre>`;

const codeToFSharp = code => {
    return lightner.lighten({
        backgroundColor: "#FAFAFA",
        textColor: "",
        grammarFiles: [
            "./paket-files/docs/ionide/ionide-fsgrammar/grammar/fsharp.json"
        ],
        scopeName: "source.fsharp",
        themeFile: "./paket-files/docs/akamud/vscode-theme-onelight/themes/OneLight.json"
    }, code);
};

export function unEscapeHtml(unsafe) {
    return unsafe
        .replace(/&amp;/g, "&")
        .replace(/&lt;/g, "<")
        .replace(/&gt;/g, ">")
        .replace(/&quot;/g, "\"")
        .replace(/&#039;/g, "'")
}

export function markdown(content) {
    return md.render(content);
}

// Format the code after the markdown pass is done
// This is needed because markdown-it want a synchronous highlight function
const codeFormat = html => {
    let startIndex = html.indexOf(startTag);
    if (startIndex !== -1) {
        let endIndex = html.indexOf(endTag);

        let startStr = html.substring(0, startIndex);
        let codeTxt = html.substring(startIndex + startTag.length, endIndex);
        let endStr = html.substring(endIndex + endTag.length); // 13 = `</code></pre>`.length

        let code = unEscapeHtml(codeTxt);

        return codeToFSharp(code)
            .then(htmlCode => {
                return codeFormat(endStr)
                    .then(endHtmlCode => {
                        return startStr + htmlCode + endHtmlCode;
                    });
            });
    } else {
        return Promise.resolve(html);
    }
}
