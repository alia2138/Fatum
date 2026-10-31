// Профіль: спочатку перегляд (як у соцмережах), редагування — окремою кнопкою
let profileEditing = false;

async function renderProfile(container) {
    const p = await api("/profile/me");

    if (profileEditing) {
        container.replaceChildren(profileEditView(p, container));
        return;
    }

    let stats = null;
    if (p.role === "Consultant") {
        try { stats = await api(`/consultants/${p.id}`); } catch (err) { stats = null; }
    }
    container.replaceChildren(profileInfoView(p, stats, container));
}

function chips(items, labelOf) {
    return el("div", { class: "chips" }, ...items.map((x) => el("span", { class: "chip" }, labelOf ? labelOf(x) : x)));
}

function profileInfoView(p, stats, container) {
    const isClient = p.role === "Client";
    const c = p.client;
    const k = p.consultant;

    const statCell = (value, label) => el("div", { class: "stat" }, el("strong", {}, value), el("span", { class: "muted" }, label));

    const statsRow = isClient ? null : el("div", { class: "stats" },
        statCell(String(k.experienceYears), "років досвіду"),
        statCell(String(stats ? stats.reviewsCount : 0), "відгуків"),
        statCell(stats && stats.reviewsCount ? stats.ratingAverage.toFixed(1) : "—", "рейтинг"),
        statCell(`${k.priceUah} ₴`, `за ${k.sessionMinutes} хв`));

    const details = isClient
        ? [
            el("p", {}, "Дата народження: " + (c && c.birthDate ? formatDateOnly(c.birthDate) : "—")),
            el("p", {}, "Таймзона: " + ((c && c.timezone) || "—")),
            el("p", {}, "Цікавлять теми:"),
            chips(c ? c.interests : [], (t) => TOPIC_TITLES[t] || t)
        ]
        : [
            el("p", {}, "Спеціалізації:"),
            chips(k.specializations, (t) => TOPIC_TITLES[t] || t),
            el("p", {}, "Мови: " + (k.languages.join(", ") || "—")),
            el("p", {}, "Telegram: " + (k.contactTelegram || "—")),
            el("p", {}, k.isAcceptingClients ? "Приймає нових клієнтів" : "Зараз не приймає нових клієнтів")
        ];

    return el("div", { class: "page" },
        el("div", { class: "profile-top" },
            el("div", { class: "avatar xl" }, p.photoUrl ? el("img", { src: assetUrl(p.photoUrl), alt: "" }) : initials(p.displayName)),
            el("div", { class: "profile-id" },
                el("h2", {}, p.displayName || p.email),
                el("p", { class: "muted" }, isClient ? "Клієнт" : "Таролог"),
                el("button", { type: "button", class: "ghost", onclick: () => {
                    profileEditing = true;
                    renderProfile(container);
                } }, "Редагувати профіль"))),
        p.bio ? el("p", { class: "bio" }, p.bio) : muted("Опис ще не додано."),
        statsRow,
        el("section", { class: "card-block" }, ...details),
        p.isComplete ? null : el("p", { class: "hint" }, "Щоб профіль був повним, заповніть: " + p.missingFields.join(", ")),
        el("button", { type: "button", class: "ghost logout", onclick: logout }, "Вийти з акаунта"));
}

