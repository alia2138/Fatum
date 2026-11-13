(function () {
    const cfg = window.socialConfig;
    const container = document.getElementById("social-buttons");
    if (!cfg || !container) return;

    const errorEl = document.getElementById("error");
    const showError = (msg) => { if (errorEl) errorEl.textContent = msg; };
    const telegramReady = !!cfg.botName && cfg.botName !== "YOUR_BOT_USERNAME";

    const providers = [
        { key: "google", label: "Увійти через Google" },
        { key: "github", label: "Увійти через GitHub" },
        { key: "telegram", label: "Увійти через Telegram" }
    ];

    const buttons = providers
        .filter((p) => p.key !== "telegram" || !telegramReady)
        .map((p) => el("button", {
            type: "button",
            class: "btn-social",
            title: p.label,
            "aria-label": p.label,
            onclick: () => {
                showError("");
                if (p.key === "telegram") {
                    showError("Вхід через Telegram ще не налаштовано.");
                    return;
                }
                window.location.href = oauthLoginUrl(p.key, cfg.getRole(), cfg.getBirthDate());
            }
        }, brandIcon(p.key)));

    container.replaceChildren(...buttons);

    if (telegramReady) {
        window.onTelegramAuth = async function (user) {
            showError("");
            try {
                const data = await postJson("/auth/telegram", { ...user, role: cfg.getRole(), birthDate: cfg.getBirthDate() });
                saveSession(data);
                window.location.href = `success.html?type=${cfg.successType}`;
            } catch (err) {
                showError(err.message);
            }
        };
        const tgScript = document.createElement("script");
        tgScript.src = "https://telegram.org/js/telegram-widget.js?22";
        tgScript.async = true;
        tgScript.setAttribute("data-telegram-login", cfg.botName);
        tgScript.setAttribute("data-size", "large");
        tgScript.setAttribute("data-radius", "10");
        tgScript.setAttribute("data-onauth", "onTelegramAuth(user)");
        container.append(tgScript);
    }
})();
