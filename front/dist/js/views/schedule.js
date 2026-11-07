// Робочий кабінет таролога: тижневий графік, звіт за період, історія графіків, консультації
const DAYS = Object.keys(DAY_LABELS);
const TIME_RE = /^([01]\d|2[0-3]):[0-5]\d$|^24:00$/;

function periodRange(kind) {
    const now = new Date();
    if (kind === "week") {
        const offset = (now.getDay() + 6) % 7;
        const start = new Date(now.getFullYear(), now.getMonth(), now.getDate() - offset);
        const end = new Date(now.getFullYear(), now.getMonth(), now.getDate() - offset + 6);
        return [ymd(start), ymd(end)];
    }
    const start = new Date(now.getFullYear(), now.getMonth(), 1);
    const end = new Date(now.getFullYear(), now.getMonth() + 1, 0);
    return [ymd(start), ymd(end)];
}

function statCell(value, label) {
    return el("div", { class: "stat" }, el("strong", {}, value), el("span", { class: "muted" }, label));
}

async function renderSchedule(container) {
    const [schedule, history] = await Promise.all([
        api("/consultants/me/schedule"),
        safe(api("/consultants/me/schedule/history"), [])
    ]);

    container.replaceChildren(el("div", { class: "page wide" },
        el("h2", {}, "Графік і робота"),
        editorCard(schedule),
        reportCard(),
        historyCard(history),
        consultationsCard()));
}

function editorCard(schedule) {
    const tz = el("input", { type: "text", value: schedule.timezone || "", placeholder: "Europe/Kyiv" });
    const rows = el("div", { class: "schedule-rows" });
    const errorEl = el("p", { class: "error" });
    const okEl = el("p", { class: "ok" });

    function addRow(interval) {
        const row = interval || { day: "Monday", start: "10:00", end: "18:00" };
        const day = el("select", {}, ...DAYS.map((d) => el("option", { value: d }, DAY_LABELS[d])));
        day.value = row.day;
        const start = el("input", { type: "text", value: row.start, placeholder: "09:00" });
        const end = el("input", { type: "text", value: row.end, placeholder: "18:00 (24:00 = кінець доби)" });
        const line = el("div", { class: "schedule-row" }, day, start, end,
            el("button", { type: "button", class: "icon-btn", onclick: () => line.remove(), "aria-label": "Прибрати" }, "×"));
        rows.append(line);
    }
    schedule.intervals.forEach((i) => addRow(i));

    return el("section", { class: "card-block" },
        el("h3", {}, "Тижневий графік"),
        el("p", { class: "hint" }, "Клієнти бачать вільні слоти за цим графіком. Зміни діють з моменту збереження."),
        el("form", { onsubmit: async (e) => {
            e.preventDefault();
            errorEl.textContent = "";
            okEl.textContent = "";
            const intervals = [...rows.children].map((line) => {
                const [day, start, end] = line.querySelectorAll("select, input");
                return { day: day.value, start: start.value.trim(), end: end.value.trim() };
            });
            if (intervals.some((i) => !TIME_RE.test(i.start) || !TIME_RE.test(i.end))) {
                errorEl.textContent = "Час має бути у форматі ГГ:ХХ, наприклад 09:00.";
                return;
            }
            try {
                await api("/consultants/me/schedule", { method: "PUT", body: { timezone: tz.value.trim(), intervals: intervals } });
                okEl.textContent = "Графік збережено.";
            } catch (err) {
                errorEl.textContent = err.message;
            }
        } },
            el("label", {}, "Таймзона", tz),
            rows,
            el("button", { type: "button", class: "ghost", onclick: () => addRow() }, "+ Додати інтервал"),
            errorEl,
            okEl,
            el("button", { type: "submit", class: "primary" }, "Зберегти графік")));
}

