// Кабінети: консультанта і клієнта, спільний стиль
const WEEK_DAYS = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
const WEEK_SHORT = ["Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Нд"];
const CLOSED_STATUSES = ["Declined", "CancelledByClient", "CancelledByConsultant"];

function safe(promise, fallback) {
    return promise.catch(() => fallback);
}

function byStart(a, b) {
    return new Date(a.startUtc) - new Date(b.startUtc);
}

function dmy(date) {
    return `${String(date.getDate()).padStart(2, "0")}.${String(date.getMonth() + 1).padStart(2, "0")}.${date.getFullYear()}`;
}

function firstName(name) {
    return (name || "").split(" ")[0];
}

function avatarNode(profile, cls) {
    return el("div", { class: cls },
        profile.photoUrl ? el("img", { src: assetUrl(profile.photoUrl), alt: "" }) : initials(profile.displayName));
}

function cabHead(eyebrow, title, subtitle, actions) {
    return el("header", { class: "cab-head" },
        el("div", {},
            el("span", { class: "cab-eyebrow" }, eyebrow),
            el("h1", {}, title),
            el("p", { class: "cab-sub" }, subtitle)),
        el("div", { class: "cab-actions" }, ...actions));
}

function cabStat(label, value, sub) {
    return el("div", { class: "cab-stat" },
        el("span", { class: "cab-eyebrow" }, label),
        el("strong", {}, value),
        el("small", {}, sub));
}

function cabCard(title, action, ...children) {
    return el("section", { class: "cab-card" },
        el("div", { class: "cab-card-head" }, el("h3", {}, title), action || null),
        ...children);
}

function cabButton(label, onclick, variant = "") {
    return el("button", { type: "button", class: "cab-btn " + variant, onclick: onclick }, label);
}

function cabRow(main, right, onclick) {
    return el("div", { class: "cab-row" + (onclick ? " clickable" : ""), onclick: onclick },
        el("div", { class: "cab-row-main" }, ...main),
        right || null);
}

async function renderCabinet(container) {
    if (profileEditing) {
        const p = await api("/profile/me");
        container.replaceChildren(profileEditView(p, container));
        return;
    }
    if (currentRole() === "Consultant") return renderConsultantCabinet(container);
    return renderClientCabinet(container);
}

