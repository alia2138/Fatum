// Адмін-панель: статистика, користувачі (блокування), модерація відгуків
const ROLE_LABELS = { Client: "Клієнт", Consultant: "Таролог", Admin: "Адмін" };
const PAGE_USERS = 20;

if (!isLoggedIn() || !getSessionUser()) {
    logout();
} else if (currentRole() !== "Admin") {
    window.location.replace("home.html");
}

function adminStat(label, value, sub) {
    return el("div", { class: "cab-stat" },
        el("span", { class: "cab-eyebrow" }, label),
        el("strong", {}, String(value)),
        el("small", {}, sub));
}

function adminCard(title, ...children) {
    return el("section", { class: "cab-card" },
        el("div", { class: "cab-card-head" }, el("h3", {}, title)),
        ...children);
}

function kvList(obj, labelOf) {
    return el("div", { class: "cab-list" }, ...Object.entries(obj || {}).map(([key, value]) =>
        el("div", { class: "cab-kv" },
            el("span", {}, labelOf ? labelOf(key) : key),
            el("strong", {}, String(value)))));
}

async function renderStats(host) {
    const s = await api("/admin/stats");
    const ai = s.ai || {};
    const aiBody = ai.supported
        ? kvList({ "Використано": ai.usage, "Ліміт": ai.limit, "Залишок": ai.remaining })
        : muted(ai.note || "Провайдер не віддає баланс через API.");

    host.replaceChildren(
        el("div", { class: "cab-stats" },
            adminStat("Користувачі", s.totalUsers, `заблоковано: ${s.blockedUsers}`),
            adminStat("Консультації", s.totalConsultations, "усього"),
            adminStat("Гадання", s.totalReadings, `AI-відповідей: ${s.aiReadingAnswers}`),
            adminStat("Матриці", s.totalMatrices, `AI-відповідей: ${s.aiMatrixAnswers}`)),
        el("div", { class: "cab-grid" },
            el("div", { class: "cab-col" },
                adminCard("Користувачі за роллю", kvList(s.usersByRole, (k) => ROLE_LABELS[k] || k)),
                adminCard("Консультації за статусом", kvList(s.consultationsByStatus, (k) => STATUS_LABELS[k] || k)),
                adminCard("Відгуки", kvList({ "Усього відгуків": s.totalReviews }))),
            el("div", { class: "cab-col" },
                adminCard("AI-баланс (" + (ai.provider || "—") + ")", aiBody),
                adminCard("Нові користувачі", el("div", { class: "cab-list" },
                    ...(s.recentUsers.length
                        ? s.recentUsers.map((u) => userRow(u))
                        : [muted("Немає користувачів.")]))))));
}

function userRow(u, onOpen) {
    return el("div", { class: "cab-row" + (onOpen ? " clickable" : ""), onclick: onOpen ? () => onOpen(u.id) : undefined },
        el("div", { class: "cab-row-main" },
            el("strong", {}, u.displayName || u.email),
            el("span", {}, `${u.email} · ${formatDateTime(u.createdAt)}`)),
        el("div", { class: "cab-row-actions" },
            el("span", { class: "badge" }, ROLE_LABELS[u.role] || u.role),
            u.isBlocked ? el("span", { class: "badge status-declined" }, "Заблоковано") : null));
}

