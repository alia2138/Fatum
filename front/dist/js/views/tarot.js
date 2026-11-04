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
        el("button", { type: "button", class: "chat-title", title: item.question, onclick: () => go("tarot", item.id) }, item.question),
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
        replayTarot(container, detail);
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
                    startTarotChat(container, topic);
                }
            }, topic.title));
        });
    } catch (err) {
        grid.replaceChildren(notice(err.message));
    }
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


// ===== Гадання як чат =====
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

function scrollDown(log) {
    log.scrollTop = log.scrollHeight;
}

function botTyping(log) {
    const node = el("div", { class: "msg assistant typing" },
        el("span", { class: "dots" }, el("i"), el("i"), el("i")));
    log.append(node);
    scrollDown(log);
    return node;
}

async function say(log, text) {
    const typing = botTyping(log);
    await sleep(Math.min(1000, 350 + text.length * 10));
    typing.replaceWith(messageBubble("assistant", "Таролог", text));
    scrollDown(log);
}

// Питання в чаті: повертає текст, коли він пройшов перевірку довжини
function ask(log, bar, placeholder, min, max) {
    return new Promise((resolve) => {
        const input = el("textarea", { rows: "2", maxlength: String(max), placeholder: placeholder });
        const errorEl = el("p", { class: "error" });
        const send = el("button", { type: "submit", class: "primary" }, "Надіслати");
        const form = el("form", { class: "chat-input", onsubmit: (e) => {
            e.preventDefault();
            const text = input.value.trim();
            if (text.length < min) {
                errorEl.textContent = `Напишіть щонайменше ${min} символів.`;
                return;
            }
            bar.replaceChildren();
            log.append(messageBubble("user", "Ви", text));
            scrollDown(log);
            resolve(text);
        } }, input, send);
        input.addEventListener("keydown", (e) => {
            if (e.key === "Enter" && !e.shiftKey) {
                e.preventDefault();
                form.requestSubmit();
            }
        });
        bar.replaceChildren(form, errorEl);
        input.focus();
    });
}

function shuffleStage() {
    return el("div", { class: "tarot-deck" },
        ...[0, 1, 2, 3, 4, 5].map((i) => el("div", { class: "tarot-deck-card", style: `--i:${i}` })));
}

function spreadRow(log) {
    const row = el("div", { class: "tarot-spread" });
    log.append(row);
    return row;
}

function cardSlot(c) {
    const flip = el("div", { class: "tarot-flip" },
        el("div", { class: "tarot-back" }),
        el("div", { class: "tarot-front" + (c.isReversed ? " reversed" : "") },
            el("span", { class: "tarot-sym" }, "✦")));
    return { slot: el("div", { class: "tarot-slot" }, flip), flip: flip, c: c };
}

function captionOf(c) {
    return el("div", { class: "tarot-caption" },
        el("span", { class: "muted" }, c.positionName),
        el("strong", {}, c.cardName + (c.isReversed ? " (перевернута)" : "")),
        el("p", {}, c.meaning));
}

// Карти без анімації (історія)
function showCardsStatic(log, cards) {
    const row = spreadRow(log);
    cards.forEach((c) => {
        const item = cardSlot(c);
        item.slot.classList.add("in");
        item.flip.classList.add("flipped");
        item.slot.append(captionOf(c));
        row.append(item.slot);
    });
    scrollDown(log);
}

// Карти вилітають з колоди, перевертаються, під кожною — назва і значення
async function revealCards(log, cards) {
    const row = spreadRow(log);
    const items = cards.map((c) => {
        const item = cardSlot(c);
        row.append(item.slot);
        return item;
    });
    for (const item of items) {
        await sleep(280);
        item.slot.classList.add("in");
        scrollDown(log);
    }
    for (const item of items) {
        await sleep(450);
        item.flip.classList.add("flipped");
        await sleep(350);
        item.slot.append(captionOf(item.c));
        scrollDown(log);
    }
}

