# JSON Lab

JSON-редактор на C# / .NET 10, Blazor и SQLite.
Проверка и форматирование JSON, дерево, импорт и экспорт, личная библиотека с папками и тегами.

## Запуск

1. Откройте `JsonLab.sln` в Visual Studio 2026 с компонентами ASP.NET и .NET 10.
2. Выберите `JsonLab.Web` запускаемым проектом и профиль `https-manual-browser`.
3. Нажмите F5 и откройте https://localhost:7174.

Другой способ: `Start.cmd`, адрес http://localhost:5196.
Для первой сборки нужен интернет. База создаётся автоматически.

Демонстрационный вход: **student / JsonLab2026!**
Изменения сохраняются кнопкой «Сохранить». Автосохранения нет. При работе в нескольких вкладках сохраняется последний отправленный вариант.

## Где что находится

- `src/JsonLab.Web` – интерфейс Blazor и запуск приложения.
- `src/JsonLab.Domain` – документы, папки, теги и правила проверки.
- `src/JsonLab.Application` – операции библиотеки.
- `src/JsonLab.Infrastructure` – EF Core, SQLite и миграции.
- `tests/JsonLab.Tests` – тесты; запуск через `Test.cmd`.

База: `src/JsonLab.Web/App_Data/jsonlab.db`.
Для резервной копии остановите приложение и скопируйте `App_Data`.

Схема базы данных: [docs/schema.sql](docs/schema.sql).
