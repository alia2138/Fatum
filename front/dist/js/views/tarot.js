// Гадання: тема, передісторія, питання, розклад і уточнення в чаті. Сайдбар з історією.
let topicsCache = null;
let selectedTopic = null;
let savedBackground = "";
let readingsCache = [];

function pinnedKey() {
    const user = getSessionUser();
    return "taro_pinned_" + (user ? user.userId : "anon");
}

function getPinnedIds() {
    try { return JSON.parse(localStorage.getItem(pinnedKey())) || []; }
    catch (e) { return []; }
}

function setPinnedIds(ids) {
    localStorage.setItem(pinnedKey(), JSON.stringify(ids));
}

function togglePin(id) {
    const ids = getPinnedIds();
    setPinnedIds(ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id]);
    renderHistory();
}

async function refreshHistory() {
    try { readingsCache = await api("/tarot/readings"); }
    catch (err) { readingsCache = []; }
    renderHistory();
}

function historyItem(item, pinned) {
    const active = location.hash === `#tarot/${item.id}`;
    return el("div", { class: "chat-item" + (active ? " active" : "") },
        el("button", { type: "button", class: "chat-title", title: item.question, onclick: () => go("tarot", item.id) }, item.title),
        el("button", { type: "button", class: "chat-action", onclick: () => togglePin(item.id) }, pinned ? "Відкріпити" : "Закріпити"),
        el("button", { type: "button", class: "chat-action", onclick: () => deleteReading(item.id) }, "×"));
}

function renderHistory() {
    const pinnedIds = getPinnedIds();
    const pinned = readingsCache.filter((r) => pinnedIds.includes(r.id));
    const others = readingsCache.filter((r) => !pinnedIds.includes(r.id));
    const pinnedList = document.getElementById("pinned-list");
    const recentList = document.getElementById("recent-list");

    document.getElementById("pinned-section").hidden = pinned.length === 0;
    pinnedList.replaceChildren(...pinned.map((r) => historyItem(r, true)));
    recentList.replaceChildren(...others.map((r) => historyItem(r, false)));
    if (readingsCache.length === 0) {
        recentList.append(el("p", { class: "muted" }, "Поки що немає гадань."));
    }
}

async function deleteReading(id) {
    if (!confirm("Видалити це гадання?")) return;
    try {
        await api(`/tarot/readings/${id}`, { method: "DELETE" });
        setPinnedIds(getPinnedIds().filter((x) => x !== id));
        if (location.hash === `#tarot/${id}`) go("tarot");
        refreshHistory();
    } catch (err) {
        alert(err.message);
    }
}

async function renderTarot(container, readingId) {
    if (readingId) {
        const detail = await api(`/tarot/readings/${readingId}`);
        showReading(container, detail);
        return;
    }
    renderTopics(container);
}

// Крок 1: тема (опис показується підказкою при наведенні)
async function renderTopics(container) {
    selectedTopic = null;
    savedBackground = "";

    const grid = el("div", { class: "topics" });
    container.replaceChildren(el("div", { class: "page" },
        el("h2", { class: "greeting" }, "Із чого почнемо?"),
        el("p", { class: "hint" }, "Оберіть тему, потім розкажіть ситуацію і поставте питання."),
        grid));

    try {
        if (!topicsCache) topicsCache = await api("/tarot/topics");
        topicsCache.forEach((topic) => {
            grid.append(el("button", {
                type: "button",
                class: "topic-card",
                title: `${topic.story}\n\nПозиції: ${topic.positions.join(", ")}`,
                onclick: () => {
                    selectedTopic = topic;
                    renderBackground(container);
                }
            }, topic.title));
        });
    } catch (err) {
        grid.replaceChildren(notice(err.message));
    }
}

