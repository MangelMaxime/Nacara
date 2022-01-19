---
layout: api
---

<style>
.api-code {
    font-family: monospace;
    margin-bottom: 1rem;
}

/* Animate anchor when targetted
This make it easier to spot the anchor
when jumping to it */
@keyframes blink {
    0% {
        background-color: var(--nacara-api-blink-background-color, yellow);
        color: var(--nacara-api-blink-active-color, black);
    }
    100% {
        background-color: transparent;
        color: var(--nacara-api-blink-color, black);
    }
}
.api-code .property[id]:target,
.api-code a[id]:target {
    animation-name: blink;
    animation-direction: normal;
    animation-duration: 0.75s;
    animation-iteration-count: 2;
    animation-timing-function: ease;
    /* Make the background a bit bigger than the actual text */
    margin: -0.25rem;
    padding: 0.25rem;
}

/* Anchor position */
.api-code .property[id],
.api-code a[id] {
    scroll-margin-top: var(--nacara-api-scroll-margin-top);
}

/* .api-code pre {
    background-color: transparent;
}

.api-code .line {
    white-space: nowrap;
} */

/* Synthax highlighting */
.api-code .keyword {
    color: var(--nacara-api-keyword-color, #a626a4);
}

.api-code .property,
.api-code .property:hover {
    color: var(--nacara-api-property-color, #6669d7);
}

.api-code .type,
.api-code .type:hover {
    color: var(--nacara-api-type-color, #c18401);
}

/* Hover instruction */
.api-code .property:hover,
.api-code .type:hover {
    text-decoration: underline;
    cursor: pointer;
}

/*
    Documentation formatting
*/

.api-doc-summary {
    margin-top: 1rem;
    margin-bottom: 1rem;
}

dl.api-doc-record-fields {
    margin-left: 1rem;
}

dl.api-doc-record-fields dt:not(:first-child) {
    padding-top: 1rem;
    border-top: var(--nacara-api-separator-width, 2px) solid var(--nacara-api-separator-color, black);
}

dl.api-doc-record-fields dd {
    margin-top: 1rem;
    margin-bottom: 1rem;
}
</style>

<h2 class="title is-3">ModuleWithRecord</h2>

<p><div><strong>Namespace:</strong> <a href="/test-project/reference/TestProject/global.html">global</a></div></p>



<hr>

<p class="is-size-5"><strong>Declared types</strong></p>




<table class="table is-bordered docs-modules"><thead><tr><th width="25%">Type</th><th width="75%">Description</th></tr></thead><tbody><tr><td><a href="/test-project/reference/TestProject/modulewithrecord-user.html">User</a></td><td>

Simple User type

</td></tr></tbody></table>
