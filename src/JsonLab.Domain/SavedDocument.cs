namespace JsonLab.Domain;

/// <summary>Корень агрегата документа. Изменение состояния возможно только через методы.</summary>
public sealed class SavedDocument
{
    private readonly List<DocumentTag> _tags = [];
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public DocumentTitle Title { get; private set; } = null!;
    public JsonContent Content { get; private set; } = null!;
    public Guid? FolderId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<DocumentTag> Tags => _tags.AsReadOnly();
    private SavedDocument() { }

    public static SavedDocument Create(Guid owner, string title, string content, Guid? folder, IEnumerable<string> tags)
    {
        if (owner == Guid.Empty) throw new DomainException("Не задан владелец.");
        var doc = new SavedDocument { Id = Guid.NewGuid(), OwnerId = owner, CreatedAtUtc = DateTime.UtcNow };
        doc.Update(title, content, folder, tags);
        return doc;
    }

    public void Update(string title, string content, Guid? folder, IEnumerable<string> tags)
    {
        // Сначала проверяем все значения; не оставляем агрегат частично изменённым.
        var newTitle = DocumentTitle.Create(title);
        var newContent = JsonContent.Create(content);
        var names = tags.Select(DocumentTag.Normalize).Distinct(StringComparer.Ordinal).ToArray();
        if (names.Length > 8) throw new DomainException("Можно назначить не более 8 тегов.");
        Title = newTitle;
        Content = newContent;
        FolderId = folder;
        _tags.RemoveAll(t => !names.Contains(t.Name, StringComparer.Ordinal));
        foreach (var name in names.Where(n => _tags.All(t => t.Name != n)))
            _tags.Add(new DocumentTag(Id, name));
        Touch();
    }

    public void MoveToRoot()
    {
        FolderId = null;
        Touch();
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
