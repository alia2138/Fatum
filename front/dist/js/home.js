if (!isLoggedIn()) {
    window.location.replace("login.html");
}

function el(tag, attrs = {}, ...children) {
    const node = document.createElement(tag);
    Object.entries(attrs).forEach(([key, value]) => {
        if (key.startsWith("on")) node.addEventListener(key.slice(2), value);
        else if (key === "class") node.className = value;
        else node.setAttribute(key, value);
    });
    children.forEach((child) => {
        node.append(typeof child === "string" ? document.createTextNode(child) : child);
    });
    return node;
}

async function authedFetch(path, options = {}) {
    const token = localStorage.getItem(TOKEN_KEY);
    const headers = { Authorization: "Bearer " + token };
    if (options.body !== undefined) headers["Content-Type"] = "application/json";

    let res;
    try {
        res = await fetch(API_BASE + path, {
            method: options.method || "GET",
            headers: headers,
            body: options.body !== undefined ? JSON.stringify(options.body) : undefined
        });
    } catch (e) {
        throw new Error("Немає зв'язку з сервером.");
    }

    if (res.status === 401) {
        logout();
        throw new Error("Сесія закінчилась. Увійдіть знову.");
    }

    let data = null;
    try {
        data = await res.json();
    } catch (e) {
        // відповідь без тіла (наприклад, 204)
    }

    if (!res.ok) {
        throw new Error((data && data.error) || "Сталася помилка.");
    }
    return data;
}

// Закріплення зберігаємо окремо для кожного користувача, лише в цьому браузері
const currentUser = getSessionUser();
const PINNED_KEY = "taro_pinned_" + (currentUser ? currentUser.userId : "anon");

function getPinnedIds() {
    try {
        return JSON.parse(localStorage.getItem(PINNED_KEY)) || [];
    } catch (e) {
        return [];
    }
}

function setPinnedIds(ids) {
    localStorage.setItem(PINNED_KEY, JSON.stringify(ids));
}

function togglePin(id) {
    const ids = getPinnedIds();
    const next = ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id];
    setPinnedIds(next);
    renderSidebar();
}

// Кеш списку гадань для бокової панелі
let readingsCache = [];

async function refreshSidebar() {
    try {
        readingsCache = await authedFetch("/tarot/readings");
    } catch (err) {
        readingsCache = [];
    }
    renderSidebar();
}

function renderChatItem(item, pinned) {
    const isActive = window.currentReadingId === item.id;

    return el("div", { class: "chat-item" + (isActive ? " active" : "") },
        el("button", {
            type: "button",
            class: "chat-title",
            title: item.question,
            onclick: () => openReading(item.id)
        }, item.title),
        el("button", {
            type: "button",
            class: "chat-action",
            title: pinned ? "Відкріпити" : "Закріпити",
            onclick: () => togglePin(item.id)
        }, pinned ? "Відкріпити" : "Закріпити"),
        el("button", {
            type: "button",
            class: "chat-action",
            title: "Видалити",
            onclick: () => deleteReading(item.id)
        }, "×")
    );
}

function renderSidebar() {
    const pinnedIds = getPinnedIds();
    const pinnedList = document.getElementById("pinned-list");
    const recentList = document.getElementById("recent-list");
    const pinnedSection = document.getElementById("pinned-section");

    pinnedList.replaceChildren();
    recentList.replaceChildren();

    const pinned = readingsCache.filter((r) => pinnedIds.includes(r.id));
    const others = readingsCache.filter((r) => !pinnedIds.includes(r.id));

    pinnedSection.hidden = pinned.length === 0;
    pinned.forEach((r) => pinnedList.append(renderChatItem(r, true)));

    if (others.length === 0 && pinned.length === 0) {
        recentList.append(el("p", { class: "muted" }, "Поки що немає гадань."));
    }
    others.forEach((r) => recentList.append(renderChatItem(r, false)));
}

async function openReading(id) {
    try {
        const detail = await authedFetch(`/tarot/readings/${id}`);
        showReading(detail);
    } catch (err) {
        alert(err.message);
    }
}

async function deleteReading(id) {
    if (!confirm("Видалити це гадання?")) return;
    try {
        await authedFetch(`/tarot/readings/${id}`, { method: "DELETE" });
        setPinnedIds(getPinnedIds().filter((x) => x !== id));
        if (window.currentReadingId === id) {
            window.currentReadingId = null;
            document.getElementById("tarot-root").replaceChildren();
            renderTopics();
        }
        refreshSidebar();
    } catch (err) {
        alert(err.message);
    }
}

document.getElementById("new-chat").addEventListener("click", () => {
    window.currentReadingId = null;
    savedBackground = "";
    selectedTopic = null;
    document.getElementById("tarot-root").replaceChildren();
    renderTopics();
    renderSidebar();
});

// Стартові дії після завантаження всіх скриптів
window.addEventListener("DOMContentLoaded", () => {
    renderTopics();
    refreshSidebar();
});