function reportCard() {
    const from = el("input", { type: "date" });
    const to = el("input", { type: "date" });
    const out = el("div", { class: "stats" });

    async function load() {
        if (!from.value || !to.value) return;
        out.replaceChildren(loader());
        try {
            const s = await api(`/consultants/me/work-summary?from=${from.value}&to=${to.value}`);
            out.replaceChildren(
                statCell(`${s.availableHours} год`, "доступно"),
                statCell(`${s.bookedHours} год`, "заброньовано"),
                statCell(`${s.completedHours} год`, "проведено"),
                statCell(String(s.completedCount), "завершено"),
                statCell(String(s.confirmedCount), "підтверджено"),
                statCell(String(s.cancelledCount + s.declinedCount), "скасовано або відхилено"));
        } catch (err) {
            out.replaceChildren(notice(err.message));
        }
    }

    const setRange = (kind) => {
        const [a, b] = periodRange(kind);
        from.value = a;
        to.value = b;
        load();
    };
    const [a, b] = periodRange("month");
    from.value = a;
    to.value = b;

    setTimeout(load, 0);
    return el("section", { class: "card-block" },
        el("h3", {}, "Звіт за період"),
        el("div", { class: "row" },
            el("button", { type: "button", class: "ghost", onclick: () => setRange("week") }, "Цей тиждень"),
            el("button", { type: "button", class: "ghost", onclick: () => setRange("month") }, "Цей місяць")),
        el("form", { class: "filters", onsubmit: (e) => { e.preventDefault(); load(); } },
            el("label", {}, "Від", from),
            el("label", {}, "До", to),
            el("button", { type: "submit", class: "primary" }, "Показати")),
        out);
}

function historyCard(history) {
    return el("section", { class: "card-block" },
        el("h3", {}, "Історія графіків"),
        history.length
            ? el("div", { class: "list" }, ...history.map((h) => el("div", { class: "list-item" },
                el("div", { class: "list-main" },
                    el("strong", {}, `${formatDateOnly(h.validFrom)} – ${h.validTo ? formatDateOnly(h.validTo) : "зараз"}`),
                    el("span", { class: "muted" }, h.timezone || "Без таймзони"),
                    el("span", {}, h.intervals.length
                        ? h.intervals.map((i) => `${DAY_LABELS[i.day] || i.day} ${i.start}–${i.end}`).join(" · ")
                        : "Без інтервалів")))))
            : muted("Історії ще немає. Вона з'явиться після зміни графіка."));
}

function consultationsCard() {
    const [a, b] = periodRange("month");
    const from = el("input", { type: "date", value: a });
    const to = el("input", { type: "date", value: b });
    const status = el("select", {}, el("option", { value: "" }, "Усі статуси"),
        ...Object.entries(STATUS_LABELS).map(([key, label]) => el("option", { value: key }, label)));
    const list = el("div", { class: "list" });

    async function load() {
        list.replaceChildren(loader());
        const params = new URLSearchParams();
        if (from.value) params.set("from", from.value);
        if (to.value) params.set("to", to.value);
        if (status.value) params.set("status", status.value);
        try {
            const items = await api(`/consultants/me/consultations?${params.toString()}`);
            list.replaceChildren(...(items.length
                ? items.map((c) => el("div", { class: "list-item" },
                    el("div", { class: "list-main" },
                        el("strong", {}, c.clientName),
                        el("span", { class: "muted" }, `${formatDateTime(c.startUtc)} · ${c.durationMinutes} хв${c.topic ? " · " + (TOPIC_TITLES[c.topic] || c.topic) : ""}`),
                        c.meetingUrl ? el("a", { href: c.meetingUrl, target: "_blank", rel: "noopener" }, "Посилання на зустріч") : null),
                    statusBadge(c.status)))
                : [muted("За цей період консультацій немає.")]));
        } catch (err) {
            list.replaceChildren(notice(err.message));
        }
    }

    setTimeout(load, 0);
    return el("section", { class: "card-block" },
        el("h3", {}, "Консультації"),
        el("form", { class: "filters", onsubmit: (e) => { e.preventDefault(); load(); } },
            el("label", {}, "Від", from),
            el("label", {}, "До", to),
            el("label", {}, "Статус", status),
            el("button", { type: "submit", class: "primary" }, "Показати")),
        list);
}
