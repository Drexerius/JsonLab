namespace JsonLab.Domain;

public sealed class Folder
{
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = "";
    public string NormalizedName { get; private set; } = "";
    private Folder() { }
    public static Folder Create(Guid owner, string name)
    {
        if (owner == Guid.Empty) throw new DomainException("Не задан владелец.");
        var folder = new Folder { Id = Guid.NewGuid(), OwnerId = owner };
        folder.Rename(name);
        return folder;
    }
    public void Rename(string name)
    {
        var title = DocumentTitle.Create(name).Value;
        if (title.Length > 50) throw new DomainException("Название папки не длиннее 50 символов.");
        Name = title;
        NormalizedName = title.ToUpperInvariant();
    }
}
