// Каталог таро-консультантів, картка консультанта, бронювання часу
const PAGE_SIZE = 12;

function todayIso() {
    const d = new Date();
    d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
    return d.toISOString().slice(0, 10);
}

function consultantCard(c) {
    return el("article", { class: "consultant-card", onclick: () => go("catalog", c.id) },
        el("div", { class: "avatar" }, c.photoUrl ? el("img", { src: assetUrl(c.photoUrl), alt: "" }) : initials(c.displayName)),
        el("div", { class: "consultant-info" },
            el("h3", {}, c.displayName),
            el("p", { class: "muted" }, c.bioPreview || ""),
            el("p", {}, `Досвід: ${c.experienceYears} р. · ${c.languages.join(", ")}`),
            el("p", {}, c.specializations.map((t) => TOPIC_TITLES[t] || t).join(", ")),
            el("p", { class: "price" }, `${c.priceUah} ₴ / ${c.sessionMinutes} хв · ${ratingText(c.ratingAverage, c.reviewsCount)}`)));
}

async function renderCatalog(container, consultantId) {
    if (consultantId) return renderConsultant(container, consultantId);
    return renderCatalogList(container);
}

const SORTERS = {
    rating: (a, b) => (b.ratingAverage ?? -1) - (a.ratingAverage ?? -1)
        || b.reviewsCount - a.reviewsCount
        || a.displayName.localeCompare(b.displayName),
    price: (a, b) => a.priceUah - b.priceUah,
    experience: (a, b) => b.experienceYears - a.experienceYears,
    name: (a, b) => a.displayName.localeCompare(b.displayName)
};

// Завантажує всіх консультантів, які підходять під фільтри (сторінками по 50)
async function fetchAllConsultants(filters) {
    const items = [];
    let page = 1;
    let total = 0;
    do {
        const params = new URLSearchParams({ ...filters, sortBy: "name", desc: "false", page: page, pageSize: 50 });
        const data = await api(`/consultants?${params.toString()}`);
        total = data.total;
        items.push(...data.items);
        page++;
        if (data.items.length === 0) break;
    } while (items.length < total);
    return items;
}

function renderCatalogList(container) {
    const search = el("input", { type: "text", placeholder: "Пошук за іменем або описом" });
    const topic = el("select", {}, el("option", { value: "" }, "Усі теми"),
        ...Object.entries(TOPIC_TITLES).map(([key, label]) => el("option", { value: key }, label)));
    const language = el("input", { type: "text", placeholder: "Мова, наприклад Українська" });
    const minPrice = el("input", { type: "number", min: "0", placeholder: "Ціна від, ₴" });
    const maxPrice = el("input", { type: "number", min: "0", placeholder: "Ціна до, ₴" });
    const minExp = el("input", { type: "number", min: "0", placeholder: "Досвід від, років" });
    const sortBy = el("select", { onchange: () => render() },
        ...[["rating", "За рейтингом"], ["price", "За ціною"], ["experience", "За досвідом"], ["name", "За іменем"]]
            .map(([key, label]) => el("option", { value: key }, label)));
    const count = el("p", { class: "hint" });
    const grid = el("div", { class: "catalog-grid" });
    let cache = [];

    function render() {
        const list = [...cache].sort(SORTERS[sortBy.value]);
        count.textContent = `Консультантів: ${list.length}`;
        grid.replaceChildren(...(list.length ? list.map(consultantCard) : [muted("Нікого не знайдено. Змініть фільтри.")]));
    }

    async function load() {
        const filters = {};
        if (search.value.trim()) filters.search = search.value.trim();
        if (topic.value) filters.topic = topic.value;
        if (language.value.trim()) filters.language = language.value.trim();
        if (minPrice.value) filters.minPrice = minPrice.value;
        if (maxPrice.value) filters.maxPrice = maxPrice.value;
        if (minExp.value) filters.minExperience = minExp.value;

        grid.replaceChildren(muted("Завантаження..."));
        try {
            cache = await fetchAllConsultants(filters);
            render();
        } catch (err) {
            grid.replaceChildren(notice(err.message));
        }
    }

    container.replaceChildren(el("div", { class: "page wide" },
        el("h2", {}, "Консультанти"),
        el("form", { class: "filters", onsubmit: (e) => { e.preventDefault(); load(); } },
            search, topic, language, minPrice, maxPrice, minExp, sortBy,
            el("button", { type: "submit", class: "primary" }, "Шукати")),
        count,
        grid));

    load();
}

