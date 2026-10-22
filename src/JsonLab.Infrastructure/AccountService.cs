using JsonLab.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JsonLab.Infrastructure;

public sealed class AccountService(IDbContextFactory<WorkspaceDbContext> factory)
{
    private readonly PasswordHasher<AuthUser> _hasher = new();
    public async Task<AuthUser> RegisterAsync(string name, string password)
    {
        name = name.Trim();
        if (name.Length is < 3 or > 32 || name.Any(c => !char.IsLetterOrDigit(c) && c is not '-' and not '_'))
            throw new WorkspaceException("Логин: 3–32 буквы, цифры, дефис или подчёркивание.");
        if (password.Length is < 8 or > 128)
            throw new WorkspaceException("Пароль должен содержать от 8 до 128 символов.");
        await using var db = await factory.CreateDbContextAsync();
        var user = new AuthUser { UserName = name, NormalizedName = name.ToUpperInvariant() };
        user.PasswordHash = _hasher.HashPassword(user, password);
        db.Users.Add(user);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { throw new WorkspaceException("Логин уже занят или база данных недоступна."); }
        return user;
    }
    public async Task<AuthUser?> LoginAsync(string name, string password)
    {
        if (name.Length > 32 || password.Length > 128) return null;
        await using var db = await factory.CreateDbContextAsync();
        var normalized = name.Trim().ToUpperInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.NormalizedName == normalized);
        if (user is null || _hasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed)
            return null;
        return user;
    }
}
