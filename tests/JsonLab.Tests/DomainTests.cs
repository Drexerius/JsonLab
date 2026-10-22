using JsonLab.Domain;
using System.Text.Json;

namespace JsonLab.Tests;

public class DomainTests
{
    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("42")]
    [InlineData("\"строка\"")]
    public void AllJsonRootTypesAreAccepted(string json) => Assert.Equal(json, JsonContent.Create(json).Value);

    [Theory]
    [InlineData("")]
    [InlineData("{\"a\":}")]
    [InlineData("{\"a\":1,}")]
    [InlineData("{/* comment */}")]
    public void InvalidJsonIsRejected(string json) => Assert.Throws<DomainException>(() => JsonContent.Create(json));

    [Theory]
    [InlineData("{\"я\":}", 1, 6)]
    [InlineData("{\"😀\":}", 1, 6)]
    [InlineData("{\r\n\"я\":}", 2, 5)]
    [InlineData("", 1, 1)]
    public void ErrorLocationCountsUnicodeCharacters(string json, int line, int column)
    {
        var error = Assert.Throws<DomainException>(() => JsonContent.Create(json));
        Assert.Contains($"строка {line}, столбец {column}.", error.Message);
        Assert.DoesNotContain("BytePosition", error.Message);
        Assert.DoesNotContain("байт", error.Message);
    }

    [Fact]
    public void FormattingPreservesNumbersAndUnicode()
    {
        const string json = "{\"n\":123456789012345678901234567890,\"text\":\"Привет\",\"a\":[true,null,1.2e30]}";
        var formatted = JsonContent.Create(json).Format(true);
        Assert.Contains('\n', formatted.Value);
        Assert.Equal(json, formatted.Format(false).Value);
    }

    [Fact]
    public void Utf8SizeAndDepthAreBounded()
    {
        Assert.Throws<DomainException>(() => JsonContent.Create("\"" + new string('я', JsonContent.MaxBytes / 2) + "\""));
        Assert.Throws<DomainException>(() => JsonContent.Create(new string('[', 65) + "0" + new string(']', 65)));
    }

    [Fact]
    public void FailedUpdateDoesNotPartiallyMutateAggregate()
    {
        var doc = SavedDocument.Create(Guid.NewGuid(), "before", "{}", null, ["api"]);
        Assert.Throws<DomainException>(() => doc.Update("after", "[]", null, ["bad tag"]));
        Assert.Equal("before", doc.Title.Value);
        Assert.Equal("{}", doc.Content.Value);
        Assert.Single(doc.Tags);
    }

    [Fact]
    public void TagsAreNormalizedDeduplicatedAndLimited()
    {
        var doc = SavedDocument.Create(Guid.NewGuid(), "file", "{}", null, [" API ", "api"]);
        Assert.Equal("api", Assert.Single(doc.Tags).Name);
        Assert.Throws<DomainException>(() => doc.Update("file", "{}", null, Enumerable.Range(0, 9).Select(i => $"tag{i}")));
    }

    [Fact]
    public void RenamingAndMovingPreserveDocumentIdentity()
    {
        var doc = SavedDocument.Create(Guid.NewGuid(), "file", "{}", Guid.NewGuid(), []);
        var id = doc.Id;
        doc.Update("renamed", "[]", doc.FolderId, ["new"]);
        doc.MoveToRoot();
        Assert.Equal(id, doc.Id);
        Assert.Null(doc.FolderId);
        Assert.Equal("renamed", doc.Title.Value);
    }
}
