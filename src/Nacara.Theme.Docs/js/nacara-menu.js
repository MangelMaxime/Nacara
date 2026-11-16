import { foldMemory, menuFilter } from "./sidebar-menu.js";

foldMemory();

for (const box of document.querySelectorAll("[data-nacara-menu-filter]")) menuFilter(box);
