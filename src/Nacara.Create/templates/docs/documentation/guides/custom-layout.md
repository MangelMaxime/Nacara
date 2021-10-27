---
title: Custom layout
layout: standard
---

Nacara is extensible, you can write your own layout or re-use existing layout to adapt them to your needs.

In the previous chapter, we created our blog section but it is pretty bad looking and each time you write a blog post you have to manually add it to the index page.

We are going to auto-generate the index page and also make a blog specific layout.

## Blog page layout

:::info
We are going to use JavaScript to write our layout but you can also write them using F#.

For the tutorial, it is easier to use JavaScript as no setup is required
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
    const pageContent = await rendererContext.MarkdownToHtml(pageContext.Content)

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

1. Retrieve the list of `blog-page`
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
