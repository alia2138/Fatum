// Консультації: клієнт бачить свої записи, консультант — заявки і керує ними
async function renderConsultations(container) {
    const role = currentRole();
    const list = await api("/consultations");
    list.sort((a, b) => new Date(b.startUtc) - new Date(a.startUtc));
    const reload = () => renderConsultations(container);

    container.replaceChildren(el("div", { class: "page wide" },
        el("h2", {}, role === "Consultant" ? "Заявки на консультації" : "Мої консультації"),
        list.length
            ? el("div", { class: "list" }, ...list.map((c) => consultationCard(c, role, reload)))
            : muted("Поки що немає консультацій.")));
}

async function act(reload, path, body, confirmText) {
    if (confirmText && !confirm(confirmText)) return;
    try {
        await api(path, { method: "POST", body: body });
        reload();
    } catch (err) {
        alert(err.message);
    }
}

function consultationCard(c, role, reload) {
    const isClient = role === "Client";
    const actions = el("div", { class: "actions row" });
    const card = el("article", { class: "card-block consultation" },
        el("div", { class: "row" }, el("strong", {}, isClient ? c.consultantName : c.clientName), statusBadge(c.status)),
        el("p", { class: "muted" }, `${formatDateTime(c.startUtc)} · ${c.durationMinutes} хв${c.topic ? " · " + (TOPIC_TITLES[c.topic] || c.topic) : ""}`),
        el("p", {}, c.question),
        c.meetingUrl ? el("p", {}, el("a", { href: c.meetingUrl, target: "_blank", rel: "noopener" }, "Посилання на зустріч")) : null,
        c.declineReason ? el("p", { class: "muted" }, "Причина відмови: " + c.declineReason) : null,
        actions);

    if (c.conversationId) {
        actions.append(el("button", { type: "button", class: "ghost", onclick: () => go("chats", c.conversationId) }, "Відкрити чат"));
    }

    if (isClient) {
        if (c.status === "Pending" || c.status === "Confirmed") {
            actions.append(el("button", { type: "button", class: "ghost",
                onclick: () => act(reload, `/consultations/${c.id}/cancel`, undefined, "Скасувати консультацію?") }, "Скасувати"));
        }
        if (c.status === "Completed") {
            actions.append(el("button", { type: "button", class: "ghost", onclick: (e) => {
                e.currentTarget.remove();
                openReviewForm(card, c.id, reload);
            } }, "Залишити відгук"));
        }
    } else {
        if (c.status === "Pending") {
            const url = el("input", { type: "url", placeholder: "Посилання на зустріч (необов'язково)" });
            actions.append(url,
                el("button", { type: "button", class: "primary",
                    onclick: () => act(reload, `/consultations/${c.id}/confirm`, { meetingUrl: url.value.trim() || null }) }, "Підтвердити"),
                el("button", { type: "button", class: "ghost", onclick: () => {
                    const reason = prompt("Причина відмови (необов'язково)");
                    if (reason === null) return;
                    act(reload, `/consultations/${c.id}/decline`, { reason: reason.trim() || null });
                } }, "Відхилити"));
        }
        if (c.status === "Confirmed") {
            actions.append(
                el("button", { type: "button", class: "primary",
                    onclick: () => act(reload, `/consultations/${c.id}/complete`) }, "Завершити"),
                el("button", { type: "button", class: "ghost",
                    onclick: () => act(reload, `/consultations/${c.id}/cancel`, undefined, "Скасувати консультацію?") }, "Скасувати"));
        }
    }

    return card;
}

function openReviewForm(card, consultationId, reload) {
    const rating = el("select", {}, ...[5, 4, 3, 2, 1].map((n) => el("option", { value: String(n) }, `${n} ★`)));
    const text = el("textarea", { rows: "3", maxlength: "1000", placeholder: "Ваш відгук (необов'язково)" });
    const errorEl = el("p", { class: "error" });

    card.append(el("form", { onsubmit: async (e) => {
        e.preventDefault();
        try {
            await api("/reviews", {
                method: "POST",
                body: { consultationId: consultationId, rating: Number(rating.value), text: text.value.trim() || null }
            });
            reload();
        } catch (err) {
            errorEl.textContent = err.message;
        }
    } }, rating, text, errorEl, el("button", { type: "submit", class: "primary" }, "Надіслати відгук")));
}
