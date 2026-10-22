using System.Text.Json;
namespace JsonLab.Web.Components;
public sealed record TreeItem(string Name, string Value, bool IsString, List<TreeItem> Children)
{
    public static TreeItem Build(JsonElement element, string name, ref int remaining)
    {
        remaining--;
        List<TreeItem> children = [];
        if (element.ValueKind == JsonValueKind.Object)
            foreach (var property in element.EnumerateObject())
            {
                if (remaining <= 0) break;
                children.Add(Build(property.Value, property.Name, ref remaining));
            }
        if (element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                if (remaining <= 0) break;
                children.Add(Build(item, $"[{index++}]", ref remaining));
            }
        }
        var value = element.ValueKind switch
        {
            JsonValueKind.Object => $"{{ {element.EnumerateObject().Count()} полей }}",
            JsonValueKind.Array => $"[ {element.GetArrayLength()} элементов ]",
            _ => element.GetRawText()
        };
        if (value.Length > 120) value = value[..120] + "…";
        return new(name, value, element.ValueKind == JsonValueKind.String, children);
    }
}