// Тасування → запит → розклад нових карт (prevCount — скільки карт уже було)
async function drawCards(log, request, prevCount) {
    const deck = shuffleStage();
    log.append(deck);
    scrollDown(log);

    const pending = request().then((detail) => ({ ok: true, detail }), (err) => ({ ok: false, err }));
    await sleep(2600);
    const result = await pending;
    deck.remove();

    if (!result.ok) {
        await say(log, "Не вдалося розкласти карти: " + result.err.message);
        return null;
    }
    await revealCards(log, result.detail.cards.slice(prevCount));
    return result.detail;
}

function lastAssistant(detail) {
    return [...detail.messages].reverse().find((m) => m.role === "Assistant");
}

async function startTarotChat(container, topic) {
    const log = el("div", { class: "chat-messages tarot-log" });
    const bar = el("div", { class: "tarot-bar" });
    container.replaceChildren(el("div", { class: "chat-page tarot-chat" }, log, bar));

    await say(log, "Розкажіть, будь ласка, що зараз відбувається у вашому житті. Чим більше деталей, тим точніше буде розклад.");
    const background = await ask(log, bar, "Коротко опишіть ситуацію...", 10, 1500);

    await say(log, "Агаа, зрозуміло. Яке конкретне питання вас найбільше хвилює?");
    const question = await ask(log, bar, "Ваше питання...", 3, 500);

    const detail = await drawCards(log, () => api("/tarot/readings", {
        method: "POST",
        body: { topic: topic.key, background: background, question: question }
    }), 0);
    if (!detail) return;

    history.replaceState(null, "", `#tarot/${detail.id}`);
    refreshHistory();

    const reply = lastAssistant(detail);
    await say(log, reply ? reply.text : "Трактування поки недоступне.");
    followUpLoop(log, bar, detail.id, detail.cards.length);
}

// Відтворення збереженої розмови у такому самому вигляді
function replayTarot(container, detail) {
    const log = el("div", { class: "chat-messages tarot-log" });
    const bar = el("div", { class: "tarot-bar" });
    container.replaceChildren(el("div", { class: "chat-page tarot-chat" }, log, bar));

    log.append(messageBubble("assistant", "Таролог", "Розкажіть, будь ласка, що зараз відбувається у вашому житті. Чим більше деталей, тим точніше буде розклад."));
    log.append(messageBubble("user", "Ви", detail.background));
    log.append(messageBubble("assistant", "Таролог", "Агаа, зрозуміло. Яке конкретне питання вас найбільше хвилює?"));
    log.append(messageBubble("user", "Ви", detail.question));

    let shownRound = 0;
    const showRound = (round) => {
        while (shownRound < round) {
            shownRound++;
            const cards = detail.cards.filter((c) => c.round === shownRound);
            if (cards.length) showCardsStatic(log, cards);
        }
    };

    showRound(1);
    let seenAssistant = false;
    detail.messages.forEach((m) => {
        if (m.role === "User") {
            if (seenAssistant) log.append(messageBubble("user", "Ви", m.text));
            return;
        }
        if (seenAssistant) showRound(shownRound + 1);
        seenAssistant = true;
        log.append(messageBubble("assistant", "Таролог", m.text));
    });

    scrollDown(log);
    followUpLoop(log, bar, detail.id, detail.cards.length);
}

async function followUpLoop(log, bar, readingId, cardCount) {
    let count = cardCount;
    while (true) {
        const text = await ask(log, bar, "Уточнювальне питання...", 3, 500);
        const detail = await drawCards(log, () => api(`/tarot/readings/${readingId}/messages`, {
            method: "POST",
            body: { question: text }
        }), count);
        if (!detail) continue;
        count = detail.cards.length;
        const reply = lastAssistant(detail);
        await say(log, reply ? reply.text : "Відповідь поки недоступна.");
    }
}
