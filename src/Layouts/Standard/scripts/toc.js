window.addEventListener("DOMContentLoaded", () => {

    const refContainer = document.querySelector(".toc-scrollable-container");

    // Listener used to update the active section in the TOC
    refContainer.addEventListener("scroll", () => {
        const headers = document.querySelectorAll("h2, h3, h4");

        // In order tof in the active section, we iterate over all the header
        // from the bottom to the top.
        // We take the first header that is "above" the top position of the window + 60px
        const inversedHeaders =
            [...headers]
                .sort( (headerA, headerB) => {
                    return headerB.getBoundingClientRect().top - headerA.getBoundingClientRect().top;
                });

        let activeHeader = null;

        for (let index = 0; index < inversedHeaders.length; index++) {
            const header = inversedHeaders[index];
            if (header.getBoundingClientRect().top < 60) {
                activeHeader = header;
                break;
            }
        }


        if (activeHeader === null)
            return;

        const anchorTag = activeHeader.querySelector("a").getAttribute("href");

        // Make sure to remove `is-active` to all the TOC anchor elements
        document
            .querySelectorAll(".toc-container a")
            .forEach(anchor => {
                anchor.classList.remove("is-active");
            });

        // Set `is-active` to the current section
        document
            .querySelector(`.toc-container a[href="${anchorTag}"]`)
            .classList.add("is-active");
    });

    document
        .querySelector(".material-like-button.toggle-toc")
        .addEventListener("click", () => {
            document
                .querySelector(".toc-container")
                .classList.toggle("force-show");

            document
                .querySelector(".column.toc-column")
                .classList.toggle("force-show");
        });

});
