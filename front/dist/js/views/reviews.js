// Відгуки про консультанта і відповіді на них
async function renderReviews(container) {
    const me = await api("/profile/me");
    const data = await api(`/reviews/consultant/${me.id}?page=1&pageSize=50`);
    const reload = () => renderReviews(container);

    container.replaceChildren(el("div", { class: "page" },
        el("h2", {}, "Відгуки"),
        el("p", { class: "hint" }, `Всього відгуків: ${data.total}`),
        data.items.length
            ? el("div", { class: "list" }, ...data.items.map((r) => reviewRow(r, reload)))
            : muted("Відгуків поки немає.")));
}

function reviewRow(r, reload) {
    const errorEl = el("p", { class: "error" });
    const body = [
        el("div", { class: "row" }, el("strong", {}, r.clientName), el("span", { class: "stars" }, stars(r.rating))),
        el("small", { class: "muted" }, formatDateTime(r.createdAt)),
        el("p", {}, r.text || "")
    ];

    if (r.consultantReply) {
        body.push(el("p", { class: "reply" }, "Ваша відповідь: " + r.consultantReply));
    } else {
        const replyText = el("textarea", { rows: "2", maxlength: "1000", placeholder: "Ваша відповідь..." });
        body.push(el("form", { onsubmit: async (e) => {
            e.preventDefault();
            const text = replyText.value.trim();
            if (!text) return;
            try {
                await api(`/reviews/${r.id}/reply`, { method: "POST", body: { text: text } });
                reload();
            } catch (err) {
                errorEl.textContent = err.message;
            }
        } }, replyText, errorEl, el("button", { type: "submit", class: "primary" }, "Відповісти")));
    }

    return el("article", { class: "card-block review" }, ...body);
}
