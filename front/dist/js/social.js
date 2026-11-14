(function () {
    const cfg = window.socialConfig;
    const container = document.getElementById("social-buttons");
    if (!cfg || !container) return;

    const errorEl = document.getElementById("error");
    const showError = (msg) => { if (errorEl) errorEl.textContent = msg; };
    const isLocal = location.hostname === "localhost" || location.hostname === "127.0.0.1";

    const providers = [
        { key: "google", label: "Увійти через Google" },
        { key: "github", label: "Увійти через GitHub" },
        { key: "telegram", label: "Увійти через Telegram" }
    ];

    container.replaceChildren(...providers.map((p) => el("button", {
        type: "button",
        class: "btn-social",
        title: p.label,
        "aria-label": p.label,
        onclick: () => {
            showError("");
            if (p.key !== "telegram") {
                window.location.href = oauthLoginUrl(p.key, cfg.getRole(), cfg.getBirthDate());
                return;
            }
            if (isLocal) {
                showError("Вхід через Telegram працює тільки на публічному сайті, не на localhost.");
                return;
            }
            if (!cfg.botId) {
                showError("Вхід через Telegram ще не налаштовано.");
                return;
            }
            sessionStorage.setItem("taro_tg_role", cfg.getRole());
            sessionStorage.setItem("taro_tg_birth", cfg.getBirthDate() || "");
            const back = location.origin + location.pathname;
            window.location.href = "https://oauth.telegram.org/auth?bot_id=" + encodeURIComponent(cfg.botId)
                + "&origin=" + encodeURIComponent(location.origin)
                + "&return_to=" + encodeURIComponent(back)
                + "&request_access=write";
        }
    }, brandIcon(p.key))));
})();