// Крок 2: передісторія
function renderBackground(container) {
    const input = el("textarea", { rows: "6", maxlength: "1500", placeholder: "Коротко опишіть свою ситуацію..." });
    input.value = savedBackground;
    const errorEl = el("p", { class: "error" });

    container.replaceChildren(el("div", { class: "page" },
        el("h2", {}, selectedTopic.title),
        el("p", { class: "hint" }, "Розкажіть коротко вашу ситуацію (від 10 до 1500 символів)."),
        el("form", { onsubmit: (e) => {
            e.preventDefault();
            const text = input.value.trim();
            if (text.length < 10) {
                errorEl.textContent = "Напишіть трохи більше, щонайменше 10 символів.";
                return;
            }
            savedBackground = text;
            renderQuestion(container);
        } },
            input,
            errorEl,
            el("div", { class: "form-actions" },
                el("button", { type: "button", class: "ghost", onclick: () => renderTopics(container) }, "Назад"),
                el("button", { type: "submit", class: "primary" }, "Далі")))));
}

// Крок 3: питання, після чого створюється гадання
function renderQuestion(container) {
    const input = el("input", { type: "text", maxlength: "500", placeholder: "Ваше питання" });
    const errorEl = el("p", { class: "error" });
    const submit = el("button", { type: "submit", class: "primary" }, "Почати гадання");

    container.replaceChildren(el("div", { class: "page" },
        el("h2", {}, "Ваше питання"),
        el("form", { onsubmit: async (e) => {
            e.preventDefault();
            const question = input.value.trim();
            if (question.length < 3) {
                errorEl.textContent = "Питання має бути щонайменше 3 символи.";
                return;
            }
            submit.disabled = true;
            submit.textContent = "Тасуємо карти...";
            try {
                const detail = await api("/tarot/readings", {
                    method: "POST",
                    body: { topic: selectedTopic.key, background: savedBackground, question: question }
                });
                history.replaceState(null, "", `#tarot/${detail.id}`);
                refreshHistory();
                showReading(container, detail);
            } catch (err) {
                errorEl.textContent = err.message;
                submit.disabled = false;
                submit.textContent = "Почати гадання";
            }
        } },
            input,
            errorEl,
            el("div", { class: "form-actions" },
                el("button", { type: "button", class: "ghost", onclick: () => renderBackground(container) }, "Назад"),
                submit))));
}

// Розклад і чат з таро-аналітиком
function showReading(container, detail) {
    const log = el("div", { class: "chat-messages" },
        ...detail.messages.map((m) => messageBubble(
            m.role === "User" ? "user" : "assistant",
            m.role === "User" ? "Ви" : "Таролог",
            m.text)));

    const input = el("input", { type: "text", maxlength: "500", placeholder: "Уточнювальне питання..." });
    const errorEl = el("p", { class: "error" });
    const send = el("button", { type: "submit", class: "primary" }, "Надіслати");

    container.replaceChildren(el("div", { class: "page chat-page" },
        el("h2", {}, detail.title),
        el("p", { class: "hint" }, "Питання: " + detail.question),
        el("details", { class: "card-block" },
            el("summary", {}, `Карти (${detail.cards.length})`),
            el("ul", { class: "points" }, ...detail.cards.map((c) =>
                el("li", {}, el("strong", {}, `${c.positionName}: ${c.cardName}${c.isReversed ? " (перевернута)" : ""}`),
                    el("span", { class: "muted" }, ` ${c.meaning}`))))),
        log,
        errorEl,
        el("form", { class: "chat-input", onsubmit: async (e) => {
            e.preventDefault();
            const question = input.value.trim();
            if (question.length < 3) {
                errorEl.textContent = "Питання має бути щонайменше 3 символи.";
                return;
            }
            send.disabled = true;
            send.textContent = "Таролог думає...";
            try {
                const next = await api(`/tarot/readings/${detail.id}/messages`, { method: "POST", body: { question: question } });
                showReading(container, next);
            } catch (err) {
                errorEl.textContent = err.message;
                send.disabled = false;
                send.textContent = "Надіслати";
            }
        } }, input, send)));

    log.scrollTop = log.scrollHeight;
}
