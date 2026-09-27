---
title: Community Plugins
---

A list of community created plugins and integrations for the Nacara platform.

## Stylesheets

- [TailwindCss](https://shayanhabibi.github.io/Partas.Nacara.Plugins/guide/tailwind/) - [TailwindCss](https://tailwindcss.com/) for Nacara; configurable
- [DaisyUI](https://shayanhabibi.github.io/Partas.Nacara.Plugins/guide/daisyui/) - [DaisyUI](https://daisyui.com/) for Nacara; configurable, routes through the [TailwindCss](https://github.com/shayanhabibi/Partas.Nacara.Plugins) plugin.
- [Theme Contract](https://shayanhabibi.github.io/Partas.Nacara.Plugins/guide/styling/) - Allows
  plugins and themes to offer styles without depending on the theme/plugin that renders them.

## Custom Components

- [Directives](https://shayanhabibi.github.io/Partas.Nacara.Plugins/guide/directives/) - Small DSL
  for you to create custom directive components (such as `:::note`) in Nacara - whether to use in
  your own projects or to share as a plugin. Type safe argument capture, just like Nacara page
  front matter!

## Metadata & Output

- [OgImage](https://shayanhabibi.github.io/Partas.Nacara.Plugins/guide/og-image/) - Link preview
  images: `og:image` and `twitter:image` tags from page front matter, with a site-wide fallback.
- [AgentFriendly](https://shayanhabibi.github.io/Partas.Nacara.Plugins/guide/agent-friendly/) - A
  site agents can read without scraping it: an [`llms.txt`](https://llmstxt.org) index, an
  `llms-full.txt` holding every page, and a markdown copy of each page beside its html.

## Themes

- [Partas.Nacara.Theme](https://shayanhabibi.github.io/Partas.Nacara.Plugins/guide/theme/) - The
  default `Nacara.Theme.Default` with more granular control over styling, allowing you to
  replace/drop/inject style sheets as layers.