async function renderConsultant(container, id) {
    const [c, reviews, favorites] = await Promise.all([
        api(`/consultants/${id}`),
        api(`/reviews/consultant/${id}?page=1&pageSize=5`),
        api("/favorites?type=Consultant")
    ]);

    let favorite = favorites.find((f) => f.targetId === id) || null;
    const favBtn = el("button", { type: "button", class: "ghost", onclick: async () => {
        try {
            if (favorite) {
                await api(`/favorites/${favorite.id}`, { method: "DELETE" });
                favorite = null;
            } else {
                favorite = await api("/favorites", { method: "POST", body: { type: "Consultant", targetId: id } });
            }
            favBtn.textContent = favorite ? "★ В обраному" : "☆ До обраного";
        } catch (err) {
            alert(err.message);
        }
    } }, favorite ? "★ В обраному" : "☆ До обраного");

    const dateInput = el("input", { type: "date", value: todayIso(), min: todayIso(), onchange: () => loadSlots() });
    const slotsBox = el("div", { class: "slots" });
    const topicSelect = el("select", {}, el("option", { value: "" }, "Тема (необов'язково)"),
        ...Object.entries(TOPIC_TITLES).map(([key, label]) => el("option", { value: key }, label)));
    const question = el("textarea", { rows: "3", maxlength: "500", placeholder: "Опишіть коротко, з чим прийшли" });
    const bookError = el("p", { class: "error" });
    const bookBtn = el("button", { type: "submit", class: "primary", disabled: true }, "Забронювати");
    let chosen = null;

    async function loadSlots() {
        chosen = null;
        bookBtn.disabled = true;
        slotsBox.replaceChildren(muted("Завантаження..."));
        try {
            const data = await api(`/consultants/${id}/slots?date=${dateInput.value}`);
            if (!data.slots.length) {
                slotsBox.replaceChildren(muted("Вільних слотів на цей день немає."));
                return;
            }
            slotsBox.replaceChildren(...data.slots.map((s) => el("button", { type: "button", class: "slot", onclick: (e) => {
                chosen = s;
                slotsBox.querySelectorAll(".slot").forEach((b) => b.classList.remove("selected"));
                e.currentTarget.classList.add("selected");
                bookBtn.disabled = false;
            } }, s.startLocal)));
        } catch (err) {
            slotsBox.replaceChildren(notice(err.message));
        }
    }

    const bookForm = el("form", { onsubmit: async (e) => {
        e.preventDefault();
        bookError.textContent = "";
        if (!chosen) return;
        const text = question.value.trim();
        if (text.length < 3) {
            bookError.textContent = "Опишіть ситуацію (щонайменше 3 символи).";
            return;
        }
        bookBtn.disabled = true;
        try {
            await api("/consultations", {
                method: "POST",
                body: { consultantId: id, startUtc: chosen.startUtc, topic: topicSelect.value || null, question: text }
            });
            go("consultations");
        } catch (err) {
            bookError.textContent = err.message;
            bookBtn.disabled = false;
        }
    } }, topicSelect, question, bookError, bookBtn);

    const bookingSection = c.isAcceptingClients
        ? el("section", { class: "card-block" },
            el("h3", {}, "Запис на консультацію"),
            el("label", {}, "Дата", dateInput),
            el("p", { class: "hint" }, `Час у таймзоні консультанта: ${c.timezone}`),
            slotsBox,
            bookForm)
        : el("section", { class: "card-block" }, el("h3", {}, "Запис"), muted("Консультант зараз не приймає нових клієнтів."));

    container.replaceChildren(el("div", { class: "page" },
        el("button", { type: "button", class: "ghost", onclick: () => go("catalog") }, "← До каталогу"),
        el("div", { class: "profile-head" },
            el("div", { class: "avatar big" }, c.photoUrl ? el("img", { src: assetUrl(c.photoUrl), alt: "" }) : initials(c.displayName)),
            el("div", {},
                el("h2", {}, c.displayName),
                el("p", { class: "muted" }, `${c.experienceYears} р. досвіду · ${c.languages.join(", ")}`),
                el("p", { class: "price" }, `${c.priceUah} ₴ / ${c.sessionMinutes} хв · ${ratingText(c.ratingAverage, c.reviewsCount)}`),
                favBtn)),
        c.bio ? el("p", { class: "bio" }, c.bio) : null,
        el("p", {}, "Спеціалізації: " + c.specializations.map((t) => TOPIC_TITLES[t] || t).join(", ")),
        bookingSection,
        el("section", { class: "card-block" },
            el("h3", {}, "Відгуки"),
            ...(reviews.items.length ? reviews.items.map(reviewItem) : [muted("Відгуків поки немає.")]))));

    if (c.isAcceptingClients) loadSlots();
}

function reviewItem(r) {
    return el("div", { class: "review" },
        el("div", { class: "row" }, el("strong", {}, r.clientName), el("span", { class: "stars" }, stars(r.rating))),
        el("p", {}, r.text || ""),
        r.consultantReply ? el("p", { class: "reply" }, "Відповідь: " + r.consultantReply) : null);
}
