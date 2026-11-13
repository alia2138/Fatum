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

let matrixTab = "mine";

function svgEl(tag, attrs = {}, ...children) {
    const node = document.createElementNS("http://www.w3.org/2000/svg", tag);
    Object.entries(attrs).forEach(([key, value]) => node.setAttribute(key, String(value)));
    children.forEach((child) => {
        if (child === undefined || child === null) return;
        node.append(child instanceof Node ? child : document.createTextNode(String(child)));
    });
    return node;
}

// Розміщення точок за інструкцією: ромб основних точок (A, B, V, G), центр D,
// квадрат кутів (Heaven, Earth, E, MoneyEnd), родові точки на вертикалі,
// вікові призначення на горизонталі, канали на діагоналях. Значення — зсув від центру.
const MATRIX_LAYOUT = {
    A: [-165, 0], B: [0, -165], V: [165, 0], G: [0, 165], D: [0, 0],
    Heaven: [-115, -115], Earth: [115, -115], E: [-115, 115], MoneyEnd: [115, 115],
    Zh: [0, -55], Z: [0, -100], Spiritual: [0, -135],
    I: [0, 55], K: [0, 100],
    Male: [-125, 0], Personal: [-60, 0], Social: [60, 0], Female: [125, 0],
    Rel: [-60, 60],
    Money: [55, 55], MoneyMid: [85, 85]
};

const MATRIX_RADIUS = { "Основні": 20, "Канал": 15, "Канал (відео)": 15, "Призначення": 14, "Родові": 12 };

function matrixDiagram(points) {
    const size = 400;
    const c = size / 2;
    const svg = svgEl("svg", { viewBox: `0 0 ${size} ${size}`, class: "mx-diagram", role: "img", "aria-label": "Діаграма матриці" });
    const byKey = {};
    points.forEach((p) => { byKey[p.key] = p; });

    const pos = {};
    Object.entries(MATRIX_LAYOUT).forEach(([key, off]) => { pos[key] = [c + off[0], c + off[1]]; });

    const line = (a, b, cls) => {
        if (!a || !b) return;
        svg.append(svgEl("line", { x1: a[0], y1: a[1], x2: b[0], y2: b[1], class: cls }));
    };
    const polygon = (keys) => {
        svg.append(svgEl("polygon", { points: keys.map((k) => pos[k].join(",")).join(" "), class: "mx-frame" }));
    };

    svg.append(svgEl("circle", { cx: c, cy: c, r: 178, class: "mx-ring" }));
    polygon(["A", "B", "V", "G"]);
    polygon(["Heaven", "Earth", "MoneyEnd", "E"]);
    ["A", "B", "V", "G", "Heaven", "Earth", "E", "MoneyEnd"].forEach((k) => line(pos.D, pos[k], "mx-spoke"));
    line(pos.D, pos.MoneyEnd, "mx-channel");
    line(pos.D, pos.E, "mx-channel");

    // Кружечки на всіх позиціях; заповнені — з числом аркана і його назвою
    Object.keys(MATRIX_LAYOUT).forEach((key) => {
        const p = byKey[key];
        const [x, y] = pos[key];
        const isCenter = key === "D";
        const r = isCenter ? 26 : (p ? (MATRIX_RADIUS[p.group] || 13) : 12);

        if (!p) {
            svg.append(svgEl("g", { class: "mx-node empty" },
                svgEl("circle", { cx: x, cy: y, r: r })));
            return;
        }

        svg.append(svgEl("g", { class: "mx-node" + (isCenter ? " center" : "") },
            svgEl("title", {}, `${p.title}: ${p.value} ${p.arcana}`),
            svgEl("circle", { cx: x, cy: y, r: r }),
            svgEl("text", { x: x, y: y, "text-anchor": "middle", "dominant-baseline": "central" }, String(p.value)),
            ));
    });
    return svg;
}

function matrixCard(points, title) {
    return el("section", { class: "mx-card" },
        el("div", { class: "mx-card-head" },
            el("h3", {}, title),
            el("span", { class: "mx-pill" }, `${points.length} точок`)),
        matrixDiagram(points));
}

function interpretationCard(text) {
    return el("section", { class: "mx-card" },
        el("span", { class: "mx-tag" }, "AI-трактування"),
        el("p", { class: "mx-text" }, text),
        el("p", { class: "mx-note" }, "Трактування допомагає для самоусвідомлення і не є точним прогнозом."));
}

