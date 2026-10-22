using JsonLab.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JsonLab.Infrastructure;

public static class DemoData
{
    /// <summary>Явно включаемые демонстрационные данные. Существующие аккаунты не меняются.</summary>
    public static async Task SeedAsync(WorkspaceDbContext db)
    {
        if (await db.Users.AnyAsync(x => x.NormalizedName == "STUDENT")) return;
        var user = new AuthUser { UserName = "student", NormalizedName = "STUDENT" };
        user.PasswordHash = new PasswordHasher<AuthUser>().HashPassword(user, "JsonLab2026!");
        var folder = Folder.Create(user.Id, "Примеры API");
        db.Users.Add(user);
        db.Folders.Add(folder);
        db.Documents.Add(SavedDocument.Create(user.Id, "Ответ API.json", """
            {
              "status": "ok",
              "requestId": "req-2026-001",
              "data": {
                "id": 42,
                "name": "Пример пользователя",
                "roles": ["reader", "editor"],
                "active": true
              },
              "pagination": { "page": 1, "total": 24 }
            }
            """, folder.Id, ["api", "пример"]));
        db.Documents.Add(SavedDocument.Create(user.Id, "Настройки.json", """
            {"theme":"dark","language":"ru","editor":{"indent":2,"wordWrap":false},"features":["tree","validation","library"]}
            """, null, ["конфигурация"]));
        db.Documents.Add(SavedDocument.Create(user.Id, "Каталог.json", """
            [{"id":1,"name":"Блокнот","price":250.50},{"id":2,"name":"Карандаш","price":35}]
            """, folder.Id, ["массив", "пример"]));
        await db.SaveChangesAsync();
    }
}
