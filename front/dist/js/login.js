const loginForm = document.getElementById("login-form");
const loginError = document.getElementById("error");
const loginButton = document.getElementById("submit");

// Якщо вже увійшли, одразу на головну
if (isLoggedIn()) {
    window.location.href = "home.html";
}

loginForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    loginError.textContent = "";
    loginButton.disabled = true;

    try {
        const data = await postJson("/auth/login", {
            email: loginForm.email.value.trim(),
            password: loginForm.password.value
        });

        saveSession(data);
        window.location.href = "success.html?type=login";
    } catch (err) {
        loginError.textContent = err.message;
        loginButton.disabled = false;
    }
});