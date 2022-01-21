---
title: Directory structure
layout: standard
---

A basic Nacara project have a structure similar to this:

```
├── docs
│   ├── _partials
│   │   └── [...]
│   ├── documentation
│   │   └── [...]
│   ├── scss
│   │   └── [...]
│   └── style.scss
├── docs_deploy
│   └── [...]
├── lightner
│   ├── grammars
│   └── themes
├── nacara.config.json
├── package.json
```

To understand it better, we present this overview describing each configuration:

<table class="table is-narrow is-bordered is-vcentered">
    <thead>
        <tr>
            <th class="label-cell">File / Directory</th>
            <th class="label-cell">Description</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td class="label-cell">
                <code>.nacara</code>
            </td>
            <td class="fullwidth-cell">
                <p>
                    Internal folder where Nacara put generated or cached files
                </p>
                <p>
                    You should add this folder to your <code>.gitignore</code>
                </p>
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>docs</code>
            </td>
            <td class="fullwidth-cell">
                Default folder where you place your website source files like:
                <ul>
                    <li>Static resources</li>
                    <li>Markdown files to convert</li>
                    <li>Style files</li>
                </ul>
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>docs/style.scss</code>
                <div class="is-size-7 my-2">or</div>
                <code>docs/style.sass</code>
            </td>
            <td class="fullwidth-cell">
                <p>
                    Main entry file for styling your application, generated file will be <code>docs_deploy/style.css</code>
                </p>
                <p>
                    Only one of those files should be used at a time.
                </p>
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>docs/scss</code>
                <div class="is-size-7 my-2">or</div>
                <code>docs/sass</code>
            </td>
            <td class="fullwidth-cell">
                This is where you place your SASS/SCSS files imported by <code>style.scss</code>.
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>docs/_partials</code>
            </td>
            <td class="fullwidth-cell">
                This is where you place your partials files like <code>footer.jsx</code>
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>docs_deploy</code>
            </td>
            <td class="fullwidth-cell">
                <p>This is where Nacara will put the generated files (by default).</p>
                <p>You should add this folder to your <code>.gitignore</code>.</p>
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>lightner/themes</code>
            </td>
            <td class="fullwidth-cell">
                <p>

Recommended place to place your theme file user by [Code-lightner](https://github.com/MangelMaxime/Code-Lightner) to provides code highlights.
                </p>
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>lightner/grammars</code>
            </td>
            <td class="fullwidth-cell">
                <p>

Recommended place to place your grammars files user by [Code-lightner](https://github.com/MangelMaxime/Code-Lightner) to provides code highlights.
                </p>
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>nacara.config.json</code>
            </td>
            <td class="fullwidth-cell">
                <p>Nacara config file.</p>
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>package.json</code>
            </td>
            <td class="fullwidth-cell">
                <p>File used to configure your NPM dependencies like Nacara, Babel, nacara-layout-standard.</p>
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>docs/index.md</code>
                <div class="is-size-7 my-2">or</div>
                <code>docs/\*\*/\*.md</code>
            </td>
            <td class="fullwidth-cell">
                <p>Any markdown file encounter are going to be transform by Nacara into HTML using the layout provided via the front-matter property.</p>
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>docs/\*\*/\*.fs</code>
                <div class="is-size-7 my-2">or</div>
                <code>docs/\*\*/\*.fsx</code>
            </td>
            <td class="fullwidth-cell">
                <p>F# files are tested to see if there are [literate](/nacara/guides/literate-files.html) files.</p>
                <p>If they are, they are transformed like as markdown files.</p>
                <p>If not, there are copied as is to the output folder.</p>
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>docs/\*\*/menu.json</code>
            </td>
            <td class="fullwidth-cell">
                Menu configuration for the section it is in.
            </td>
        </tr>
        <tr>
            <td class="label-cell">
                <code>Other files</code>
                <div class="is-size-7 my-2">or</div>
                <code>folders</code>
            </td>
            <td class="fullwidth-cell">
                Except for the special cases listed above, all the other file and folder will be copied into the destination folder.
            </td>
        </tr>
    </tbody>
</table>
