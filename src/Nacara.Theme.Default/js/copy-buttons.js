export const copyButtons = () => {
    const announcer = document.createElement("div");
    announcer.setAttribute("role", "status");
    announcer.className = "nacara-visually-hidden";
    document.body.append(announcer);

    // The Clipboard API exists only in a secure context, which a site served over plain http is not.
    const write = async (text) => {
        if (navigator.clipboard?.writeText) {
            await navigator.clipboard.writeText(text);
            return true;
        }

        const staging = document.createElement("textarea");
        staging.value = text;
        staging.setAttribute("readonly", "");
        staging.style.position = "fixed";
        staging.style.top = "0";
        staging.style.opacity = "0";
        document.body.append(staging);
        staging.select();

        try {
            return document.execCommand("copy");
        } finally {
            staging.remove();
        }
    };

    document.addEventListener("click", async (event) => {
        const button = event.target.closest(".nacara-code__copy");
        if (!button) return;

        // A block that folds some of its lines is drawn as more than one <pre>.
        const block = button.closest(".nacara-code");
        const text =
            block?.dataset.source ??
            [...(button.closest(".nacara-code__body")?.querySelectorAll("code") ?? [])]
                .map((node) => node.textContent)
                .join("");

        if (!text) return;

        let copied = false;

        try {
            copied = await write(text.replace(/\n$/, ""));
        } catch {
            copied = false;
        }

        const said = copied ? "Copied" : "Press Ctrl+C to copy";
        button.dataset.copied = String(copied);
        button.dataset.feedback = said;
        announcer.textContent = said;

        setTimeout(
            () => {
                delete button.dataset.copied;
                delete button.dataset.feedback;
                announcer.textContent = "";
            },
            copied ? 1500 : 3000,
        );
    });
};
