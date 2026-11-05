// Оболонка застосунку: верхня панель, нижня панель з іконками, сайдбар з історією, роутер
const ROUTES = {
    cabinet:       { roles: ["Client", "Consultant"], render: renderCabinet },
    tarot:         { roles: ["Client", "Consultant"], render: renderTarot },
    matrix:        { roles: ["Client", "Consultant"], render: renderMatrix },
    catalog:       { roles: ["Client"], render: renderCatalog },
    consultations: { roles: ["Client", "Consultant"], render: renderConsultations },
    chats:         { roles: ["Client", "Consultant"], render: renderChats },
    favorites:     { roles: ["Client"], render: renderFavorites },
    schedule:      { roles: ["Consultant"], render: renderSchedule },
    reviews:       { roles: ["Consultant"], render: renderReviews },
    notifications: { roles: ["Client", "Consultant"], render: renderNotifications },
    profile:       { roles: ["Client", "Consultant"], render: renderProfile }
};

// Нижня панель: [маршрут, іконка, підпис]
const DOCK = {
    Client: [
        ["tarot", "tarot", "Гадання"],
        ["matrix", "matrix", "Матриця"],
        ["catalog", "users", "Консультанти"],
        ["consultations", "calendar", "Консультації"],
        ["chats", "chat", "Чати"]
    ],
    Consultant: [
        ["consultations", "calendar", "Заявки"],
        ["tarot", "tarot", "Гадання"],
        ["matrix", "matrix", "Матриця"],
        ["chats", "chat", "Чати"],
        ["schedule", "clock", "Графік"],
        ["reviews", "star", "Відгуки"]
    ]
};

// Верхня панель
const TOP = {
    Client: [
        ["notifications", "bell", "Сповіщення"],
        ["favorites", "heart", "Обране"],
        ["cabinet", "user", "Акаунт"]
    ],
    Consultant: [
        ["notifications", "bell", "Сповіщення"],
        ["cabinet", "user", "Акаунт"]
    ]
};

const HOME = { Client: "cabinet", Consultant: "cabinet" };

function go(key, param) {
    location.hash = param ? `${key}/${param}` : key;
}

function parseHash() {
    const [key = "", param = null] = location.hash.slice(1).split("/");
    return { key, param };
}

function navButton([key, iconName, label]) {
    const children = [icon(iconName)];
    if (key === "notifications") {
        children.push(el("span", { class: "badge count", id: "notif-count", hidden: true }));
    }
    return el("button", {
        type: "button",
        class: "nav-btn",
        "data-route": key,
        title: label,
        "aria-label": label,
        onclick: () => go(key)
    }, ...children);
}

function buildNav() {
    const role = currentRole();
    document.getElementById("dock").replaceChildren(...DOCK[role].map(navButton));
    document.getElementById("top-actions").replaceChildren(...TOP[role].map(navButton));
}

function highlight(key) {
    document.querySelectorAll("[data-route]").forEach((btn) => {
        btn.classList.toggle("active", btn.dataset.route === key);
    });
}

async function refreshBadge() {
    const badge = document.getElementById("notif-count");
    if (!badge) return;
    try {
        const list = await api("/notifications");
        const unread = list.filter((n) => !n.isRead).length;
        badge.textContent = unread;
        badge.hidden = unread === 0;
    } catch (err) {
        badge.hidden = true;
    }
}

async function route() {
    const { key, param } = parseHash();
    const role = currentRole();
    const target = ROUTES[key] && ROUTES[key].roles.includes(role) ? key : HOME[role];

    if (target !== key) {
        location.hash = target;
        return;
    }

    if (target !== "profile") profileEditing = false;
    highlight(target);
    updateSidebar(target);

    const container = document.getElementById("view");
    container.replaceChildren(muted("Завантаження..."));
    try {
        await ROUTES[target].render(container, param);
    } catch (err) {
        console.error(err);
        container.replaceChildren(notice(err.message));
    }

    refreshBadge();
    document.getElementById("content").scrollTop = 0;
}

// Сайдбар показуємо лише для гадань і матриць, з відповідною історією
const SIDEBAR = {
    tarot: { title: "Історія гадань", newLabel: "+ Новий чат", newRoute: "tarot", refresh: () => refreshHistory() },
    matrix: { title: "Збережені матриці", newLabel: "+ Нова матриця", newRoute: "matrix", refresh: () => refreshMatrixSidebar() }
};

function updateSidebar(key) {
    const sidebar = document.getElementById("sidebar");
    const config = SIDEBAR[key];
    sidebar.hidden = !config;
    if (!config) return;
    document.getElementById("new-chat").textContent = config.newLabel;
    document.getElementById("history-title").textContent = config.title;
    document.getElementById("history-section").hidden = false;
    config.refresh();
}

function init() {
    const newChat = document.getElementById("new-chat");
    newChat.addEventListener("click", () => {
        const target = SIDEBAR[parseHash().key] ? SIDEBAR[parseHash().key].newRoute : "tarot";
        if (location.hash === "#" + target) route();
        else go(target);
    });

    buildNav();
    window.addEventListener("hashchange", route);
    route();
}

if (isLoggedIn() && getSessionUser() && currentRole() === "Admin") {
    location.replace("admin.html");
} else if (isLoggedIn() && getSessionUser() && HOME[currentRole()]) {
    init();
} else {
    logout();
}