// Вікно з назвою замість системного prompt
function askName() {
    return new Promise((resolve) => {
        const input = el("input", { type: "text", maxlength: "60", placeholder: "Назва матриці" });
        let overlay;
        const close = (value) => { overlay.remove(); resolve(value); };
        overlay = el("div", { class: "mx-modal-overlay", onclick: (e) => { if (e.target === overlay) close(null); } },
            el("div", { class: "mx-modal" },
                el("h3", {}, "Зберегти матрицю"),
                el("p", { class: "hint" }, "Назва з'явиться в історії (необов'язково)."),
                input,
                el("div", { class: "mx-modal-actions" },
                    el("button", { type: "button", class: "ghost", onclick: () => close(null) }, "Скасувати"),
                    el("button", { type: "button", class: "primary", onclick: () => close(input.value.trim()) }, "Зберегти"))));
        document.body.append(overlay);
        input.focus();
    });
}

// Зберігає матрицю з тим самим способом (відео/документ), в якому її зроблено
async function saveMatrix(data) {
    const name = await askName();
    if (name === null) return null;
    return api("/matrix/saved", {
        method: "POST",
        body: { birthDate: String(data.birthDate).slice(0, 10), method: data.method, label: name || null }
    });
}

function saveRow(data) {
    return el("div", { class: "mx-actions" },
        el("button", { type: "button", class: "ghost", onclick: async () => {
            try {
                const saved = await saveMatrix(data);
                if (saved) go("matrix", saved.id);
            } catch (err) {
                alert(err.message);
            }
        } }, "Зберегти матрицю"));
}

function mxHeader(title, subtitle, back) {
    return el("div", { class: "mx-head" },
        el("div", {},
            el("span", { class: "mx-eyebrow" }, "Матриця долі / Персональний розрахунок"),
            el("h2", {}, title),
            subtitle ? el("p", { class: "hint" }, subtitle) : null),
        back ? el("button", { type: "button", class: "ghost", onclick: back }, "← Назад") : null);
}

// Трактування основних арканів: точки згруповані, біля кожної значення, аркан і його опис
function arcanaSection(points) {
    const groups = {};
    points.forEach((p) => {
        const group = p.group || "Сумісність";
        groups[group] = groups[group] || [];
        groups[group].push(p);
    });
    return el("section", { class: "mx-card" },
        el("h3", {}, "Трактування основних арканів"),
        ...Object.entries(groups).map(([group, items]) => el("div", { class: "mx-group" },
            el("h4", {}, group),
            ...items.map((p) => el("div", { class: "mx-arcana-row" },
                el("span", { class: "mx-num" }, String(p.value)),
                el("div", {},
                    el("strong", {}, `${p.title} · ${p.arcana}`),
                    el("p", { class: "mx-text" }, p.arcanaMeaning)))))));
}

// Питання до матриці: матриця зберігається при першому питанні
function mxBubble(m) {
    return messageBubble(m.role === "User" ? "user" : "assistant", m.role === "User" ? "Ви" : "Аналітик", m.text);
}

function questionSection(data, saved = null) {
    let savedId = saved ? saved.id : null;
    const log = el("div", { class: "chat-messages mx-log" }, ...(saved ? saved.messages.map(mxBubble) : []));
    const input = el("textarea", { rows: "3", maxlength: "500", placeholder: "Поставте питання до своєї матриці..." });
    const errorEl = el("p", { class: "error" });
    const send = el("button", { type: "submit", class: "send-btn", "aria-label": "Надіслати", title: "Надіслати" }, icon("send"));

    const form = el("form", { class: "chat-input mx-question-bar", onsubmit: async (e) => {
        e.preventDefault();
        const question = input.value.trim();
        if (question.length < 3) {
            errorEl.textContent = "Питання має бути щонайменше 3 символи.";
            return;
        }
        errorEl.textContent = "";
        send.disabled = true;
        try {
            if (!savedId) {
                const saved = await api("/matrix/saved", {
                    method: "POST",
                    body: { birthDate: String(data.birthDate).slice(0, 10), method: data.method, label: null }
                });
                savedId = saved.id;
            }
            const detail = await api(`/matrix/saved/${savedId}/questions`, { method: "POST", body: { question: question, zoneKey: null } });
            log.replaceChildren(...detail.messages.map((m) => messageBubble(
                m.role === "User" ? "user" : "assistant",
                m.role === "User" ? "Ви" : "Аналітик",
                m.text)));
            input.value = "";
        } catch (err) {
            errorEl.textContent = err.message;
        } finally {
            send.disabled = false;
        }
    } }, input, errorEl, send);

    return el("section", { class: "mx-card" },
        el("h3", {}, "Поставити питання"),
        log,
        form);
}

