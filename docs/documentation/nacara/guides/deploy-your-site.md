---
title: Deploy your site
layout: nacara-standard
---

Nacara is a static site generator, meaning that your website is only static HTML, JavaScript and CSS files.

## Build your site

You can build your side for **production** by running:

```
npx nacara build
```

The static files generated are located in the `build` folder.

## Deploy your site

You can test your production website locally by running:

```
npx nacara serve
```

You can now deploy the `docs` folder almost anywhere.

For example, on Github you can choose to push the `docs` and serve your
