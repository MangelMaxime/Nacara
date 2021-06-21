const setupMenuNavigation = () => {
    /*
     * Initialize the menu state
     */

    // Collapse menu-group which doesn't concerns the current page
    document
        .querySelectorAll(`.menu .menu-group`)
        .forEach(function (menuGroup) {
            var parentChildren = Array.from(menuGroup.parentElement.children);

            var subItems =
                parentChildren.find(function (child) {
                    return child.nodeName === "UL";
                });

            // A menu-group is expanded when one of it's element is tagged as `is-active`
            // This takes care of nested menus
            const isActiveMenu =
                menuGroup.parentElement.querySelector(".is-active") !== null

            if (isActiveMenu) {
                menuGroup.classList.add("is-expanded");
                subItems.style.display = "block";
            } else {
                menuGroup.classList.remove("is-expanded");
                subItems.style.display = "none";
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

}

const setupNavbarBurger = () => {

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

}

const setupMobileMenu = () => {

    const mobileMenuTrigger = document.querySelector(".mobile-menu .menu-trigger");

    if (mobileMenuTrigger !== null) {
        mobileMenuTrigger
            .addEventListener("click", () => {
                document
                    .querySelector(".is-menu-column")
                    .classList
                    .toggle("force-show");

                mobileMenuTrigger.classList.toggle("is-active");
        });
    }

}

window.addEventListener("DOMContentLoaded", () => {

    setupNavbarBurger();
    setupMobileMenu();

    if (document.querySelector(".menu") !== null) {
        setupMenuNavigation();
    }

});
