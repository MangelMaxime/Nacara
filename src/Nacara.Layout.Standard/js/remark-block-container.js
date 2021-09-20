import { visit } from "unist-util-visit";
import { h } from 'hastscript';
import {toHast} from 'mdast-util-to-hast';

const renderMessageHeader = (title) => {
    return h(
        "div",
        {
            className: "message-header"
        },
        h(
            "p",
            null,
            title
        )
    )
}

export default function () {
    return (tree) => {
        visit(tree, (node) => {
            if (
                node.type === 'containerDirective'
            ) {
                if (
                    node.name === "primary"
                    || node.name === "info"
                    || node.name === "success"
                    || node.name === "warning"
                    || node.name === "danger"
                ) {
                    const data = node.data || (node.data = {})

                    const hast =
                        h(
                            "article",
                            {
                                className: `message is-${node.name}`
                            },
                            (node.attributes.title) ? renderMessageHeader(node.attributes.title) : null,
                            h(
                                "div",
                                {
                                    className: "message-body"
                                },
                                toHast(node)
                            )
                        )

                    data.hName = hast.tagName
                    data.hProperties = hast.properties
                    data.hChildren = hast.children
                }

            }
        })
    }
}
