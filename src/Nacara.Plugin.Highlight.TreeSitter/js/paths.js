// The theme emits plugin scripts as classic deferred scripts, which still have document.currentScript.
const BASE = new URL(".", document.currentScript.src).href;

export const at = (name) => new URL(name, BASE).href;
