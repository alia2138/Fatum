// Чати між клієнтом і таро-консультантом після підтвердження консультації
async function renderChats(container, conversationId) {
    if (conversationId) return renderChat(container, conversationId);

    const list = await api("/chats");
    list.sort((a, b) => new Date(b.lastMessageAt) - new Date(a.lastMessageAt));

    container.replaceChildren(el("div", { class: "page" },
        el("h2", {}, "Чати"),
        list.length
            ? el("div", { class: "list" }, ...list.map((c) => el("button", { type: "button", class: "list-item", onclick: () => go("chats", c.id) },
                el("div", { class: "list-main" },
                    el("strong", {}, c.otherPartyName),
                    el("span", { class: "muted" }, formatDateTime(c.lastMessageAt))))))
            : muted("Чатів поки немає. Вони з'являться після підтвердження консультації.")));
}

async function renderChat(container, id) {
    const [messages, list] = await Promise.all([api(`/chats/${id}/messages`), api("/chats")]);
    const conv = list.find((c) => c.id === id);
    const me = currentUserId();

    const log = el("div", { class: "chat-messages" }, ...messages.map((m) => chatMessage(m, me)));
    const input = el("input", { type: "text", maxlength: "2000", placeholder: "Напишіть повідомлення..." });
    const errorEl = el("p", { class: "error" });
    const send = el("button", { type: "submit", class: "primary" }, "Надіслати");

    container.replaceChildren(el("div", { class: "page chat-page" },
        el("button", { type: "button", class: "ghost", onclick: () => go("chats") }, "← До чатів"),
        el("h2", {}, conv ? conv.otherPartyName : "Чат"),
        log,
        errorEl,
        el("form", { class: "chat-input", onsubmit: async (e) => {
            e.preventDefault();
            const text = input.value.trim();
            if (!text) return;
            send.disabled = true;
            try {
                await api(`/chats/${id}/messages`, { method: "POST", body: { text: text } });
                renderChat(container, id);
            } catch (err) {
                errorEl.textContent = err.message;
                send.disabled = false;
            }
        } }, input, send)));

    log.scrollTop = log.scrollHeight;

    // Нові повідомлення з'являються самі, без оновлення сторінки
    stopPolling();
    let lastCount = messages.length;
    startPolling(async () => {
        try {
            const fresh = await api(`/chats/${id}/messages`);
            if (fresh.length !== lastCount) {
                lastCount = fresh.length;
                log.replaceChildren(...fresh.map((m) => chatMessage(m, me)));
                log.scrollTop = log.scrollHeight;
            }
        } catch (err) {
            // тимчасова помилка мережі: спробуємо наступним разом
        }
    }, 4000);
}

function chatMessage(m, me) {
    if (!m.senderId) return el("p", { class: "muted center" }, m.text);
    return el("div", { class: "msg " + (m.senderId === me ? "user" : "assistant") },
        el("span", {}, m.text),
        m.link ? el("a", { href: m.link, target: "_blank", rel: "noopener" }, "Посилання") : null,
        el("small", { class: "muted" }, formatDateTime(m.createdAt)));
}
