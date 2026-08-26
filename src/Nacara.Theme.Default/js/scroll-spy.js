export const scrollSpy = () => {
    const entries = [...document.querySelectorAll(".nacara-toc__link")]
        .map((link) => ({
            link,
            heading: document.getElementById(decodeURIComponent(link.hash.slice(1))),
        }))
        .filter((entry) => entry.heading);

    if (entries.length === 0) return;

    let active = null;
    let pinned = null;

    const setActive = (link) => {
        if (link === active) return;

        if (active) delete active.dataset.active;
        if (link) link.dataset.active = "true";

        active = link;
    };

    const update = () => {
        if (pinned) {
            setActive(pinned);
            return;
        }

        const bar = document.querySelector(".nacara-navbar");
        const line = (bar ? bar.getBoundingClientRect().height : 56) + 24;

        const atBottom =
            Math.ceil(scrollY + innerHeight) >= document.documentElement.scrollHeight - 2;

        if (atBottom) {
            setActive(entries[entries.length - 1].link);
            return;
        }

        let current = entries[0];

        for (const entry of entries) {
            if (entry.heading.getBoundingClientRect().top - line > 1) break;

            current = entry;
        }

        setActive(current.link);
    };

    let queued = false;

    const onScroll = () => {
        if (queued) return;

        queued = true;

        requestAnimationFrame(() => {
            queued = false;
            update();
        });
    };

    const pinFromHash = () => {
        if (!location.hash) return;

        const wanted = decodeURIComponent(location.hash.slice(1));
        const entry = entries.find((entry) => entry.heading.id === wanted);

        if (entry) {
            pinned = entry.link;
            setActive(entry.link);
        }
    };

    for (const entry of entries) {
        entry.link.addEventListener("click", () => {
            pinned = entry.link;
            setActive(entry.link);
        });
    }

    const release = () => {
        pinned = null;
    };

    addEventListener("wheel", release, { passive: true });
    addEventListener("touchmove", release, { passive: true });
    addEventListener("keydown", (event) => {
        if (/^(Arrow|Page)|^(Home|End|Space| )$/.test(event.key)) release();
    });

    addEventListener("scroll", onScroll, { passive: true });
    addEventListener("resize", onScroll);
    addEventListener("hashchange", () => {
        pinFromHash();
        onScroll();
    });

    update();
    pinFromHash();
};
