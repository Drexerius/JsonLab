using JsonLab.Application;
using JsonLab.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JsonLab.Tests;

public sealed class PersistenceTests : IAsyncLifetime
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"jsonlab-test-{Guid.NewGuid():N}.db");
    private TestFactory factory = null!;
    private WorkspaceService service = null!;
    private Guid alice, bob;
    public async Task InitializeAsync()
    {
        factory = new TestFactory(path);
        await using var db = factory.CreateDbContext();
        await db.Database.MigrateAsync();
        var accounts = new AccountService(factory);
        alice = (await accounts.RegisterAsync("alice", "Password123!")).Id;
        bob = (await accounts.RegisterAsync("bob", "Password123!")).Id;
        service = new WorkspaceService(new WorkspaceSessionFactory(factory));
    }
    public Task DisposeAsync()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm" }) File.Delete(path + suffix);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task DemoSeedIsIdempotentAndProvidesWorkingAccount()
    {
        await using (var db = factory.CreateDbContext())
        {
            await DemoData.SeedAsync(db);
            await DemoData.SeedAsync(db);
        }
        var account = await new AccountService(factory).LoginAsync("student", "JsonLab2026!");
        Assert.NotNull(account);
        var workspace = await service.LoadAsync(account.Id);
        Assert.Equal(3, workspace.Documents.Count);
        Assert.Single(workspace.Folders);
    }

    [Fact]
    public async Task CrudSurvivesNewContextAndDeletesChildTags()
    {
        var doc = await service.SaveAsync(alice, null, "a", "{\"a\":1}", null, ["api", "old"]);
        var reopened = new WorkspaceService(new WorkspaceSessionFactory(new TestFactory(path)));
        var loaded = Assert.Single((await reopened.LoadAsync(alice)).Documents);
        Assert.Equal(doc.Id, loaded.Id);
        var changed = await reopened.SaveAsync(alice, loaded.Id, "b", "[1,2]", null, ["api", "new"]);

        Assert.Equal(["api", "new"], changed.Tags.Order().ToArray());
        await reopened.DeleteDocumentAsync(alice, changed.Id);
        Assert.Empty((await reopened.LoadAsync(alice)).Documents);
        await using var db = factory.CreateDbContext();
        Assert.Equal(0, await db.Set<JsonLab.Domain.DocumentTag>().CountAsync());
    }

    [Fact]
    public async Task OwnershipIsEnforcedOnReadUpdateDeleteAndFolderAssignment()
    {
        var doc = await service.SaveAsync(alice, null, "private", "{}", null, []);
        await service.SaveFolderAsync(alice, null, "private");
        var folder = Assert.Single((await service.LoadAsync(alice)).Folders);
        Assert.Empty((await service.LoadAsync(bob)).Documents);
        await Assert.ThrowsAsync<WorkspaceException>(() => service.SaveAsync(bob, doc.Id, "stolen", "{}", null, []));
        await Assert.ThrowsAsync<WorkspaceException>(() => service.DeleteDocumentAsync(bob, doc.Id));
        await Assert.ThrowsAsync<WorkspaceException>(() => service.SaveAsync(bob, null, "x", "{}", folder.Id, []));
        await Assert.ThrowsAsync<WorkspaceException>(() => service.DeleteFolderAsync(bob, folder.Id));
        await Assert.ThrowsAsync<WorkspaceException>(() => service.SaveFolderAsync(bob, folder.Id, "renamed"));
    }

    [Fact]
    public async Task FolderDeletionMovesDocumentsWithoutDeletingThem()
    {
        await service.SaveFolderAsync(alice, null, "Examples");
        var folder = Assert.Single((await service.LoadAsync(alice)).Folders);
        var doc = await service.SaveAsync(alice, null, "a", "{}", folder.Id, ["keep"]);
        await service.DeleteFolderAsync(alice, folder.Id);
        var state = await service.LoadAsync(alice);
        Assert.Empty(state.Folders);
        var moved = Assert.Single(state.Documents);
        Assert.Null(moved.FolderId);
        Assert.Equal("keep", Assert.Single(moved.Tags));
    }

    [Fact]
    public async Task DuplicateFolderNamesAreRejectedPerOwner()
    {
        await service.SaveFolderAsync(alice, null, "Examples");
        await Assert.ThrowsAsync<WorkspaceException>(() => service.SaveFolderAsync(alice, null, " examples "));
        await service.SaveFolderAsync(bob, null, "Examples");
    }

    [Fact]
    public async Task LaterSaveReplacesEarlierContent()
    {
        var doc = await service.SaveAsync(alice, null, "file", "{}", null, ["old"]);
        await service.SaveAsync(alice, doc.Id, "first", "[1]", null, ["one"]);
        await service.SaveAsync(alice, doc.Id, "second", "[2]", null, ["two"]);
        var saved = Assert.Single((await service.LoadAsync(alice)).Documents);
        Assert.Equal("second", saved.Title);
        Assert.Equal("[2]", saved.Content);
        Assert.Equal("two", Assert.Single(saved.Tags));
    }

    [Fact]
    public async Task InvalidSaveDoesNotWriteData()
    {
        await Assert.ThrowsAsync<JsonLab.Domain.DomainException>(() => service.SaveAsync(alice, null, "invalid", "{", null, []));
        Assert.Empty((await service.LoadAsync(alice)).Documents);
        await Assert.ThrowsAsync<WorkspaceException>(() => service.LoadAsync(Guid.Empty));
    }

    [Fact]
    public async Task PasswordsAreHashedAndLoginRejectsWrongPassword()
    {
        var accounts = new AccountService(factory);
        Assert.Null(await accounts.LoginAsync("alice", "wrong"));
        Assert.Equal(alice, (await accounts.LoginAsync("ALICE", "Password123!"))!.Id);
        await using var db = factory.CreateDbContext();
        Assert.NotEqual("Password123!", (await db.Users.SingleAsync(u => u.Id == alice)).PasswordHash);
        await Assert.ThrowsAsync<WorkspaceException>(() => accounts.RegisterAsync("Alice", "Password123!"));
    }

    private sealed class TestFactory(string file) : IDbContextFactory<WorkspaceDbContext>
    {
        public WorkspaceDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<WorkspaceDbContext>().UseSqlite($"Data Source={file};Foreign Keys=True").Options);
        public Task<WorkspaceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
}