// Кабінет консультанта
async function renderConsultantCabinet(container) {
    const profile = await api("/profile/me");
    const k = profile.consultant;
    const [consultations, schedule, stats, reviews] = await Promise.all([
        safe(api("/consultations"), []),
        safe(api("/consultants/me/schedule"), { timezone: "", intervals: [] }),
        safe(api(`/consultants/${profile.id}`), null),
        safe(api(`/reviews/consultant/${profile.id}?page=1&pageSize=50`), { items: [], total: 0 })
    ]);
    const reload = () => renderConsultantCabinet(container);
    const now = new Date();
    const monthStart = new Date(now.getFullYear(), now.getMonth(), 1);
    const inMonth = (c) => new Date(c.startUtc) >= monthStart;

    const pending = consultations.filter((c) => c.status === "Pending").sort(byStart);
    const confirmed = consultations.filter((c) => c.status === "Confirmed" && new Date(c.startUtc) > now).sort(byStart);
    const completed = consultations.filter((c) => c.status === "Completed").sort((a, b) => byStart(b, a));
    const monthCount = consultations.filter((c) => inMonth(c) && !CLOSED_STATUSES.includes(c.status)).length;
    const completedMonth = completed.filter(inMonth).length;
    const ratingText = stats && stats.reviewsCount ? `${stats.ratingAverage.toFixed(1)} / 5` : "—";

    // Тиждень: понеділок — початок
    const todayIdx = (now.getDay() + 6) % 7;
    const weekStart = new Date(now.getFullYear(), now.getMonth(), now.getDate() - todayIdx);
    const weekEnd = new Date(now.getFullYear(), now.getMonth(), now.getDate() - todayIdx + 6);
    let selected = todayIdx;
    const weekBox = el("div", { class: "cab-week" });
    const slotsBox = el("div", { class: "cab-day-slots" });

    function paintWeek() {
        weekBox.replaceChildren(...WEEK_SHORT.map((short, i) => {
            const date = new Date(weekStart.getFullYear(), weekStart.getMonth(), weekStart.getDate() + i);
            const hasSlots = schedule.intervals.some((x) => x.day === WEEK_DAYS[i]);
            return el("button", {
                type: "button",
                class: "cab-day" + (i === selected ? " selected" : ""),
                onclick: () => { selected = i; paintWeek(); }
            },
                el("span", { class: "dow" }, short),
                el("span", { class: "num" }, String(date.getDate())),
                hasSlots ? el("span", { class: "dot" }) : null);
        }));

        const intervals = schedule.intervals.filter((x) => x.day === WEEK_DAYS[selected]);
        slotsBox.replaceChildren(...(intervals.length
            ? intervals.map((x) => el("div", { class: "cab-slot" },
                el("strong", {}, `${x.start} – ${x.end}`),
                el("span", {}, "Робочий інтервал")))
            : [muted("Вихідний: інтервалів немає.")]));
    }
    paintWeek();

    const scheduleCard = cabCard("Ваш робочий тиждень",
        cabButton("Змінити графік", () => go("schedule"), "cab-btn-ghost"),
        el("p", { class: "cab-sub" }, `${dmy(weekStart)} – ${dmy(weekEnd)}${schedule.timezone ? " · " + schedule.timezone : ""}`),
        weekBox,
        slotsBox);

    const pendingCard = cabCard(`Нові заявки${pending.length ? " · " + pending.length : ""}`, null,
        pending.length
            ? el("div", { class: "cab-list" }, ...pending.map((c) => requestRow(c, reload)))
            : muted("Нових заявок немає."));

    const nearest = confirmed[0];
    const nearestCard = cabCard("Найближча підтверджена", null,
        nearest
            ? cabRow([
                el("strong", {}, nearest.clientName),
                el("span", {}, `${formatDateTime(nearest.startUtc)} · ${nearest.durationMinutes} хв`),
                el("span", {}, nearest.question)
            ], nearest.conversationId ? cabButton("Чат", () => go("chats", nearest.conversationId), "cab-btn-ghost") : null)
            : muted("Підтверджених консультацій попереду немає."));

    const completedCard = cabCard("Проведені консультації", null,
        completed.length
            ? el("div", { class: "cab-list" }, ...completed.slice(0, 5).map((c) => cabRow([
                el("strong", {}, c.clientName),
                el("span", {}, formatDateTime(c.startUtc))
            ], el("span", { class: "cab-price" }, `${k.priceUah} ₴`))))
            : muted("Ще немає завершених консультацій."));

    const profileCard = cabCard("Мій профіль",
        cabButton("Редагувати", () => { profileEditing = true; go("profile"); }, "cab-btn-ghost"),
        el("div", { class: "cab-profile" },
            avatarNode(profile, "cab-avatar"),
            el("div", {},
                el("strong", {}, profile.displayName || profile.email),
                el("span", { class: "cab-muted" }, k.isAcceptingClients ? "Опубліковано" : "Приховано з каталогу"))),
        el("p", { class: "cab-muted" }, "Спеціалізації"),
        chips(k.specializations, (t) => TOPIC_TITLES[t] || t),
        el("div", { class: "cab-kv" }, el("span", {}, "Тривалість сесії"), el("strong", {}, `${k.sessionMinutes} хв`)),
        el("div", { class: "cab-kv" }, el("span", {}, "Ціна"), el("strong", {}, `${k.priceUah} ₴`)));

    const counts = [5, 4, 3, 2, 1].map((n) => reviews.items.filter((r) => r.rating === n).length);
    const maxCount = Math.max(1, ...counts);
    const latest = reviews.items[0];
    const reviewsCard = cabCard("Рейтинг і відгуки",
        cabButton("Усі відгуки", () => go("reviews"), "cab-btn-ghost"),
        el("div", { class: "cab-rating" },
            el("strong", { class: "cab-rating-big" }, ratingText.replace(" / 5", "")),
            el("span", { class: "cab-muted" }, `${reviews.total} відгуків`)),
        el("div", { class: "cab-bars" }, ...counts.map((cnt, i) => el("div", { class: "cab-bar" },
            el("span", {}, String(5 - i)),
            el("div", { class: "cab-track" }, el("i", { style: `width:${(cnt / maxCount) * 100}%` })),
            el("span", {}, String(cnt))))),
        latest
            ? el("div", { class: "cab-review" },
                el("strong", {}, `${latest.clientName} · ${stars(latest.rating)}`),
                el("p", {}, latest.text || "Без тексту"),
                latest.consultantReply ? null : cabButton("Відповісти", () => go("reviews"), "cab-btn-dark"))
            : muted("Відгуків поки немає."));

    const statRow = el("div", { class: "cab-stats" },
        cabStat("Консультацій цього місяця", String(monthCount), `${completedMonth} завершено`),
        cabStat("Дохід цього місяця", `${(completedMonth * k.priceUah).toLocaleString("uk-UA")} ₴`, `${k.priceUah} ₴ за сесію`),
        cabStat("Середній рейтинг", ratingText, `${reviews.total} відгуків`),
        cabStat("Нові заявки", String(pending.length), pending.length ? "Потребують відповіді" : "Усе опрацьовано"));

    container.replaceChildren(el("div", { class: "cabinet" },
        cabHead("Кабінет консультанта",
            `Вітаємо, ${firstName(profile.displayName) || "колего"}`,
            "Ваш графік, запити та відгуки у спокійному робочому ритмі.",
            [cabButton("Редагувати профіль", () => { profileEditing = true; go("profile"); }, "cab-btn-ghost")]),
        statRow,
        el("div", { class: "cab-grid" },
            el("div", { class: "cab-col" }, scheduleCard, pendingCard, nearestCard, completedCard),
            el("div", { class: "cab-col" }, profileCard, reviewsCard))));
}

