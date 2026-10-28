const tarotRoot = document.getElementById("tarot-root");

let topicsCache = null;
let selectedTopic = null;
let savedBackground = "";

// Крок 1: вибір теми. Видно лише назву, опис з'являється при наведенні.
async function renderTopics() {
    const user = getSessionUser();
    if (!user || user.role !== "Client") {
        tarotRoot.replaceChildren(el("p", {}, "Гадання доступне лише для клієнтів."));
        return;
    }

    tarotRoot.replaceChildren(
        el("h2", { class: "greeting" }, "Із чого почнемо?"),
        el("p", { class: "hint" }, "Оберіть тему, а потім розкажіть ситуацію і поставте питання.")
    );

    const grid = el("div", { class: "topics" });
    tarotRoot.append(grid);

    try {
        if (!topicsCache) topicsCache = await authedFetch("/tarot/topics");

        topicsCache.forEach((topic) => {
            const tooltip = topic.story + "\n\nПозиції: " + topic.positions.join(", ");

            grid.append(
                el("button", {
                    type: "button",
                    class: "topic-card",
                    title: tooltip,
                    onclick: () => {
                        selectedTopic = topic;
                        renderBackground();
                    }
                }, topic.title)
            );
        });
    } catch (err) {
        tarotRoot.append(el("p", { class: "error" }, err.message));
    }
}

// Крок 2: передісторія
function renderBackground() {
    const input = el("textarea", { rows: "6", maxlength: "1500" });
    input.value = savedBackground;
    const errorEl = el("p", { class: "error" });

    tarotRoot.replaceChildren(
        el("h2", {}, selectedTopic.title),
        el("p", {}, "Розкажіть коротко вашу ситуацію, щоб трактування було зрозумілим (від 10 до 1500 символів)."),
        el("form", {
                onsubmit: (e) => {
                    e.preventDefault();
                    const text = input.value.trim();
                    if (text.length < 10) {
                        errorEl.textContent = "Напишіть трохи більше, щонайменше 10 символів.";
                        return;
                    }
                    savedBackground = text;
                    renderQuestion();
                }
            },
            input,
            errorEl,
            el("button", { type: "button", onclick: renderTopics }, "Назад"),
            el("button", { type: "submit" }, "Далі")
        )
    );
}

// Крок 3: питання і запуск гадання
function renderQuestion() {
    const input = el("input", { type: "text", maxlength: "500" });
    const errorEl = el("p", { class: "error" });
    const submit = el("button", { type: "submit" }, "Почати гадання");

    tarotRoot.replaceChildren(
        el("h2", {}, "Ваше питання"),
        el("form", {
                onsubmit: async (e) => {
                    e.preventDefault();
                    const question = input.value.trim();
                    if (question.length < 3) {
                        errorEl.textContent = "Питання має бути щонайменше 3 символи.";
                        return;
                    }

                    submit.disabled = true;
                    tarotRoot.append(el("p", { id: "loading" }, "Тасуємо карти..."));
                    try {
                        const detail = await authedFetch("/tarot/readings", {
                            method: "POST",
                            body: { topic: selectedTopic.key, background: savedBackground, question: question }
                        });
                        showReading(detail);
                    } catch (err) {
                        document.getElementById("loading")?.remove();
                        errorEl.textContent = err.message;
                        submit.disabled = false;
                    }
                }
            },
            input,
            errorEl,
            el("button", { type: "button", onclick: renderBackground }, "Назад"),
            submit
        )
    );
}

// Розклад і чат
function showReading(detail) {
    window.currentReadingId = detail.id;
    if (window.refreshSidebar) refreshSidebar();

    const messagesEl = el("div", { id: "messages" },
        ...detail.messages.map((m) =>
            el("div", { class: "msg " + (m.role === "User" ? "user" : "assistant") },
                el("strong", {}, m.role === "User" ? "Ви: " : "Таролог: "),
                m.text
            )
        )
    );

    const followInput = el("input", { type: "text", maxlength: "500", placeholder: "Уточнювальне питання..." });
    const followError = el("p", { class: "error" });
    const followSubmit = el("button", { type: "submit" }, "Надіслати");

    tarotRoot.replaceChildren(
        el("h2", {}, detail.title),
        el("p", {}, "Питання: " + detail.question),

        el("h3", {}, "Карти"),
        el("ul", {},
            ...detail.cards.map((c) =>
                el("li", {},
                    `${c.positionName}: ${c.cardName}${c.isReversed ? " (перевернута)" : ""}. ${c.meaning}`
                )
            )
        ),

        el("h3", {}, "Розмова"),
        messagesEl,

        el("form", {
                onsubmit: async (e) => {
                    e.preventDefault();
                    const question = followInput.value.trim();
                    if (question.length < 3) {
                        followError.textContent = "Питання має бути щонайменше 3 символи.";
                        return;
                    }

                    followSubmit.disabled = true;
                    followSubmit.textContent = "Таролог думає...";
                    try {
                        const next = await authedFetch(`/tarot/readings/${detail.id}/messages`, {
                            method: "POST",
                            body: { question: question }
                        });
                        showReading(next);
                    } catch (err) {
                        followError.textContent = err.message;
                        followSubmit.disabled = false;
                        followSubmit.textContent = "Надіслати";
                    }
                }
            },
            followInput,
            followError,
            followSubmit
        )
    );

    messagesEl.scrollTop = messagesEl.scrollHeight;
}