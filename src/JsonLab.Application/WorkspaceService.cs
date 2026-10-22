using JsonLab.Domain;

namespace JsonLab.Application;

public sealed class WorkspaceService(IWorkspaceSessionFactory sessions)
{
    public async Task<WorkspaceView> LoadAsync(Guid owner)
    {
        RequireOwner(owner);
        await using var db = await sessions.OpenAsync();
        var documents = await db.DocumentsAsync(owner);
        var folders = await db.FoldersAsync(owner);
        return new(documents.OrderByDescending(d => d.UpdatedAtUtc).Select(View).ToArray(),
            folders.OrderBy(f => f.Name).Select(f => new FolderView(f.Id, f.Name)).ToArray());
    }

    public async Task<DocumentView> SaveAsync(Guid owner, Guid? id,
        string title, string content, Guid? folderId, IEnumerable<string> tags)
    {
        RequireOwner(owner);
        await using var db = await sessions.OpenAsync();
        if (folderId is Guid folder && await db.FolderAsync(owner, folder) is null)
            throw new WorkspaceException("Папка не найдена.");
        SavedDocument document;
        if (id is Guid existing)
        {
            document = await db.DocumentAsync(owner, existing) ?? throw new WorkspaceException("Документ не найден.");
            document.Update(title, content, folderId, tags);
        }
        else
        {
            document = SavedDocument.Create(owner, title, content, folderId, tags);
            db.Add(document);
        }
        await db.CommitAsync();
        return View(document);
    }

    public async Task DeleteDocumentAsync(Guid owner, Guid id)
    {
        RequireOwner(owner);
        await using var db = await sessions.OpenAsync();
        var document = await db.DocumentAsync(owner, id) ?? throw new WorkspaceException("Документ не найден.");
        db.Remove(document);
        await db.CommitAsync();
    }

    public async Task SaveFolderAsync(Guid owner, Guid? id, string name)
    {
        RequireOwner(owner);
        await using var db = await sessions.OpenAsync();
        var folder = id is Guid key
            ? await db.FolderAsync(owner, key) ?? throw new WorkspaceException("Папка не найдена.")
            : Folder.Create(owner, name);
        folder.Rename(name);
        if ((await db.FoldersAsync(owner)).Any(f => f.Id != folder.Id && f.NormalizedName == folder.NormalizedName))
            throw new WorkspaceException("Папка с таким названием уже существует.");
        if (id is null) db.Add(folder);
        await db.CommitAsync();
    }

    public async Task DeleteFolderAsync(Guid owner, Guid id)
    {
        RequireOwner(owner);
        await using var db = await sessions.OpenAsync();
        var folder = await db.FolderAsync(owner, id) ?? throw new WorkspaceException("Папка не найдена.");
        foreach (var document in (await db.DocumentsAsync(owner)).Where(d => d.FolderId == id))
            document.MoveToRoot();
        db.Remove(folder);
        await db.CommitAsync(); // Документы перемещаются и папка удаляется одной транзакцией.
    }

    private static void RequireOwner(Guid owner)
    {
        if (owner == Guid.Empty) throw new WorkspaceException("Сначала войдите в аккаунт.");
    }
    private static DocumentView View(SavedDocument d) =>
        new(d.Id, d.Title.Value, d.Content.Value, d.FolderId, d.Tags.Select(t => t.Name).ToArray(), d.UpdatedAtUtc);
}
