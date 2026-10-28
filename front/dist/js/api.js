// Адреса бекенду (профіль http з launchSettings.json)
const API_BASE = "http://localhost:52639/api";

const TOKEN_KEY = "taro_token";
const USER_KEY = "taro_user";

async function postJson(path, body) {
    let res;
    try {
        res = await fetch(API_BASE + path, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body)
        });
    } catch (e) {
        throw new Error("Немає зв'язку з сервером. Перевірте, чи запущено API.");
    }

    let data = null;
    try {
        data = await res.json();
    } catch (e) {
        // відповідь без тіла
    }

    if (!res.ok) {
        throw new Error((data && data.error) || "Сталася помилка. Спробуйте ще раз.");
    }
    return data;
}

function saveSession(authResponse) {
    localStorage.setItem(TOKEN_KEY, authResponse.token);
    localStorage.setItem(USER_KEY, JSON.stringify({
        userId: authResponse.userId,
        email: authResponse.email,
        role: authResponse.role
    }));
}

function getSessionUser() {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? JSON.parse(raw) : null;
}

function isLoggedIn() {
    return !!localStorage.getItem(TOKEN_KEY);
}

function logout() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    window.location.href = "login.html";
}

function oauthLoginUrl(provider, role, birthDate) {
    const params = new URLSearchParams({ role: role });
    if (birthDate) params.set("birthDate", birthDate);
    return `${API_BASE}/auth/external/login/${provider}?${params.toString()}`;
}