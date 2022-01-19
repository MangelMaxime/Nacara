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

<h2 class="title is-3">User</h2>

<p><div><strong>Namespace:</strong> <a href="/test-project/reference/TestProject/global.html">global</a></div><div><strong>Parent:</strong> <a href="/test-project/reference/TestProject/global-modulewithrecord.html">ModuleWithRecord</a></div></p>

<div class="api-code"><div><span class="keyword">type</span>&nbsp;<span class="type">User</span>&nbsp;<span class="keyword">=</span></div><div>&nbsp;&nbsp;&nbsp;&nbsp;<span class="keyword">{</span></div><div class="record-field">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<a class="property" href="#id">Id</a>&nbsp;<span class="keyword">:</span>&nbsp;int</div><div class="record-field">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<a class="property" href="#name">Name</a>&nbsp;<span class="keyword">:</span>&nbsp;string</div><div class="record-field">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<a class="property" href="#email">Email</a>&nbsp;<span class="keyword">:</span>&nbsp;string</div><div>&nbsp;&nbsp;&nbsp;&nbsp;<span class="keyword">}</span></div><br><div>&nbsp;&nbsp;&nbsp;&nbsp;<span class="keyword">member</span>&nbsp;<a class="property" href="#getter_only">GetterOnly</a>&nbsp;<span class="keyword">:</span>&nbsp;string&nbsp;<span class="keyword">with</span>&nbsp;<span class="keyword">get</span></div><div>&nbsp;&nbsp;&nbsp;&nbsp;<span class="keyword">member</span>&nbsp;<a class="property" href="#getter_and_setter">GetterAndSetter</a>&nbsp;<span class="keyword">:</span>&nbsp;&nbsp;<span class="keyword">with</span>&nbsp;<span class="keyword">get</span><span class="keyword">,</span><span class="keyword">set</span></div><div>&nbsp;&nbsp;&nbsp;&nbsp;<span class="keyword">member</span>&nbsp;<a class="property" href="#setter_only">SetterOnly</a>&nbsp;<span class="keyword">:</span>&nbsp;&nbsp;<span class="keyword">with</span>&nbsp;<span class="keyword">set</span></div><br><div>&nbsp;&nbsp;&nbsp;&nbsp;<span class="keyword">static member</span>&nbsp;<a class="property" href="#create">Create</a>&nbsp;<span class="keyword">:</span>&nbsp;<div>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;id&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span class="keyword">:</span>&nbsp;int&nbsp;&nbsp;&nbsp;&nbsp;<span class="keyword">*</span></div><div>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;name&nbsp;&nbsp;&nbsp;<span class="keyword">:</span>&nbsp;string&nbsp;<span class="keyword">*</span></div><div>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;?email&nbsp;<span class="keyword">:</span>&nbsp;string&nbsp;</div><div>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span class="keyword">-&gt;</span>&nbsp;User</div></div></div>

<section class="api-doc-summary"><p><strong>Description</strong></p><p>

Simple User type

</p></section>

<section><p><strong>Properties</strong></p><dl class="api-doc-record-fields"><dt class="api-code"><a class="property" href="#id" id="id">Id</a>&nbsp;<span class="keyword">:</span>&nbsp;int</dt><dd>

Unique Id of the user

</dd><dt class="api-code"><a class="property" href="#name" id="name">Name</a>&nbsp;<span class="keyword">:</span>&nbsp;string</dt><dd>

Name of the user

</dd><dt class="api-code"><a class="property" href="#email" id="email">Email</a>&nbsp;<span class="keyword">:</span>&nbsp;string</dt><dd>

Email of the user

</dd></dl></section>

<section><p><strong>Instance members</strong></p><dl class="api-doc-record-fields"><dt class="api-code"><a class="member" href="#getter_only" id="getter_only">GetterOnly</a>&nbsp;<span class="keyword">:</span>&nbsp;string</dt><dd>

This is a getter only property

</dd><dt class="api-code"><a class="member" href="#getter_and_setter" id="getter_and_setter">GetterAndSetter</a>&nbsp;<span class="keyword">:</span>&nbsp;</dt><dd></dd><dt class="api-code"><a class="member" href="#setter_only" id="setter_only">SetterOnly</a>&nbsp;<span class="keyword">:</span>&nbsp;</dt><dd></dd></dl></section>

<section><p><strong>Static members</strong></p><dl class="api-doc-record-fields"><dt class="api-code"><div><span class="keyword">static member</span>&nbsp;<a class="property" href="#create" id="create">Create</a>&nbsp;<span class="keyword">:</span>&nbsp;<div>&nbsp;&nbsp;&nbsp;&nbsp;id&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span class="keyword">:</span>&nbsp;int&nbsp;&nbsp;&nbsp;&nbsp;<span class="keyword">*</span></div><div>&nbsp;&nbsp;&nbsp;&nbsp;name&nbsp;&nbsp;&nbsp;<span class="keyword">:</span>&nbsp;string&nbsp;<span class="keyword">*</span></div><div>&nbsp;&nbsp;&nbsp;&nbsp;?email&nbsp;<span class="keyword">:</span>&nbsp;string&nbsp;</div><div>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span class="keyword">-&gt;</span>&nbsp;User</div></div></dt><dd><p><strong>Parameters</strong></p>

Create a new user




 &lt;returns&gt;
Returns the newly created user
&lt;/returns&gt;
</dd></dl></section>
