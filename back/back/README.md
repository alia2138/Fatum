# TaroPlatform — Backend (Архів №1: каркас)

## Вимоги
- .NET 8 SDK
- MongoDB (локально або Docker)

## Запуск MongoDB через Docker
    docker run -d --name taro-mongo -p 27017:27017 mongo:7

## Запуск API
    cd src/TaroPlatform.Api
    dotnet run

Swagger: https://localhost:<port>/swagger
Перевірка: GET /api/health — має повернути mongo: "connected".

## Тести
    dotnet test src/TaroPlatform.Tests

## Структура шарів
- Domain — сутності та базові класи, без залежностей
- Application — інтерфейси, Result, бізнес-логіка (далі)
- Infrastructure — MongoDB, репозиторії, сервіси
- Api — контролери, middleware, Program.cs
