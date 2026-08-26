import { themePicker } from "./color-scheme.js";
import { copyButtons } from "./copy-buttons.js";
import { dropdowns } from "./dropdowns.js";
import { mobileMenu } from "./mobile-menu.js";
import { narrowWidgets } from "./narrow-widgets.js";
import { scrollSpy } from "./scroll-spy.js";
import { NacaraTabs } from "./tabs.js";
import { foldMemory, menuFilter } from "./sidebar-menu.js";

customElements.define("nacara-tabs", NacaraTabs);
customElements.define("nacara-tab", class extends HTMLElement {});

themePicker();
copyButtons();
dropdowns();
mobileMenu();
narrowWidgets();
scrollSpy();
foldMemory();

for (const box of document.querySelectorAll("[data-nacara-menu-filter]")) menuFilter(box);
