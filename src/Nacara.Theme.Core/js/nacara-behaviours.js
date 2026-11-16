import { themePicker } from "./color-scheme.js";
import { copyButtons } from "./copy-buttons.js";
import { dropdowns } from "./dropdowns.js";
import { mobileMenu } from "./mobile-menu.js";
import { narrowWidgets } from "./narrow-widgets.js";
import { scrollSpy } from "./scroll-spy.js";
import { NacaraTabs } from "./tabs.js";

customElements.define("nacara-tabs", NacaraTabs);
customElements.define("nacara-tab", class extends HTMLElement {});

themePicker();
copyButtons();
dropdowns();
mobileMenu();
narrowWidgets();
scrollSpy();
