// Робочий графік консультанта: інтервали за днями тижня
const DAYS = Object.keys(DAY_LABELS);
const TIME_RE = /^([01]\d|2[0-3]):[0-5]\d$|^24:00$/;

async function renderSchedule(container) {
    const s = await api("/consultants/me/schedule");

    const tz = el("input", { type: "text", value: s.timezone || "", placeholder: "Europe/Kyiv" });
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
            el("button", { type: "button", class: "icon-btn", onclick: () => line.remove() }, "×"));
        rows.append(line);
    }

    s.intervals.forEach((i) => addRow(i));

    container.replaceChildren(el("div", { class: "page" },
        el("h2", {}, "Мій графік"),
        el("p", { class: "hint" }, "Вкажіть робочі інтервали. Клієнти бачать вільні слоти саме за цим графіком."),
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
            el("button", { type: "submit", class: "primary" }, "Зберегти графік"))));
}