async function renderUsers(host) {
    const state = { page: 1, role: "", search: "" };
    const roleSelect = el("select", {}, el("option", { value: "" }, "Усі ролі"),
        ...Object.keys(ROLE_LABELS).map((k) => el("option", { value: k }, ROLE_LABELS[k])));
    const searchInput = el("input", { type: "text", placeholder: "Пошук за email або іменем" });
    const list = el("div", { class: "cab-list" });
    const pager = el("div", { class: "cab-actions" });
    const detail = el("div", { class: "admin-detail" });

    async function openUser(id) {
        detail.replaceChildren(muted("Завантаження..."));
        try {
            const u = await api(`/admin/users/${id}`);
            const blockBtn = el("button", { type: "button", class: "cab-btn " + (u.isBlocked ? "cab-btn-ghost" : "cab-btn-dark"),
                onclick: async () => {
                    const text = u.isBlocked ? "Розблокувати користувача?" : "Заблокувати користувача? Він одразу втратить доступ.";
                    if (!confirm(text)) return;
                    try {
                        await api(`/admin/users/${id}/${u.isBlocked ? "unblock" : "block"}`, { method: "POST" });
                        openUser(id);
                        load();
                    } catch (err) {
                        alert(err.message);
                    }
                } }, u.isBlocked ? "Розблокувати" : "Заблокувати");

            detail.replaceChildren(adminCard(u.displayName || u.email,
                kvList({
                    "Email": u.email,
                    "Роль": ROLE_LABELS[u.role] || u.role,
                    "Створено": formatDateTime(u.createdAt),
                    "Профіль заповнено": u.isProfileComplete ? "так" : "ні",
                    "Гадань": u.readingsCount,
                    "Консультацій": u.consultationsCount,
                    "Матриць": u.matricesCount
                }),
                el("div", { class: "cab-actions" }, blockBtn)));
        } catch (err) {
            detail.replaceChildren(notice(err.message));
        }
    }

    async function load() {
        list.replaceChildren(muted("Завантаження..."));
        const params = new URLSearchParams({ page: state.page, pageSize: PAGE_USERS });
        if (state.role) params.set("role", state.role);
        if (state.search) params.set("search", state.search);
        try {
            const data = await api(`/admin/users?${params.toString()}`);
            list.replaceChildren(...(data.items.length
                ? data.items.map((u) => userRow(u, openUser))
                : [muted("Нікого не знайдено.")]));
            const pages = Math.max(1, Math.ceil(data.total / PAGE_USERS));
            pager.replaceChildren(
                el("button", { type: "button", class: "cab-btn cab-btn-ghost", disabled: state.page <= 1,
                    onclick: () => { state.page--; load(); } }, "← Назад"),
                el("span", { class: "cab-muted" }, `${state.page} з ${pages} · всього ${data.total}`),
                el("button", { type: "button", class: "cab-btn cab-btn-ghost", disabled: state.page >= pages,
                    onclick: () => { state.page++; load(); } }, "Далі →"));
        } catch (err) {
            list.replaceChildren(notice(err.message));
        }
    }

    host.replaceChildren(
        adminCard("Користувачі",
            el("form", { class: "admin-filters", onsubmit: (e) => {
                e.preventDefault();
                state.page = 1;
                state.role = roleSelect.value;
                state.search = searchInput.value.trim();
                load();
            } }, roleSelect, searchInput, el("button", { type: "submit", class: "cab-btn cab-btn-dark" }, "Знайти")),
            list,
            pager),
        detail);

    load();
}

async function renderModeration(host) {
    const consultantList = el("div", { class: "cab-list" });
    const reviewList = el("div", { class: "cab-list" }, muted("Оберіть таролога, щоб побачити відгуки."));

    async function loadReviews(consultant) {
        reviewList.replaceChildren(muted("Завантаження..."));
        try {
            const data = await api(`/reviews/consultant/${consultant.id}?page=1&pageSize=50`);
            reviewList.replaceChildren(...(data.items.length
                ? data.items.map((r) => el("div", { class: "cab-row" },
                    el("div", { class: "cab-row-main" },
                        el("strong", {}, `${r.clientName} · ${stars(r.rating)}`),
                        el("span", {}, r.text || "Без тексту"),
                        el("small", { class: "cab-muted" }, formatDateTime(r.createdAt))),
                    el("button", { type: "button", class: "cab-btn cab-btn-ghost", onclick: async () => {
                        if (!confirm("Видалити відгук?")) return;
                        try {
                            await api(`/admin/reviews/${r.id}`, { method: "DELETE" });
                            loadReviews(consultant);
                        } catch (err) {
                            alert(err.message);
                        }
                    } }, "Видалити")))
                : [muted("Відгуків немає.")]));
        } catch (err) {
            reviewList.replaceChildren(notice(err.message));
        }
    }

    try {
        const data = await api("/consultants?sortBy=name&desc=false&page=1&pageSize=50");
        consultantList.replaceChildren(...(data.items.length
            ? data.items.map((c) => el("div", { class: "cab-row clickable", onclick: () => loadReviews(c) },
                el("div", { class: "cab-row-main" },
                    el("strong", {}, c.displayName),
                    el("span", {}, `${ratingText(c.ratingAverage, c.reviewsCount)}`))))
            : [muted("Таро-консультантів немає.")]));
    } catch (err) {
        consultantList.replaceChildren(notice(err.message));
    }

    host.replaceChildren(el("div", { class: "cab-grid" },
        adminCard("Таро-консультанти", consultantList),
        adminCard("Відгуки", reviewList)));
}

const ADMIN_TABS = { stats: renderStats, users: renderUsers, moderation: renderModeration };

async function route() {
    const key = location.hash.slice(1) || "stats";
    const name = ADMIN_TABS[key] ? key : "stats";
    document.querySelectorAll("#admin-tabs button").forEach((b) => b.classList.toggle("active", b.dataset.tab === name));

    const view = document.getElementById("admin-view");
    const host = el("div", { class: "cabinet" }, muted("Завантаження..."));
    view.replaceChildren(host);
    try {
        await ADMIN_TABS[name](host);
    } catch (err) {
        host.replaceChildren(notice(err.message));
    }
}

document.querySelectorAll("#admin-tabs button").forEach((btn) => {
    btn.addEventListener("click", () => { location.hash = btn.dataset.tab; });
});
document.getElementById("logout").addEventListener("click", logout);
window.addEventListener("hashchange", route);
route();
