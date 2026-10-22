using JsonLab.Domain;

namespace JsonLab.Application;

public interface IWorkspaceSession : IAsyncDisposable
{
    Task<List<SavedDocument>> DocumentsAsync(Guid owner);
    Task<SavedDocument?> DocumentAsync(Guid owner, Guid id);
    Task<List<Folder>> FoldersAsync(Guid owner);
    Task<Folder?> FolderAsync(Guid owner, Guid id);
    void Add(SavedDocument document);
    void Add(Folder folder);
    void Remove(SavedDocument document);
    void Remove(Folder folder);
    Task CommitAsync();
}

public interface IWorkspaceSessionFactory
{
    Task<IWorkspaceSession> OpenAsync();
}

public sealed class WorkspaceException(string message) : Exception(message);

public sealed record DocumentView(Guid Id, string Title, string Content, Guid? FolderId,
    string[] Tags, DateTime UpdatedAtUtc);
public sealed record FolderView(Guid Id, string Name);
public sealed record WorkspaceView(IReadOnlyList<DocumentView> Documents, IReadOnlyList<FolderView> Folders);
