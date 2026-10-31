// Матриця долі: своя матриця, розрахунок, сумісність і збережені матриці з питаннями
function methodSelect(value = "Video") {
    const select = el("select", {}, ...Object.entries(METHOD_LABELS).map(([key, label]) => el("option", { value: key }, label)));
    select.value = value;
    return select;
}

function pointsBlock(points) {
    const groups = {};
    points.forEach((p) => {
        groups[p.group] = groups[p.group] || [];
        groups[p.group].push(p);
    });
    return el("div", { class: "matrix-groups" }, ...Object.entries(groups).map(([group, items]) =>
        el("section", { class: "card-block" },
            el("h3", {}, group),
            el("ul", { class: "points" }, ...items.map((p) =>
                el("li", {},
                    el("strong", {}, `${p.title}: ${p.value}`),
                    el("span", { class: "muted" }, ` · ${p.arcana}. ${p.arcanaMeaning}`)))))));
}

function timelineBlock(timeline, current) {
    return el("section", { class: "card-block" },
        el("h3", {}, "Періоди життя"),
        el("p", {}, `Зараз (${current.ageYears.toFixed(1)} р.): ${current.arcana}, значення ${current.value}. ${current.arcanaMeaning}`),
        el("ul", { class: "points" }, ...timeline.map((t) =>
            el("li", {},
                el("strong", {}, `${t.fromAge}–${t.toAge} р.: ${t.arcana} (${t.value})`),
                el("span", { class: "muted" }, ` ${t.line}`)))));
}

function matrixResultView(data, saveFor) {
    const nodes = [
        el("p", { class: "hint" }, `Спосіб: ${METHOD_LABELS[data.method] || data.method}`),
        pointsBlock(data.points),
        timelineBlock(data.timeline, data.current),
        el("section", { class: "card-block" }, el("h3", {}, "Трактування"), el("p", {}, data.interpretation))
    ];

    if (saveFor) {
        nodes.push(el("button", { type: "button", class: "primary", onclick: async () => {
            const label = prompt("Назва матриці (необов'язково)");
            if (label === null) return;
            try {
                const saved = await api("/matrix/saved", {
                    method: "POST",
                    body: { birthDate: saveFor.birthDate, method: saveFor.method, label: label.trim() || null }
                });
                go("matrix", saved.id);
            } catch (err) {
                alert(err.message);
            }
        } }, "Зберегти матрицю"));
    }

    return el("div", { class: "matrix-result" }, ...nodes);
}

function compatibilityView(d) {
    return el("div", { class: "matrix-result" },
        el("p", { class: "hint" }, `Спосіб: ${METHOD_LABELS[d.method] || d.method}`),
        el("section", { class: "card-block" },
            el("h3", {}, "Точки сумісності"),
            el("ul", { class: "points" }, ...d.points.map((p) =>
                el("li", {},
                    el("strong", {}, `${p.title}: ${p.value}`),
                    el("span", { class: "muted" }, ` · ${p.arcana}. ${p.arcanaMeaning}`))))),
        el("section", { class: "card-block" }, el("h3", {}, "Трактування"), el("p", {}, d.interpretation)));
}

async function renderMatrix(container, id) {
    if (id) return renderSavedMatrix(container, id);
    return renderMatrixHome(container);
}

async function renderMatrixHome(container) {
    const page = el("div", { class: "page" }, el("h2", {}, "Матриця долі"));
    container.replaceChildren(page);

    let mine = null;
    let mineError = null;
    try {
        mine = await api("/matrix/me");
    } catch (err) {
        mineError = err;
    }

    if (mine) {
        page.append(el("section", { class: "card-block" },
            el("h3", {}, "Моя матриця"),
            el("p", { class: "hint" }, "Розрахована за датою народження з вашого профілю."),
            matrixResultView(mine, null)));
    } else {
        page.append(muted("Щоб бачити свою матрицю тут, вкажіть дату народження в профілі." +
            (mineError ? " (" + mineError.message + ")" : "")));
    }

    const birth = el("input", { type: "date", required: true });
    const method = methodSelect();
    const calcError = el("p", { class: "error" });
    const calcResult = el("div");
    page.append(el("section", { class: "card-block" },
        el("h3", {}, "Розрахувати матрицю"),
        el("form", { onsubmit: async (e) => {
            e.preventDefault();
            calcError.textContent = "";
            calcResult.replaceChildren(muted("Рахуємо..."));
            try {
                const data = await api("/matrix/calculate", { method: "POST", body: { birthDate: birth.value, method: method.value } });
                calcResult.replaceChildren(matrixResultView(data, { birthDate: birth.value, method: method.value }));
            } catch (err) {
                calcResult.replaceChildren();
                calcError.textContent = err.message;
            }
        } },
            el("label", {}, "Дата народження", birth),
            el("label", {}, "Спосіб", method),
            calcError,
            el("button", { type: "submit", class: "primary" }, "Розрахувати")),
        calcResult));

    const first = el("input", { type: "date", required: true });
    const second = el("input", { type: "date", required: true });
    const compMethod = methodSelect();
    const compError = el("p", { class: "error" });
    const compResult = el("div");
    page.append(el("section", { class: "card-block" },
        el("h3", {}, "Сумісність двох дат"),
        el("form", { onsubmit: async (e) => {
            e.preventDefault();
            compError.textContent = "";
            try {
                const d = await api("/matrix/compatibility", {
                    method: "POST",
                    body: { firstBirthDate: first.value, secondBirthDate: second.value, method: compMethod.value }
                });
                compResult.replaceChildren(compatibilityView(d));
            } catch (err) {
                compError.textContent = err.message;
            }
        } },
            el("label", {}, "Перша дата", first),
            el("label", {}, "Друга дата", second),
            el("label", {}, "Спосіб", compMethod),
            compError,
            el("button", { type: "submit", class: "primary" }, "Порівняти")),
        compResult));

    try {
        const saved = await api("/matrix/saved");
        page.append(el("section", { class: "card-block" },
            el("h3", {}, "Збережені матриці"),
            saved.length
                ? el("div", { class: "list" }, ...saved.map((item) => savedRow(item, container)))
                : muted("Поки що немає збережених матриць.")));
    } catch (err) {
        page.append(notice(err.message));
    }
}

