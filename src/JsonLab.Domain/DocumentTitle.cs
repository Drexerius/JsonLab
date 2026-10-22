namespace JsonLab.Domain;

public sealed record DocumentTitle
{
    public string Value { get; }
    private DocumentTitle(string value) => Value = value;
    public static DocumentTitle Create(string? text)
    {
        var value = text?.Trim() ?? "";
        if (value.Length is < 1 or > 100 || value.Any(char.IsControl))
            throw new DomainException("Название должно содержать от 1 до 100 символов без управляющих знаков.");
        return new(value);
    }
}
