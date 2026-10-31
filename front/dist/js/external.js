// Сторінка після входу через Google/GitHub: бекенд передає дані у фрагменті URL
const hashParams = new URLSearchParams(window.location.hash.slice(1));
const titleEl = document.getElementById("title");
const errorEl = document.getElementById("error");

if (hashParams.get("error")) {
    titleEl.textContent = "Вхід не вдався";
    errorEl.textContent = hashParams.get("error");
} else if (hashParams.get("token")) {
    saveSession({
        token: hashParams.get("token"),
        userId: hashParams.get("userId"),
        email: hashParams.get("email"),
        role: hashParams.get("role")
    });
    window.location.replace("success.html?type=login");
} else {
    errorEl.textContent = "Немає даних для входу.";
}