function showChoice(page) {
    page.replaceChildren(
        mxHeader("Оберіть тип розрахунку", "Матриця долі — про одну людину. Сумісність — про двох."),
        el("div", { class: "mx-choice" },
            el("button", { type: "button", class: "mx-choice-card", onclick: () => showSingle(page) },
                el("span", { class: "mx-tag" }, "На одного"),
                el("strong", {}, "Матриця долі"),
                el("span", { class: "mx-muted" }, "Розрахунок за датою народження: діаграма, трактування основних арканів і питання.")),
            el("button", { type: "button", class: "mx-choice-card", onclick: () => showCompat(page) },
                el("span", { class: "mx-tag" }, "Для двох"),
                el("strong", {}, "Матриця сумісності"),
                el("span", { class: "mx-muted" }, "Порівняння двох дат народження: та сама діаграма, трактування і опис точок."))));
}

function showSingle(page) {
    const dateInput = el("input", { type: "date", required: true });
    const method = { value: "Video" };
    const errorEl = el("p", { class: "error" });
    const out = el("div", { class: "mx-out" });

    page.replaceChildren(
        mxHeader("Матриця долі", "Вкажіть дату народження і спосіб.", () => showChoice(page)),
        el("form", { class: "mx-calc", onsubmit: async (e) => {
            e.preventDefault();
            errorEl.textContent = "";
            out.replaceChildren(loader());
            try {
                const data = await api("/matrix/calculate", { method: "POST", body: { birthDate: dateInput.value, method: method.value } });
                // Кожна матриця одразу зберігається в історію (з тим самим способом)
                const saved = await api("/matrix/saved", {
                    method: "POST",
                    body: { birthDate: String(data.birthDate).slice(0, 10), method: data.method, label: null }
                });
                refreshMatrixSidebar();
                out.replaceChildren(
                    matrixCard(data.points, "Ваша матриця"),
                    interpretationCard(data.interpretation),
                    arcanaSection(data.points),
                    questionSection(data, { id: saved.id, messages: saved.messages || [] }));
            } catch (err) {
                out.replaceChildren();
                errorEl.textContent = err.message;
            }
        } },
            el("label", {}, "Дата народження", dateInput),
            el("button", { type: "submit", class: "primary" }, "Розрахувати")),
        errorEl,
        out);
}

// Питання до матриці сумісності (без збереження, відповідь від трактувальника)
function compatQuestionSection(getDates) {
    const log = el("div", { class: "chat-messages mx-log" });
    const input = el("textarea", { rows: "3", maxlength: "500", placeholder: "Поставте питання про стосунки двох людей..." });
    const errorEl = el("p", { class: "error" });
    const send = el("button", { type: "submit", class: "send-btn", "aria-label": "Надіслати", title: "Надіслати" }, icon("send"));

    const form = el("form", { class: "chat-input mx-question-bar", onsubmit: async (e) => {
        e.preventDefault();
        const question = input.value.trim();
        if (question.length < 3) {
            errorEl.textContent = "Питання має бути щонайменше 3 символи.";
            return;
        }
        errorEl.textContent = "";
        send.disabled = true;
        try {
            const dates = getDates();
            const res = await api("/matrix/compatibility/questions", {
                method: "POST",
                body: { firstBirthDate: dates.first, secondBirthDate: dates.second, method: dates.method, question: question }
            });
            log.append(messageBubble("user", "Ви", question), messageBubble("assistant", "Аналітик", res.text));
            log.scrollTop = log.scrollHeight;
            input.value = "";
        } catch (err) {
            errorEl.textContent = err.message;
        } finally {
            send.disabled = false;
        }
    } }, input, errorEl, send);

    return el("section", { class: "mx-card" },
        el("h3", {}, "Поставити питання"),
        log,
        form);
}

