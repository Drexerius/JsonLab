namespace JsonLab.Infrastructure;

public sealed class AuthUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserName { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}
