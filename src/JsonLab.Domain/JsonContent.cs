using System.Text;
using System.Text.Json;

namespace JsonLab.Domain;

/// <summary>Неизменяемое значение: в агрегат попадает только корректный JSON.</summary>
public sealed record JsonContent
{
    public const int MaxBytes = 262144;
    public const int MaxDepth = 64;
    public string Value { get; }
    private JsonContent(string value) => Value = value;

    public static JsonContent Create(string text)
    {
        if (Encoding.UTF8.GetByteCount(text) > MaxBytes)
            throw new DomainException("Документ превышает 256 КиБ.");
        try
        {
            using var parsed = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = MaxDepth });
        }
        catch (JsonException ex)
        {
            var lineIndex = (int)(ex.LineNumber ?? 0);
            var line = text.Split('\n')[lineIndex];
            long bytes = 0;
            var column = 1;
            foreach (var rune in line.EnumerateRunes())
            {
                if (bytes >= (ex.BytePositionInLine ?? 0)) break;
                bytes += rune.Utf8SequenceLength;
                column++;
            }
            throw new DomainException($"Ошибка JSON: строка {lineIndex + 1}, столбец {column}. Проверьте синтаксис JSON и глубину вложенности (до {MaxDepth} уровней).");
        }
        return new(text);
    }

    public JsonContent Format(bool indented)
    {
        using var parsed = JsonDocument.Parse(Value, new JsonDocumentOptions { MaxDepth = MaxDepth });
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Indented = indented,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }))
            parsed.RootElement.WriteTo(writer);
        // Повторная проверка размера важна: отступы увеличивают документ.
        return Create(Encoding.UTF8.GetString(stream.ToArray()));
    }
}