function showCompat(page) {
    const first = el("input", { type: "date", required: true });
    const second = el("input", { type: "date", required: true });
    const method = { value: "Video" };
    const errorEl = el("p", { class: "error" });
    const out = el("div", { class: "mx-out" });

    page.replaceChildren(
        mxHeader("Матриця сумісності", "Порівняйте дві дати народження.", () => showChoice(page)),
        el("form", { class: "mx-calc", onsubmit: async (e) => {
            e.preventDefault();
            errorEl.textContent = "";
            out.replaceChildren(loader());
            try {
                const d = await api("/matrix/compatibility", {
                    method: "POST",
                    body: { firstBirthDate: first.value, secondBirthDate: second.value, method: method.value }
                });
                out.replaceChildren(
                    matrixCard(d.points, "Матриця сумісності"),
                    interpretationCard(d.interpretation),
                    arcanaSection(d.points),
                    compatQuestionSection(() => ({ first: first.value, second: second.value, method: method.value })));
            } catch (err) {
                out.replaceChildren();
                errorEl.textContent = err.message;
            }
        } },
            el("label", {}, "Перша дата", first),
            el("label", {}, "Друга дата", second),
            el("button", { type: "submit", class: "primary" }, "Порівняти")),
        errorEl,
        out);
}

async function renderMatrixHome(container) {
    const page = el("div", { class: "page mx-page" });
    container.replaceChildren(page);
    showChoice(page);
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
    const detail = await api(`/matrix/saved/${id}`);
    const title = detail.label || `Матриця ${formatDateOnly(detail.birthDate)}`;
    container.replaceChildren(el("div", { class: "page mx-page" },
        el("div", { class: "mx-head" },
            el("div", {},
                el("span", { class: "mx-eyebrow" }, `${METHOD_LABELS[detail.method] || detail.method} · збережено ${formatDateTime(detail.createdAt)}`),
                el("h2", {}, title)),
            el("button", { type: "button", class: "ghost", onclick: () => go("matrix") }, "← До матриць")),
        el("div", { class: "mx-out" },
            matrixCard(detail.points, "Ваша матриця"),
            arcanaSection(detail.points),
            questionSection(detail, detail))));
}

// Сайдбар: збережені матриці, закріплення і видалення
function matrixPinKey() {
    const user = getSessionUser();
    return "taro_matrix_pins_" + (user ? user.userId : "anon");
}

function getMatrixPins() {
    try { return JSON.parse(localStorage.getItem(matrixPinKey())) || []; }
    catch (e) { return []; }
}

function toggleMatrixPin(id) {
    const ids = getMatrixPins();
    localStorage.setItem(matrixPinKey(), JSON.stringify(ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id]));
    refreshMatrixSidebar();
}

async function deleteMatrix(m) {
    if (!confirm("Видалити матрицю з історії?")) return;
    try {
        await api(`/matrix/saved/${m.id}`, { method: "DELETE" });
        localStorage.setItem(matrixPinKey(), JSON.stringify(getMatrixPins().filter((x) => x !== m.id)));
        if (parseHash().key === "matrix" && parseHash().param === m.id) go("matrix");
        else refreshMatrixSidebar();
    } catch (err) {
        alert(err.message);
    }
}

async function refreshMatrixSidebar() {
    const list = document.getElementById("recent-list");
    const pinnedList = document.getElementById("pinned-list");
    const pinnedSection = document.getElementById("pinned-section");
    try {
        const saved = await api("/matrix/saved");
        const pins = getMatrixPins();
        const active = parseHash().key === "matrix" ? parseHash().param : null;
        const row = (m, pinned) => el("div", { class: "chat-item" + (active === m.id ? " active" : "") },
            el("button", { type: "button", class: "chat-title", title: m.label || "", onclick: () => go("matrix", m.id) },
                m.label || `Матриця ${formatDateOnly(m.birthDate)}`),
            el("button", { type: "button", class: "chat-action pin" + (pinned ? " on" : ""),
                title: pinned ? "Відкріпити" : "Закріпити", "aria-label": pinned ? "Відкріпити" : "Закріпити",
                onclick: () => toggleMatrixPin(m.id) }, icon(pinned ? "pinFilled" : "pin")),
            el("button", { type: "button", class: "chat-action", title: "Видалити", "aria-label": "Видалити",
                onclick: () => deleteMatrix(m) }, "×"));

        const pinned = saved.filter((m) => pins.includes(m.id));
        const others = saved.filter((m) => !pins.includes(m.id));
        pinnedSection.hidden = pinned.length === 0;
        pinnedList.replaceChildren(...pinned.map((m) => row(m, true)));
        list.replaceChildren(...(others.length
            ? others.map((m) => row(m, false))
            : [el("p", { class: "muted" }, saved.length ? "Усі матриці закріплені." : "Поки що немає збережених матриць.")]));
    } catch (err) {
        list.replaceChildren(notice(err.message));
    }
}
