# Birthday Gift List

Веб-приложение-приглашение на день рождения Полины со списком желаемых подарков, бронированием без регистрации и панелью администратора.

## Запуск

1. Установите Docker и Docker Compose.
2. Скопируйте `.env.example` в `.env` и замените пароли.
3. Запустите проект:

```bash
docker compose up --build
```

После запуска сайт будет доступен на `http://localhost:8080`.

## Что внутри

- `frontend` - React, TypeScript, Vite, React Router.
- `backend/BirthdayGifts.Api` - ASP.NET Core Web API, EF Core, Npgsql, cookie-auth.
- `backend/BirthdayGifts.Tests` - xUnit-тесты основной логики бронирования.
- PostgreSQL хранит подарки, бронирования и администратора.
- Загруженные изображения хранятся в Docker volume `uploaded-images`, не в базе.

## Администратор

Администратор создаётся при старте backend, если в базе ещё нет пользователя с именем из `ADMIN_USERNAME`.
Пароль берётся из `ADMIN_PASSWORD`, сохраняется в базе только как hash и обновляется при перезапуске backend.

Маршруты:

- `/admin/login` - вход.
- `/admin` - управление подарками и бронями.

## Миграции

Backend применяет EF Core миграции автоматически при старте контейнера. Для локальной разработки можно выполнить:

```bash
dotnet ef database update --project backend/BirthdayGifts.Api
```

## Фотографии ребёнка

Положите изображения ребёнка в:

```text
frontend/src/assets/child-photos/
```

Frontend автоматически подхватит JPEG, PNG, WebP и GIF из этой папки. Если папка пустая, используется нейтральный праздничный фон. Приложение не использует чужие или сгенерированные фотографии детей.

## Тесты и проверки

```bash
dotnet test backend/BirthdayGifts.Tests
npm --prefix frontend run build
```

Для ручной проверки:

1. Откройте `/`.
2. Перейдите кнопкой «Что мне можно подарить» на `/gifts`.
3. Войдите в `/admin/login`.
4. Добавьте подарок с PNG, JPEG или WebP изображением.
5. Забронируйте подарок в публичном списке.
6. Проверьте, что другой браузер видит подарок забронированным и не может отменить чужую бронь.

## Production

- Для локального `http://localhost:8080` оставьте `COOKIE_SECURE=false`; в production с HTTPS установите `COOKIE_SECURE=true`.
- Поставьте сильные значения `POSTGRES_PASSWORD` и `ADMIN_PASSWORD`.
- Ограничьте `FRONTEND_ORIGIN` реальным origin.
- Настройте регулярные backup PostgreSQL volume и volume с изображениями.
