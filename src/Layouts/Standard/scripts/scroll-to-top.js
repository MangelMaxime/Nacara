window.addEventListener("DOMContentLoaded", () => {

    const refContainer = document.querySelector(".toc-scrollable-container");

    // Register scroll to top button
    document
        .querySelectorAll(".scroll-to-top")
        .forEach(element => {
            element.addEventListener("click", () => {
                document
                    .querySelector(".toc-scrollable-container")
                    .scrollTo(0, 0);
            });
        });

    // Listener used to update the position of TOC button at the bottom of the page
    refContainer.addEventListener("scroll", () => {

        // If we are at the end of the element
        // Source: https://developer.mozilla.org/fr/docs/Web/API/Element/scrollHeight#D%C3%A9terminer_si_un_%C3%A9l%C3%A9ment_a_compl%C3%A8tement_%C3%A9t%C3%A9_d%C3%A9fil%C3%A9
        if (refContainer.scrollTop > 30) {
            document
                .querySelector(".material-like-container.is-for-desktop")
                .classList
                .remove("hide");
        } else {
            document
                .querySelector(".material-like-container.is-for-desktop")
                .classList
                .add("hide");
        }
    });

    const bodyContainer = document.querySelector(".material-like-container.is-for-touch .material-like-container-body");

    document
        .querySelector(".material-like-button.close-open-button")
        .addEventListener("click", () => {
            bodyContainer.classList.toggle("show");
            document
                .querySelector(".material-like-button.close-open-button")
                .classList.toggle("is-open");
    });

});