function requestRow(c, reload) {
    const url = el("input", { type: "url", placeholder: "Посилання на зустріч" });
    return cabRow([
        el("strong", {}, c.clientName),
        el("span", {}, `${formatDateTime(c.startUtc)} · ${c.durationMinutes} хв${c.topic ? " · " + (TOPIC_TITLES[c.topic] || c.topic) : ""}`),
        el("span", {}, c.question)
    ], el("div", { class: "cab-row-actions" },
        url,
        cabButton("Підтвердити", () => act(reload, `/consultations/${c.id}/confirm`, { meetingUrl: url.value.trim() || null }), "cab-btn-dark"),
        cabButton("Відхилити", () => {
            const reason = prompt("Причина відмови (необов'язково)");
            if (reason === null) return;
            act(reload, `/consultations/${c.id}/decline`, { reason: reason.trim() || null });
        }, "cab-btn-ghost")));
}

// Кабінет клієнта
async function renderClientCabinet(container) {
    const profile = await api("/profile/me");
    const c = profile.client;
    const [readings, saved, consultations, notes] = await Promise.all([
        safe(api("/tarot/readings"), []),
        safe(api("/matrix/saved"), []),
        safe(api("/consultations"), []),
        safe(api("/notifications"), [])
    ]);
    const now = new Date();
    const upcoming = consultations
        .filter((x) => (x.status === "Pending" || x.status === "Confirmed") && new Date(x.startUtc) > now)
        .sort(byStart);
    const unread = notes.filter((n) => !n.isRead);

    const statRow = el("div", { class: "cab-stats" },
        cabStat("Гадань", String(readings.length), "усіх сеансів"),
        cabStat("Матриць", String(saved.length), "збережено"),
        cabStat("Консультацій", String(upcoming.length), "попереду"),
        cabStat("Сповіщень", String(unread.length), "непрочитаних"));

    const upcomingCard = cabCard("Найближчі консультації", cabButton("Усі", () => go("consultations"), "cab-btn-ghost"),
        upcoming.length
            ? el("div", { class: "cab-list" }, ...upcoming.slice(0, 3).map((x) => cabRow([
                el("strong", {}, x.consultantName),
                el("span", {}, formatDateTime(x.startUtc))
            ], el("div", { class: "cab-row-actions" },
                statusBadge(x.status),
                x.conversationId ? cabButton("Чат", () => go("chats", x.conversationId), "cab-btn-ghost") : null))))
            : el("div", { class: "cab-empty" },
                muted("Поки що немає запланованих консультацій."),
                cabButton("Знайти консультанта", () => go("catalog"), "cab-btn-dark")));

    const readingsCard = cabCard("Останні гадання", cabButton("Нове", () => go("tarot"), "cab-btn-ghost"),
        readings.length
            ? el("div", { class: "cab-list" }, ...readings.slice(0, 3).map((r) => cabRow([
                el("strong", {}, r.title),
                el("span", {}, r.question)
            ], el("span", { class: "cab-muted" }, formatDateOnly(r.createdAt)), () => go("tarot", r.id))))
            : muted("Ще немає гадань."));

    const savedCard = cabCard("Збережені матриці", cabButton("Нова", () => go("matrix"), "cab-btn-ghost"),
        saved.length
            ? el("div", { class: "cab-list" }, ...saved.slice(0, 3).map((m) => cabRow([
                el("strong", {}, m.label || `Матриця ${formatDateOnly(m.birthDate)}`),
                el("span", {}, `${METHOD_LABELS[m.method] || m.method} · питань: ${m.questionsCount}`)
            ], null, () => go("matrix", m.id))))
            : muted("Ще немає збережених матриць."));

    const profileCard = cabCard("Мій профіль",
        cabButton("Редагувати", () => { profileEditing = true; go("profile"); }, "cab-btn-ghost"),
        el("div", { class: "cab-profile" },
            avatarNode(profile, "cab-avatar"),
            el("div", {},
                el("strong", {}, profile.displayName || profile.email),
                el("span", { class: "cab-muted" }, profile.email))),
        el("div", { class: "cab-kv" }, el("span", {}, "Дата народження"),
            el("strong", {}, c && c.birthDate ? formatDateOnly(c.birthDate) : "—")),
        el("p", { class: "cab-muted" }, "Цікавлять теми"),
        chips(c ? c.interests : [], (t) => TOPIC_TITLES[t] || t));

    const notesCard = cabCard("Сповіщення", cabButton("Усі", () => go("notifications"), "cab-btn-ghost"),
        notes.length
            ? el("div", { class: "cab-list" }, ...notes.slice(0, 4).map((n) => cabRow([
                el("strong", {}, n.title),
                el("span", {}, n.body)
            ], n.isRead ? null : el("span", { class: "cab-dot" }))))
            : muted("Сповіщень поки немає."));

    container.replaceChildren(el("div", { class: "cabinet" },
        cabHead("Кабінет клієнта",
            `Вітаємо, ${firstName(profile.displayName) || "друже"}`,
            "Ваші гадання, матриці та консультації в одному місці.",
            [
                cabButton("Нове гадання", () => go("tarot"), "cab-btn-dark"),
                cabButton("Розрахувати матрицю", () => go("matrix"), "cab-btn-ghost"),
                cabButton("Консультанти", () => go("catalog"), "cab-btn-ghost")
            ]),
        statRow,
        el("div", { class: "cab-grid" },
            el("div", { class: "cab-col" }, upcomingCard, readingsCard, savedCard),
            el("div", { class: "cab-col" }, profileCard, notesCard))));
}
