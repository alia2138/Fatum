(function () {
    const cfg = window.socialConfig;
    const container = document.getElementById("social-buttons");
    if (!cfg || !container) return;

    const errorEl = document.getElementById("error");
    const showError = (msg) => { if (errorEl) errorEl.textContent = msg; };

    // Кнопки Google і GitHub
    ["google", "github"].forEach((provider) => {
        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "btn-social";
        btn.textContent = provider === "google" ? "Увійти через Google" : "Увійти через GitHub";
        btn.addEventListener("click", () => {
            showError("");
            window.location.href = oauthLoginUrl(provider, cfg.getRole(), cfg.getBirthDate());
        });
        container.appendChild(btn);
    });

    // Віджет Telegram
    window.onTelegramAuth = async function (user) {
        showError("");
        try {
            const data = await postJson("/auth/telegram", {
                ...user,
                role: cfg.getRole(),
                birthDate: cfg.getBirthDate()
            });
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
    container.appendChild(tgScript);
})();