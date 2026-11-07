// Адреса бекенду (профіль http з launchSettings.json)
const API_BASE = (location.hostname === 'localhost' || location.hostname === '127.0.0.1')
    ? 'http://localhost:52639/api'
    : 'https://fatumback.onrender.com/api';
const API_ORIGIN = API_BASE.slice(0, -4);

const TOKEN_KEY = "taro_token";
const USER_KEY = "taro_user";

const TOPIC_TITLES = {
    Love: "Кохання", Career: "Кар'єра", Money: "Фінанси", Future: "Майбутнє",
    Family: "Сім'я і друзі", Study: "Навчання", Health: "Здоров'я",
    Creativity: "Творчість", Travel: "Подорож і переїзд", Decision: "Важливе рішення"
};

const STATUS_LABELS = {
    Pending: "Очікує", Confirmed: "Підтверджено", Declined: "Відхилено",
    CancelledByClient: "Скасовано клієнтом", CancelledByConsultant: "Скасовано консультантом",
    Completed: "Завершено"
};

const METHOD_LABELS = { Video: "Відео", Document: "Документ" };

const DAY_LABELS = {
    Monday: "Понеділок", Tuesday: "Вівторок", Wednesday: "Середа", Thursday: "Четвер",
    Friday: "П'ятниця", Saturday: "Субота", Sunday: "Неділя"
};

// Створення елементів. Текст іде через textContent, тож HTML з сервера не виконується.
function el(tag, attrs = {}, ...children) {
    const node = document.createElement(tag);
    for (const [key, value] of Object.entries(attrs)) {
        if (value === undefined || value === null || value === false) continue;
        if (key.startsWith("on")) node.addEventListener(key.slice(2), value);
        else if (key === "class") node.className = value;
        else if (key === "value" || key === "checked" || key === "disabled") node[key] = value;
        else node.setAttribute(key, value === true ? "" : value);
    }
    children.flat().forEach((child) => {
        if (child === undefined || child === null || child === false) return;
        node.append(child instanceof Node ? child : document.createTextNode(String(child)));
    });
    return node;
}

// Запит для сторінок входу (без токена)
async function postJson(path, body) {
    let res;
    try {
        res = await fetch(API_BASE + path, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body)
        });
    } catch (e) {
        throw new Error("Немає зв'язку з сервером. Перевірте, чи запущено API.");
    }

    let data = null;
    try { data = await res.json(); } catch (e) { /* відповідь без тіла */ }

    if (!res.ok) {
        throw new Error((data && data.error) || "Сталася помилка. Спробуйте ще раз.");
    }
    return data;
}

// Запит з токеном. Підтримує JSON (body) і файли (form)
async function api(path, options = {}) {
    const token = localStorage.getItem(TOKEN_KEY);
    const headers = {};
    if (token) headers.Authorization = "Bearer " + token;

    let body;
    if (options.form) {
        body = options.form;
    } else if (options.body !== undefined) {
        headers["Content-Type"] = "application/json";
        body = JSON.stringify(options.body);
    }

    let res;
    try {
        res = await fetch(API_BASE + path, { method: options.method || "GET", headers, body });
    } catch (e) {
        throw new Error("Немає зв'язку з сервером.");
    }

    if (res.status === 401 && token) {
        logout();
        throw new Error("Сесія закінчилась. Увійдіть знову.");
    }

    let data = null;
    try { data = await res.json(); } catch (e) { /* відповідь без тіла, наприклад 204 */ }

    if (!res.ok) throw new Error((data && data.error) || `Помилка сервера (${res.status}). Подивіться консоль бекенду.`);
    return data;
}

function saveSession(authResponse) {
    localStorage.setItem(TOKEN_KEY, authResponse.token);
    localStorage.setItem(USER_KEY, JSON.stringify({
        userId: authResponse.userId,
        email: authResponse.email,
        role: authResponse.role
    }));
}

function getSessionUser() {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? JSON.parse(raw) : null;
}

function isLoggedIn() {
    return !!localStorage.getItem(TOKEN_KEY);
}

function currentRole() {
    const user = getSessionUser();
    return user ? user.role : null;
}

function currentUserId() {
    const user = getSessionUser();
    return user ? user.userId : null;
}

function logout() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    window.location.href = "login.html";
}

