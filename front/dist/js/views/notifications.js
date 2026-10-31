// Сповіщення: список і позначка прочитаного
async function renderNotifications(container) {
    const list = await api("/notifications");
    const reload = () => renderNotifications(container);

    container.replaceChildren(el("div", { class: "page" },
        el("h2", {}, "Сповіщення"),
        list.length
            ? el("div", { class: "list" }, ...list.map((n) => notificationRow(n, reload)))
            : muted("Сповіщень поки немає.")));
}

function notificationRow(n, reload) {
    return el("div", { class: "list-item" + (n.isRead ? "" : " unread") },
        el("div", { class: "list-main" },
            el("strong", {}, n.title),
            el("span", {}, n.body),
            el("small", { class: "muted" }, formatDateTime(n.createdAt))),
        n.isRead ? null : el("button", { type: "button", class: "ghost", onclick: async () => {
            try {
                await api(`/notifications/${n.id}/read`, { method: "POST" });
                reload();
                refreshBadge();
            } catch (err) {
                alert(err.message);
            }
        } }, "Прочитано"));
}
