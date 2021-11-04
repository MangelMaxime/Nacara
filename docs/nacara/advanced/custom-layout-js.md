---
title: With JavaScript
layout: standard
---

:::primary{title="Note"}
Creating a layout via JavaScript is the quickest way to extends Nacara because you just need a `.js` or `.jsx` file.

However, for complex layout or creating a layout package, you should prefer F# because it will have all the types definition and helpers available for you via the nuget `Nacara.Core`
:::

## Setup Babel for JSX support

:::info
If you used the template to set up Nacara, you can skip this step and go to [Blog page layout](#Blog-page-layout)
:::

<ul class="textual-steps">

<li>

Create the file `babel.config.json`

```json
{
    "presets": [
        "@babel/preset-react"
    ]
}
```

</li>

<li>

Install the Babel dependencies:

```
npm install --D @babel/preset-react
```

</li>

</ul>

## Blog page layout

:::info
We are going to use JavaScript to write our layout but you can also write them using F#.

For the tutorial, it is easier to use JavaScript as you can use scripts file directly.
:::

<ul class="textual-steps">

<li>

Create a file `layouts/blog-page.jsx` at the root of your repository

```
├── docs
├── docsrc
├── layouts
    └── blog-page.jsx
├── lightner
└── node_modules
```

Add this to the file:

```js
// We need to import React for JSX to works
import React from "react";
// We can reuse existing layouts function
// Here, we want to re-use the minimal page which generates only the navbar for us
import * as PageMinimal from "nacara-layout-standard/dist/Page.Minimal.js";

// Main render function which glue the helpers together
const render = async (rendererContext, pageContext) => {

    // Render the blog content using our helpers
    const content = "Blog page";

    return PageMinimal.render(
        rendererContext, // Forward the rendererContext
        pageContext.Section, // Forward the pageContext
        content // Pass the blog content to render below the navbar
    );

}

// Each layout needs to export this object
export default {
    // We can have several renderer, it is useful when publishing to npm
    Renderers: [
        {
            // Unique name of the layout used by Nacara to know how to render your page
            Name: "blog-page",
            // render function
            Func: render
        }
    ],
    // An array of external dependencies
    // For example, if your layouts need to include some external
    // JavaScript file you specify them here so Nacara know that it
    // has to copy them into the destination directory
    Dependencies: []
}

```


:::info
You can learn more about the API by going to the API section.

The API is documented from F# but the properties available are the same in JavaScript.

Here are the most important one when working with custom layouts:

- [RendererContext](/Nacara/reference/Nacara.Core/nacara-core-types-renderercontext.html) : Context accessible when rendering a page.
- [PageContext](/Nacara/reference/Nacara.Core/nacara-core-types-pagecontext.html) : Represents the context of a page within Nacara.
- [LayoutInfo](/Nacara/reference/Nacara.Core/nacara-core-types-layoutinfo.html) : Exposed contract of a layout
:::


</li>

<li>

We need to decide what information are specific to a blog post and add them to the front matter.

A blog page has:
- an author
- a title
- a date

We will add these information via the front-matter.

Update the front-matter of `docs/blog/2021/welcome.md` with

```
---
title: Welcome
layout: blog-page
author: Kira Nacara
date: 2021-08-20
---
```

</li>

<li>

In the previous, step we set the layout property to `blog-page`.

In order order to avoid error like:

> Layout renderer 'blog-page' is unknown

We need to register our layout into our Nacara config.

Add `./layouts/blog-page.jsx` to the list of `layouts` in `nacara.config.js`.

You should have something like that:

```js
export default {
    // ...
    "layouts": [
        "nacara-layout-standard",
        "./layouts/blog-page.jsx"
    ]
}
```

</li>

<li>

We now need to update our `blog-page.jsx` script to use the new information.

Replace your `render` function with this code:

:::info
We are adding a lot of code at once, take the time to read the comments
:::

```js
// This render a stylized title for our blog page
const BlogTitle = ({ title }) => {
    return <h2 className="title is-size-3 has-text-primary has-text-weight-normal has-text-centered blog-title">
        {title}
    </h2>
}

// A helper function to render the author and date of our blog post
const AuthorAndDate = ({ authorName, date }) => {
    const dateFormat =
        new Intl.DateTimeFormat(
            "en",
            {
                dateStyle: "long"
            }
        );

    return <div className="tags has-addons is-justify-content-center">
        <span className="tag is-rounded is-medium is-primary">
            {authorName}
        </span>
        <span className="tag is-rounded is-medium">
            {dateFormat.format(date)}
        </span>
    </div>
}

// A helper function to render the blog container,
// it will makes our blog centered on the page on desktop
const BlogContainer = ({ children }) => {
    return <section className="section container">
        <div className="columns">
            <div className="column is-8-desktop is-offset-2-desktop">
                {children}
            </div>
        </div>
    </section>
}

// Main render function which glue the helpers together
const render = async (rendererContext, pageContext) => {
    // Access the front matter information
    const title = pageContext.Attributes.title;
    const author = pageContext.Attributes.author;
    const date = pageContext.Attributes.date;

    // Transform the page content from Markdown to HTML
    const pageContent =
        await rendererContext.MarkdownToHtml(
            pageContext.Content,
            pageContext.RelativePath
        )

    // Render the blog content using our helpers
    const content =
        <BlogContainer>
            <BlogTitle title={title} />
            <AuthorAndDate authorName={author} date={date} />
            <div className="content" dangerouslySetInnerHTML={{ __html: pageContent }} />
        </BlogContainer>

    return PageMinimal.render(
        rendererContext, // Forward the rendererContext
        pageContext.Section, // Forward the pageContext
        content // Pass the blog content to render below the navbar
    );

}
```


</li>

</ul>

## Blog index

We are going to auto-generate the blog index page, so we can focus on writing our blog post instead of duplicating information.

<ul class="textual-steps">

<li>

Create a file `layouts/blog-index.jsx` at the root of your repository

```
├── docs
├── docsrc
├── layouts
    └── blog-page.jsx
    └── blog-index.jsx
├── lightner
└── node_modules
```

Add this to the file:

```js
// Nacara is using React for generating the HTML
const React = require("react");
// We can reuse existing layouts function
// Here, we want to re-use the minimal page which generates only the navbar for us
const pageMinimal = require("nacara-layout-standard/dist/Page.Minimal");

const render = async (rendererContext, pageContext) => {

    return pageMinimal.render(new pageMinimal.RenderArgs(
        rendererContext.Config,
        pageContext.Section,
        undefined,
        "Blog index"
    ));

}

// Each layout needs to export this object
export default {
    // We can have several renderer, it is useful when publishing to npm
    Renderers: [
        {
            // Unique name of the layout used by Nacara to know how to render your page
            Name: "blog-index",
            // render function
            Func: render
        }
    ],
    // An array of external dependencies
    // For example, if your layouts need to include some external
    // JavaScript file you specify them here so Nacara know that it
    // has to copy them into the destination directory
    Dependencies: []
}
```

</li>

<li>

Change the `docs/blog/index.md` content to

```
---
layout: blog-index
---
```

As you can see, we are only specifying the layout to use because this page page will be fully generated.

</li>

<li>

Now, we want to modify our render function to generate the index page.

To do that, we are going to:

1. To retrieve the list of `blog-page`
1. Sort the page per date
1. Extract the information we want from the page information, for example the title, date
1. Render the list of blog pages

Replace the `render` function with this code:

```js
// This render a stylized title for our blog page
const PageTitle = ({title}) => {
    return <h2 className="title is-size-3 has-text-primary has-text-weight-normal has-text-centered blog-title">
        {title}
    </h2>
}

const BlogPost = ({blogPage, siteMetadata}) => {
    const dateFormat =
        new Intl.DateTimeFormat(
            "en",
            {
                dateStyle: "short"
            }
        );

    const dateStr = dateFormat.format(blogPage.Attributes.date);

    // Add baseUrl + change file extension
    let internalLink =
        siteMetadata.BaseUrl + blogPage.RelativePath.substring(0, blogPage.RelativePath.lastIndexOf('.') + 1) + "html"

    // Use / for the URL and not \ needed when generating the blog from Windows
    internalLink = internalLink.replace(/\\/g, "/");

    return <li>
        {dateStr + ": "}
        <a className="is-underlined" href={internalLink}>
            {blogPage.Attributes.title}
        </a>
        , by {blogPage.Attributes.author}
    </li>
}

// A helper function to render the blog container,
// it will makes our blog centered on the page on desktop
const PageContainer = ({blogPages, siteMetadata}) => {
    return <section className="section container">
        <div className="columns">
            <div className="column is-8-desktop is-offset-2-desktop">
                <div className="content">
                    <PageTitle title={"Blog posts"}/>
                    <ul>
                        {blogPages.map((blogPage, index) => {
                            return <BlogPost key={index} blogPage={blogPage} siteMetadata={siteMetadata}/>
                        })}
                    </ul>
                </div>
            </div>
        </div>
    </section>
}

const render = async (rendererContext, pageContext) => {

    const blogPages =
        // In the rendererContext we have access to all Pages of our website
        rendererContext.Pages
            // Filter out the blog-page files
            .filter((pageContext) => {
                return pageContext.Layout === "blog-page"
            })
            // Sort the blog-page files by date (newest first)
            .sort((pageContext1, pageContext2 ) => {
                return pageContext2.Attributes.date - pageContext1.Attributes.date
            });

    return PageMinimal.render(
        rendererContext, // Forward the rendererContext
        pageContext.Section, // Forward the pageContext
        <PageContainer blogPages={blogPages} siteMetadata={rendererContext.Config.SiteMetadata}/>
    );

}
```

</li>

</ul>

## Working with async functions

Because, the markdown parser use an `async` function it can happens that you end up with this scenario.

```js
const Abstract = async ({rendererContext, blogPage}) => {
    const abstractText =
        await rendererContext.MarkdownToHtml(
            blogPage.Attributes.abstract,
            pageContext.RelativePath
        );

    return <div class="abstract">
        {abstractText}
    </div>
}
```

Unfortunately, this is not a valid JSX code.

The cleanest way to deal with this case is to pre-process the markdown code directly into the main `render` function.

```js
const render = async (rendererContext, pageContext) => {
    let blogPages =
        rendererContext.Pages
            //...

    // Pre-process the blog-page files here, so we can use standard JSX afterwards
    for (const blogPage of blogPages) {
        const abstractText =
            await rendererContext.MarkdownToHtml(
                blogPage.Attributes.abstract,
                blogPage.RelativePath
            );

        // Store the text representation into the blogPage information
        blogPage.Attributes.abstractText = abstractText;
    }

    return pageMinimal.render(
        rendererContext,
        pageContext,
        <PageContainer blogPages={blogPages} />
    ));
}

const Abstract = ({rendererContext, blogPage}) => {
    return <div class="abstract">
        {/* Use the pre-processed text */}
        {blogPage.Attributes.abstractText}
    </div>
}
```

## Minimal layout file

If you want to write your own layout, here is a minimal file to start from.

```js
// We need to import React for JSX to works
import React from "react";

// Your render function, this is where you will write all of your code
const render = async (rendererContext, pageContext) => {
    return <div>Hello, this page use my own layout</div>
}

export default {
    // List of the renderers
    // For now, we will have only one renderer
    Renderers: [
        {
            // Name of your layout, this is what you write in the front-matter
            // Example:
            // ---
            // layout: my-layout
            // ---
            Name: "my-layout",
            Func: render
        }
    ],
    // List of the static file to copy into the build directory
    // For example, if your layout need a `menu.js`
    Dependencies: []
}
```
