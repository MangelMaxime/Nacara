import { unified } from 'unified'
import remarkParse from 'remark-parse'
import remarkRehype from 'remark-rehype'
import rehypeRaw from 'rehype-raw'
import rehypeFormat from 'rehype-format'
import rehypeStringify from 'rehype-stringify'

export default async function(remarkPlugins, rehypePlugins, markdownText) {
    const chain =
        unified()
            .use(remarkParse);

    // Apply the remark plugins
    for (const plugin of remarkPlugins) {
        const instance = await import(plugin.Resolve);

        if (plugin.Property) {
            chain.use(instance.default[plugin.Property], plugin.Options);
        } else {
            chain.use(instance.default, plugin.Options);
        }
    }

    // Convert from remark to rehype
    chain
        .use(remarkRehype, { allowDangerousHtml: true })
        .use(rehypeRaw);

    // Apply the rehype plugins
    for (const plugin of rehypePlugins) {
        const instance = await import(plugin.Resolve);

        if (plugin.Property) {
            chain.use(instance.default[plugin.Property], plugin.Options);
        } else {
            chain.use(instance.default, plugin.Options);
        }
    }

    // Generate the HTML
    return chain
        .use(rehypeFormat)
        .use(rehypeStringify)
        .process(markdownText);
}
