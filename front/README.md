# TaroPlatform: фронтенд

## Збірка стилів
    npm install
    npx gulp

Gulp компілює `src/scss/style.scss` у `dist/css/style.css`. У режимі `npx gulp` він слідкує за змінами SCSS.

## Запуск
1. Запустити бекенд (порт 52639, профіль http).
2. Відкрити фронт через локальний сервер (VS Code Live Server або IntelliJ, порт 63342).
3. Адреса фронту має бути в `Frontend:AllowedOrigins` бекенду (CORS).

## Структура
- `dist/html` — сторінки: login, register, success, external (вхід через соцмережі), home (оболонка застосунку).
- `dist/js/api.js` — запити до API, сесія, спільні хелпери.
- `dist/js/app.js` — сайдбар, роутер за хешем (`#tarot`, `#catalog/{id}`, `#chats/{id}` тощо), перевірка ролей.
- `dist/js/views/` — сторінки розділів (гадання, матриця, каталог, консультації, чати, обране, профіль, сповіщення, графік, відгуки).
- `src/scss` — стилі: `utils` (змінні), `base`, `components`, `layout`, `pages`.
