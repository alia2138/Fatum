const registerForm = document.getElementById("register-form");
const registerError = document.getElementById("error");
const registerButton = document.getElementById("submit");
const roleSelect = document.getElementById("role");
const birthDateField = document.getElementById("birth-date-field");
const birthDateInput = registerForm.birthDate;

// Дата народження потрібна лише клієнту
function updateBirthDateVisibility() {
    const isClient = roleSelect.value === "Client";
    birthDateField.classList.toggle("hidden", !isClient);
    birthDateInput.required = isClient;
}

roleSelect.addEventListener("change", updateBirthDateVisibility);
updateBirthDateVisibility();

registerForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    registerError.textContent = "";

    const password = registerForm.password.value;
    const confirmPassword = registerForm.confirmPassword.value;

    if (password !== confirmPassword) {
        registerError.textContent = "Паролі не збігаються.";
        return;
    }

    const role = roleSelect.value;
    const body = {
        role: role,
        email: registerForm.email.value.trim(),
        password: password,
        confirmPassword: confirmPassword,
        birthDate: role === "Client" && birthDateInput.value ? birthDateInput.value : null
    };

    registerButton.disabled = true;

    try {
        const data = await postJson("/auth/register", body);

        saveSession(data);
        window.location.href = "success.html?type=register";
    } catch (err) {
        registerError.textContent = err.message;
        registerButton.disabled = false;
    }
});