// Оболонка застосунку: верхня панель, нижня панель з іконками, сайдбар з історією, роутер
const ROUTES = {
    tarot:         { roles: ["Client"], render: renderTarot },
    matrix:        { roles: ["Client"], render: renderMatrix },
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
        ["profile", "user", "Профіль"]
    ],
    Consultant: [
        ["notifications", "bell", "Сповіщення"],
        ["profile", "user", "Профіль"]
    ]
};

const HOME = { Client: "tarot", Consultant: "consultations" };

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

    highlight(target);
    if (role === "Client") refreshHistory();

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

function init() {
    const role = currentRole();
    const isClient = role === "Client";

    document.getElementById("sidebar").hidden = !isClient;
    document.getElementById("new-chat").addEventListener("click", () => {
        if (location.hash === "#tarot") route();
        else go("tarot");
    });

    buildNav();
    window.addEventListener("hashchange", route);
    route();
}

if (isLoggedIn() && getSessionUser() && HOME[currentRole()]) {
    init();
} else {
    logout();
}
