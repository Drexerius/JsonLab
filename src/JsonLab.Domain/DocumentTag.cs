namespace JsonLab.Domain;

public sealed class DocumentTag
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DocumentId { get; private set; }
    public string Name { get; private set; } = "";
    private DocumentTag() { }
    internal DocumentTag(Guid documentId, string name) => (DocumentId, Name) = (documentId, name);

    public static string Normalize(string text)
    {
        var value = text.Trim().ToLowerInvariant();
        if (value.Length is < 1 or > 24 || value.Any(c => !char.IsLetterOrDigit(c) && c is not '-' and not '_'))
            throw new DomainException("Тег: 1–24 буквы, цифры, дефис или подчёркивание.");
        return value;
    }
}
