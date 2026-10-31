// Обране: таро-консультанти і власні гадання
let favoritesTab = "Consultant";

async function renderFavorites(container) {
    const items = await api(`/favorites?type=${favoritesTab}`);
    const reload = () => renderFavorites(container);

    const tabs = el("div", { class: "tabs" },
        ...[["Consultant", "Таро-консультанти"], ["Reading", "Гадання"]].map(([key, label]) =>
            el("button", { type: "button", class: "tab" + (favoritesTab === key ? " active" : ""), onclick: () => {
                favoritesTab = key;
                reload();
            } }, label)));

    container.replaceChildren(el("div", { class: "page" },
        el("h2", {}, "Обране"),
        tabs,
        items.length
            ? el("div", { class: "list" }, ...items.map((f) => favoriteRow(f, reload)))
            : muted("Тут поки порожньо.")));
}

function favoriteRow(f, reload) {
    const isConsultant = f.type === "Consultant";
    return el("div", { class: "list-item" },
        el("button", { type: "button", class: "list-main", onclick: () => go(isConsultant ? "catalog" : "tarot", f.targetId) },
            el("strong", {}, f.title),
            el("span", { class: "muted" }, isConsultant ? "Таро-консультант" : "Гадання")),
        el("button", { type: "button", class: "icon-btn", onclick: async () => {
            try {
                await api(`/favorites/${f.id}`, { method: "DELETE" });
                reload();
            } catch (err) {
                alert(err.message);
            }
        } }, "×"));
}
