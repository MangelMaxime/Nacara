// Inlined into <head>: it must run before the first paint.

import { chosen, settle } from "./color-scheme.js";

settle(chosen());