function oauthLoginUrl(provider, role, birthDate) {
    const params = new URLSearchParams({ role: role });
    if (birthDate) params.set("birthDate", birthDate);
    return `${API_BASE}/auth/external/login/${provider}?${params.toString()}`;
}

function assetUrl(path) {
    if (!path) return null;
    return path.startsWith("http") ? path : API_ORIGIN + path;
}

function formatDateOnly(value) {
    if (!value) return "—";
    const [y, m, d] = value.slice(0, 10).split("-");
    return `${d}.${m}.${y}`;
}

function formatDateTime(value) {
    return value ? new Date(value).toLocaleString("uk-UA", { dateStyle: "medium", timeStyle: "short" }) : "—";
}

function initials(name) {
    return (name || "?").trim().charAt(0).toUpperCase();
}

function stars(rating) {
    return "★".repeat(rating) + "☆".repeat(5 - rating);
}

function ratingText(avg, count) {
    return count ? `★ ${avg != null ? avg.toFixed(1) : "—"} (${count})` : "Без відгуків";
}

function notice(text) {
    return el("p", { class: "error" }, text);
}

function muted(text) {
    return el("p", { class: "muted" }, text);
}

function statusBadge(status) {
    return el("span", { class: "badge status-" + status.toLowerCase() }, STATUS_LABELS[status] || status);
}

function messageBubble(kind, author, text) {
    return el("div", { class: "msg " + kind }, el("strong", {}, author), el("span", {}, text));
}

// Іконки (статичні SVG, без користувацького вмісту)
const ICONS = {
    tarot: '<path d="M12 3l1.8 4.7L18.5 9.5l-4.7 1.8L12 16l-1.8-4.7L5.5 9.5l4.7-1.8z"/><path d="M19 16l.7 1.8 1.8.7-1.8.7L19 21l-.7-1.8-1.8-.7 1.8-.7z"/>',
    matrix: '<rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><circle cx="17.5" cy="17.5" r="3.5"/>',
    users: '<circle cx="9" cy="8" r="4"/><path d="M2 21a7 7 0 0 1 14 0"/><path d="M16 4.5a4 4 0 0 1 0 7"/><path d="M22 21a6 6 0 0 0-4-5.6"/>',
    calendar: '<rect x="3" y="4" width="18" height="17" rx="2"/><path d="M3 9h18M8 2v4M16 2v4"/>',
    chat: '<path d="M21 12a8 8 0 0 1-11.6 7.1L4 20.5l1.4-4.6A8 8 0 1 1 21 12z"/>',
    clock: '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
    star: '<path d="M12 3l2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1-4.4-4.3 6.1-.9z"/>',
    bell: '<path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.7 21a2 2 0 0 1-3.4 0"/>',
    heart: '<path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1-1.1a5.5 5.5 0 0 0-7.8 7.8l1 1.1L12 21l7.8-7.8 1-1.1a5.5 5.5 0 0 0 0-7.5z"/>',
    user: '<circle cx="12" cy="8" r="4"/><path d="M4 21a8 8 0 0 1 16 0"/>',
    home: '<path d="M3 11l9-8 9 8"/><path d="M5 10v10h14V10"/>',
    pin: '<path d="M9 3h6l-1 6 3 3v2H7v-2l3-3z"/><path d="M12 17v5"/>',
    pinFilled: '<path d="M9 3h6l-1 6 3 3v2H7v-2l3-3z" fill="currentColor"/><path d="M12 17v5"/>',
    send: '<path d="M22 2L11 13"/><path d="M22 2l-7 20-4-9-9-4z"/>',
    edit: '<path d="M12 20h9"/><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4z"/>',
    logout: '<path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><path d="M16 17l5-5-5-5M21 12H9"/>'
};

function icon(name) {
    const span = document.createElement("span");
    span.className = "icon";
    span.innerHTML = `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${ICONS[name] || ""}</svg>`;
    return span;
}


// Анімація завантаження в стилі застосунку
function loader() {
    return el("div", { class: "loader", role: "status", "aria-label": "Завантаження" },
        el("span"), el("span"), el("span"));
}

// Розділяє дату на рядок yyyy-MM-dd за локальним часом
function ymd(d) {
    const pad = (n) => String(n).padStart(2, "0");
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}