function savedRow(item, container) {
    return el("div", { class: "list-item" },
        el("button", { type: "button", class: "list-main", onclick: () => go("matrix", item.id) },
            el("strong", {}, item.label || `Матриця ${formatDateOnly(item.birthDate)}`),
            el("span", { class: "muted" }, `${formatDateOnly(item.birthDate)} · ${METHOD_LABELS[item.method] || item.method} · питань: ${item.questionsCount}`)),
        el("button", { type: "button", class: "icon-btn", onclick: async () => {
            if (!confirm("Видалити матрицю?")) return;
            try {
                await api(`/matrix/saved/${item.id}`, { method: "DELETE" });
                renderMatrixHome(container);
            } catch (err) {
                alert(err.message);
            }
        } }, "×"));
}

async function renderSavedMatrix(container, id) {
    const [detail, zones] = await Promise.all([
        api(`/matrix/saved/${id}`),
        api(`/matrix/saved/${id}/zones`)
    ]);

    container.replaceChildren(el("div", { class: "page" },
        el("button", { type: "button", class: "ghost", onclick: () => go("matrix") }, "← До матриць"),
        el("h2", {}, detail.label || `Матриця ${formatDateOnly(detail.birthDate)}`),
        el("p", { class: "hint" }, `${METHOD_LABELS[detail.method] || detail.method} · створено ${formatDateTime(detail.createdAt)}`),
        pointsBlock(detail.points),
        timelineBlock(detail.timeline, detail.current),
        el("section", { class: "card-block" },
            el("h3", {}, "Зони"),
            ...zones.map((z) => el("div", { class: "zone" },
                el("strong", {}, z.title),
                el("span", { class: "muted" }, z.description),
                el("p", {}, z.text)))),
        questionBlock(container, detail, zones)));
}

function questionBlock(container, detail, zones) {
    const zoneSelect = el("select", {},
        el("option", { value: "" }, "Загальне питання"),
        ...zones.map((z) => el("option", { value: z.zoneKey }, z.title)));
    const input = el("textarea", { rows: "3", maxlength: "500", placeholder: "Ваше питання до матриці..." });
    const errorEl = el("p", { class: "error" });
    const send = el("button", { type: "submit", class: "primary" }, "Запитати");
    const log = el("div", { class: "chat-messages" },
        ...detail.messages.map((m) => messageBubble(
            m.role === "User" ? "user" : "assistant",
            m.role === "User" ? "Ви" : "Аналітик",
            m.text)));

    return el("section", { class: "card-block" },
        el("h3", {}, "Питання до матриці"),
        log,
        el("form", { onsubmit: async (e) => {
            e.preventDefault();
            const question = input.value.trim();
            if (question.length < 3) {
                errorEl.textContent = "Питання має бути щонайменше 3 символи.";
                return;
            }
            send.disabled = true;
            send.textContent = "Думаємо...";
            try {
                await api(`/matrix/saved/${detail.id}/questions`, {
                    method: "POST",
                    body: { question: question, zoneKey: zoneSelect.value || null }
                });
                renderSavedMatrix(container, detail.id);
            } catch (err) {
                errorEl.textContent = err.message;
                send.disabled = false;
                send.textContent = "Запитати";
            }
        } }, zoneSelect, input, errorEl, send));
}
