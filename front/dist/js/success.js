// Без збереженої сесії сюди заходити не можна
if (!isLoggedIn()) {
    window.location.replace("login.html");
}

const params = new URLSearchParams(window.location.search);
const type = params.get("type");

const user = getSessionUser();
document.getElementById("title").textContent =
    type === "register" ? "Реєстрація успішна!" : "Вхід успішний!";
document.getElementById("email").textContent = user ? user.email : "";

const counterEl = document.getElementById("counter");
let secondsLeft = 3;

const timer = setInterval(() => {
    secondsLeft--;
    counterEl.textContent = secondsLeft;

    if (secondsLeft <= 0) {
        clearInterval(timer);
        window.location.replace("home.html");
    }
}, 1000);