function profileEditView(p, container) {
    const isClient = p.role === "Client";
    const c = p.client;
    const k = p.consultant;
    const errorEl = el("p", { class: "error" });
    const displayName = el("input", { type: "text", maxlength: "80", value: p.displayName || "" });
    const bio = el("textarea", { rows: "4", maxlength: "1000", value: p.bio || "" });

    const photoInput = el("input", { type: "file", accept: "image/*" });
    const photoBlock = el("section", { class: "card-block" },
        el("h3", {}, "Фото"),
        el("div", { class: "row" },
            el("div", { class: "avatar big" }, p.photoUrl ? el("img", { src: assetUrl(p.photoUrl), alt: "" }) : initials(p.displayName)),
            el("div", { class: "photo-actions" },
                photoInput,
                el("button", { type: "button", class: "ghost", onclick: async () => {
                    if (!photoInput.files[0]) return alert("Оберіть файл.");
                    const form = new FormData();
                    form.append("file", photoInput.files[0]);
                    try {
                        await api("/profile/photo", { method: "POST", form: form });
                        renderProfile(container);
                    } catch (err) {
                        alert(err.message);
                    }
                } }, "Завантажити фото"),
                p.photoUrl ? el("button", { type: "button", class: "ghost", onclick: async () => {
                    try {
                        await api("/profile/photo", { method: "DELETE" });
                        renderProfile(container);
                    } catch (err) {
                        alert(err.message);
                    }
                } }, "Видалити фото") : null)));

    let fields;
    let collect;
    let endpoint;

    if (isClient) {
        endpoint = "/profile/client";
        const birth = el("input", { type: "date", value: c && c.birthDate ? c.birthDate.slice(0, 10) : "" });
        const timezone = el("input", { type: "text", placeholder: "Europe/Kyiv", value: c && c.timezone ? c.timezone : "" });
        const interests = checkboxGroup(Object.entries(TOPIC_TITLES), c ? c.interests : []);
        fields = [
            el("label", {}, "Ім'я", displayName),
            el("label", {}, "Про себе", bio),
            el("label", {}, "Дата народження", birth),
            el("label", {}, "Таймзона", timezone),
            el("span", { class: "label-text" }, "Цікавлять теми"),
            interests.node
        ];
        collect = () => ({
            displayName: displayName.value.trim() || null,
            bio: bio.value.trim() || null,
            birthDate: birth.value || null,
            timezone: timezone.value.trim() || null,
            interests: interests.values()
        });
    } else {
        endpoint = "/profile/consultant";
        const specs = checkboxGroup(Object.entries(TOPIC_TITLES), k.specializations);
        const experience = el("input", { type: "number", min: "0", value: k.experienceYears });
        const price = el("input", { type: "number", min: "0", value: k.priceUah });
        const minutes = el("input", { type: "number", min: "15", value: k.sessionMinutes });
        const languages = el("input", { type: "text", placeholder: "Українська, English", value: k.languages.join(", ") });
        const accepting = el("input", { type: "checkbox", checked: k.isAcceptingClients });
        const telegram = el("input", { type: "text", placeholder: "@username", value: k.contactTelegram || "" });
        fields = [
            el("label", {}, "Ім'я", displayName),
            el("label", {}, "Про себе", bio),
            el("span", { class: "label-text" }, "Спеціалізації"),
            specs.node,
            el("label", {}, "Досвід, років", experience),
            el("label", {}, "Ціна за сесію, ₴", price),
            el("label", {}, "Тривалість сесії, хв", minutes),
            el("label", {}, "Мови", languages),
            el("label", {}, "Контакт у Telegram", telegram),
            el("label", { class: "check" }, accepting, "Приймаю нових клієнтів")
        ];
        collect = () => ({
            displayName: displayName.value.trim() || null,
            bio: bio.value.trim() || null,
            specializations: specs.values(),
            experienceYears: Number(experience.value),
            languages: languages.value.split(",").map((s) => s.trim()).filter(Boolean),
            priceUah: Number(price.value),
            sessionMinutes: Number(minutes.value),
            isAcceptingClients: accepting.checked,
            contactTelegram: telegram.value.trim() || null
        });
    }

    const form = el("form", { onsubmit: async (e) => {
        e.preventDefault();
        errorEl.textContent = "";
        try {
            await api(endpoint, { method: "PATCH", body: collect() });
            profileEditing = false;
            renderProfile(container);
        } catch (err) {
            errorEl.textContent = err.message;
        }
    } },
        ...fields,
        errorEl,
        el("div", { class: "form-actions" },
            el("button", { type: "button", class: "ghost", onclick: () => {
                profileEditing = false;
                renderProfile(container);
            } }, "Скасувати"),
            el("button", { type: "submit", class: "primary" }, "Зберегти")));

    return el("div", { class: "page" },
        el("button", { type: "button", class: "ghost", onclick: () => {
            profileEditing = false;
            renderProfile(container);
        } }, "← До профілю"),
        el("h2", {}, "Редагування профілю"),
        photoBlock,
        el("section", { class: "card-block" }, el("h3", {}, "Дані"), form));
}

function checkboxGroup(entries, selected) {
    const boxes = entries.map(([key, label]) => {
        const input = el("input", { type: "checkbox", value: key, checked: selected.includes(key) });
        return { key, input, node: el("label", { class: "check" }, input, label) };
    });
    return {
        node: el("div", { class: "checks" }, ...boxes.map((b) => b.node)),
        values: () => boxes.filter((b) => b.input.checked).map((b) => b.key)
    };
}
