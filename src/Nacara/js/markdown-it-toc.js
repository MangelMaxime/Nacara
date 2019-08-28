// This code has been adapted from
// https://github.com/Oktavilla/markdown-it-table-of-contents/blob/32825fe8507835fb551e1b59d1efdf5418e04a06/index.js

module.exports = (md) => {

    const tocRegexp = /^\[\[toc\]\]/im;
    let grabState;

    const toc = (state, silent) => {
        let token;

        // Reject if the token does not start with [
        if (state.src.charCodeAt(state.pos) !== 0x5B /* [ */) {
            return false;
        }

        if (silent) {
            return false;
        }

        // Detect the TOC markdown marker
        let match = tocRegexp.exec(state.src.substr(state.pos));
        match = !match ? [] : match.filter(function (m) { return m; });
        if (match.length < 1) {
            return false;
        }

        // Build content
        token = state.push('toc_open', 'toc', 1);
        token.markup = '[[toc]]';
        token = state.push('toc_body', '', 0);
        token = state.push('toc_close', 'toc', -1);

        // Update pos so the parser can continue
        var newline = state.src.indexOf('\n', state.pos);
        if (newline !== -1) {
            state.pos = newline;
        } else {
            state.pos = state.pos + state.posMax + 1;
        }

        return true;
    };

    const getHref = (tokens) => {
        return tokens
            .find(token => token.type === 'anchor_open')
            .attrs
            .find(attr => attr[0] === 'id')[1];
    };

    const renderChildsTokens = (pos, tokens) => {
        let headings = [],
            buffer = '',
            currentLevel,
            subHeadings,
            size = tokens.length,
            i = pos;

        while (i < size) {
            let token = tokens[i];
            let heading = tokens[i - 1];
            let level = token.tag && parseInt(token.tag.substr(1, 1));

            if (level === 1) {
                i++;
                continue;
            }

            // If the current type is not a `heading_close` then we continue processing the tokens
            if (token.type !== 'heading_close' || level > 4 || heading.type !== 'inline') {
                i++;
                continue; // Skip if not matching criteria
            }

            // This is a valid `heading_close` token, we can process the token to add it to the TOC
            if (!currentLevel) {
                currentLevel = level;// We init with the first found level
            } else {
                if (level > currentLevel) {
                    subHeadings = renderChildsTokens(i, tokens);
                    buffer += subHeadings[1];
                    i = subHeadings[0];
                    continue;
                }
                if (level < currentLevel) {
                    // Finishing the sub headings
                    buffer += `</li>`;
                    headings.push(buffer);
                    return [i, `<ul class="toc-headings">${headings.join('')}</ul>`];
                }
                if (level == currentLevel) {
                    // Finishing the sub headings
                    buffer += `</li>`;
                    headings.push(buffer);
                }
            }

            if (level === 2) {
                buffer = `<div class="toc-label"><a href="#${getHref(heading.children)}">`;
            } else {
                buffer = `<li><a href="#${getHref(heading.children)}">`;
            }

            buffer += heading.content;

            if (level === 2) {
                buffer += `</a></div>`;
            } else {
                buffer += `</a>`;
            }

            i++;
        }
        buffer += buffer === '' ? '' : `</li>`;
        headings.push(buffer);
        return [i, `<ul class="toc-headings">${headings.join('')}</ul>`];
    };

    md.renderer.rules.toc_open = (tokens, index) => {
        return `<nav class="toc-container">`;
    };

    md.renderer.rules.toc_close = (tokens, index) => {
        return `</nav>`;
    };

    md.renderer.rules.toc_body = (tokens, index) => {
        return renderChildsTokens(0, grabState.tokens)[1];
    }

    // Catch all the tokens for iteration later
    md.core.ruler.push('grab_state', function (state) {
        grabState = state;
    });

    md.inline.ruler.after('emphasis', 'toc', toc);
}
