/* ==================================================================
   OPEXMINDS THEME RUNTIME
   - Resolves dark/light per system preference (default) or the
     user's saved override (localStorage "gmc-theme").
   - Exposes window.__oxTheme { mode, apply() } for the toggle.
   White-label safe: no brand names, no endpoints.
   ================================================================== */
(function () {
    var STORAGE_KEY = "gmc-theme";
    var root = document.documentElement;

    function resolveMode() {
        try {
            var saved = localStorage.getItem(STORAGE_KEY);
            if (saved === "dark" || saved === "light") return saved;
        } catch (e) { /* storage unavailable — fall through */ }
        if (window.matchMedia && window.matchMedia("(prefers-color-scheme: light)").matches) {
            return "light";
        }
        return "dark"; /* Opexminds brand default */
    }

    function apply(mode) {
        root.setAttribute("data-theme", mode);
        root.style.colorScheme = mode; /* native controls follow */
        try {
            document.dispatchEvent(new CustomEvent("themechanged", { detail: { mode: mode } }));
        } catch (e) { /* older browsers */ }
    }

    var current = resolveMode();
    apply(current);

    /* Live-follow system changes unless the user pinned a choice */
    if (window.matchMedia) {
        var mq = window.matchMedia("(prefers-color-scheme: light)");
        var onChange = function (e) {
            try { if (localStorage.getItem(STORAGE_KEY)) return; } catch (err) {}
            apply(e.matches ? "light" : "dark");
        };
        if (mq.addEventListener) mq.addEventListener("change", onChange);
        else if (mq.addListener) mq.addListener(onChange); /* Safari <14 */
    }

    /* Public API: toggle buttons call window.__oxTheme.toggle() */
    window.__oxTheme = {
        get mode() { return root.getAttribute("data-theme") || current; },
        apply: apply,
        toggle: function () {
            var next = (root.getAttribute("data-theme") === "light") ? "dark" : "light";
            apply(next);
            try { localStorage.setItem(STORAGE_KEY, next); } catch (e) {}
            return next;
        }
    };
})();
