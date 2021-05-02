const setupMenuNavigation = () => {
    /*
     * Initialize the menu state
     */

    // Set as active the menu for the current page
    document
        .querySelectorAll(`.menu [data-menu-id='${nacara.pageId}']`)
        .forEach(function (menuItem) {
            menuItem.classList.add("is-active")
        });

    // Collapse menu-group which doesn't concerns the current page
    document
        .querySelectorAll(`.menu .menu-group`)
        .forEach(function (menuGroup) {
            var parentChildren = Array.from(menuGroup.parentElement.children);
            var subItems = parentChildren.find(function (child) {
                return child.nodeName === "UL";
            });

            var activeMenu =
                Array.from(subItems.children)
                    .find(function (child) {
                        return child.firstChild.classList.contains("is-active")
                    });

            if (activeMenu === undefined) {
                menuGroup.classList.remove("is-expanded");
                subItems.style.display = "none";
            } else {
                menuGroup.classList.add("is-expanded");
                subItems.style.display = "block";
            }
        });

    // Register listener to handle menu-group
    document
        .querySelectorAll(`.menu .menu-group`)
        .forEach(function (menuGroup) {
            menuGroup.addEventListener("click", function (ev) {
                // All the menu "calculation" are done relative to the .menu-group element
                var menuGroup =
                    ev.target.classList.contains("menu-group")
                        ? ev.target
                        : ev.target.closest(".menu-group");

                var parentChildren = Array.from(menuGroup.parentElement.children);

                var subItems =
                    parentChildren.find(function (child) {
                        return child.nodeName === "UL";
                    });

                if (menuGroup.classList.contains("is-expanded")) {
                    menuGroup.classList.remove("is-expanded");
                    subItems.style.display = "none";
                } else {
                    menuGroup.classList.add("is-expanded");
                    subItems.style.display = "block";
                }

            });
        });

    /*
     * Setup menu navigation via Next and Previous button
     */

    // Start the counter from -1, but the first menu item will be 0
    let computedMaxRank = -1;

    document
        .querySelectorAll(`.menu [data-menu-id]`)
        .forEach(function (menuItem) {
            computedMaxRank++;
            menuItem.setAttribute("data-menu-rank", computedMaxRank);
        });

    document
        .querySelector(".navigate-to-previous")
        .addEventListener("click", () => {
            const currentMenuRank =
                document
                    .querySelector(`.menu [data-menu-id='${nacara.pageId}']`)
                    .getAttribute("data-menu-rank");

            const previousRank = parseInt(currentMenuRank) - 1;

            if (previousRank >= 0) {
                document
                    .querySelector(`.menu [data-menu-rank='${previousRank}']`)
                    .click();
            }
        });

    document
        .querySelector(".navigate-to-next")
        .addEventListener("click", () => {
            const currentMenuRank =
                document
                    .querySelector(`.menu [data-menu-id='${nacara.pageId}']`)
                    .getAttribute("data-menu-rank")

            const nextRank = parseInt(currentMenuRank) + 1;

            if (nextRank <= computedMaxRank) {
                document
                    .querySelector(`.menu [data-menu-rank='${nextRank}']`)
                    .click();
            }
        });

    // IIFE function in order to not pollute the function scope
    (() => {

        // The menu is an optional element, if no menu found then hide the Next & Previous buttons
        if (document.querySelector(".menu") === null) {
            document.querySelector(".navigate-to-next").style.visibility = "hidden"
            document.querySelector(".navigate-to-previous").style.visibility = "hidden"
            return;
        }

        const currentMenuRank =
            parseInt(
                document
                    .querySelector(`.menu [data-menu-id='${nacara.pageId}']`)
                    .getAttribute("data-menu-rank")
            );

        // Initialize the text of the Previous button
        if (currentMenuRank >= 1) {
            const previousText =
                document
                    .querySelector(`.menu [data-menu-rank='${currentMenuRank - 1}']`)
                    .textContent;

            document
                .querySelector(".navigate-to-previous .text")
                .textContent = previousText;
        }

        // Initialize the text of the Next button
        if (currentMenuRank < computedMaxRank) {
            const previousText =
                document
                    .querySelector(`.menu [data-menu-rank='${currentMenuRank + 1}']`)
                    .textContent;

            document
                .querySelector(".navigate-to-next .text")
                .textContent = previousText;
        }

        // Hide the navigation button depending on the page rank
        if (currentMenuRank == 0) {
            document.querySelector(".navigate-to-previous").style.visibility = "hidden";
        } else if (currentMenuRank == computedMaxRank) {
            document.querySelector(".navigate-to-next").style.visibility = "visibile";
        }
    })();
}


window.addEventListener("DOMContentLoaded", () => {

    if (document.querySelector(".menu") !== null
            && document.querySelector(".navigate-to-previous") !== null
            && document.querySelector(".navigate-to-next") !== null) {

        setupMenuNavigation();
    }

    /*
     * Setup menu burger behaviour
     */

    // Code copied from Bulma documentation
    // https://bulma.io/documentation/components/navbar/#navbar-menu

    // Get all "navbar-burger" elements
    const $navbarBurgers = Array.prototype.slice.call(document.querySelectorAll('.navbar-burger'), 0);

    // Check if there are any navbar burgers
    if ($navbarBurgers.length > 0) {

        // Add a click event on each of them
        $navbarBurgers.forEach(el => {
            el.addEventListener('click', () => {

                // Get the target from the "data-target" attribute
                const target = el.dataset.target;
                const $target = document.getElementById(target);

                // Toggle the "is-active" class on both the "navbar-burger" and the "navbar-menu"
                el.classList.toggle('is-active');
                $target.classList.toggle('is-active');

            });
        });
    }

    const mobileMenuTrigger = document.querySelector(".mobile-menu .menu-trigger");

    if (mobileMenuTrigger !== null) {
        mobileMenuTrigger
            .addEventListener("click", () => {
                document
                    .querySelector(".is-menu-column")
                    .classList
                    .toggle("force-show");
        });
    }

});
