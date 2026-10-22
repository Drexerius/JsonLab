using JsonLab.Application;
using JsonLab.Domain;
using Microsoft.EntityFrameworkCore;

namespace JsonLab.Infrastructure;

public sealed class WorkspaceDbContext(DbContextOptions<WorkspaceDbContext> options) : DbContext(options), IWorkspaceSession
{
    public DbSet<AuthUser> Users => Set<AuthUser>();
    public DbSet<SavedDocument> Documents => Set<SavedDocument>();
    public DbSet<Folder> Folders => Set<Folder>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        var user = model.Entity<AuthUser>();
        user.HasKey(x => x.Id);
        user.HasIndex(x => x.NormalizedName).IsUnique();
        user.Property(x => x.UserName).HasMaxLength(32);
        var folder = model.Entity<Folder>();
        folder.HasKey(x => x.Id);
        folder.HasIndex(x => new { x.OwnerId, x.NormalizedName }).IsUnique();
        folder.HasOne<AuthUser>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        var document = model.Entity<SavedDocument>();
        document.HasKey(x => x.Id);
        document.Property(x => x.Title).HasConversion(x => x.Value, x => DocumentTitle.Create(x));
        document.Property(x => x.Content).HasConversion(x => x.Value, x => JsonContent.Create(x));
        document.HasIndex(x => x.OwnerId);
        document.HasOne<AuthUser>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        document.HasOne<Folder>().WithMany().HasForeignKey(x => x.FolderId).OnDelete(DeleteBehavior.Restrict);
        document.HasMany(x => x.Tags).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        document.Navigation(x => x.Tags).UsePropertyAccessMode(PropertyAccessMode.Field);
        var tag = model.Entity<DocumentTag>();
        tag.HasKey(x => x.Id);
        tag.Property(x => x.Id).ValueGeneratedNever();
        tag.HasIndex(x => new { x.DocumentId, x.Name }).IsUnique();
    }

    public Task<List<SavedDocument>> DocumentsAsync(Guid owner) =>
        Documents.Include(x => x.Tags).Where(x => x.OwnerId == owner).ToListAsync();
    public Task<SavedDocument?> DocumentAsync(Guid owner, Guid id) =>
        Documents.Include(x => x.Tags).SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == owner);
    public Task<List<Folder>> FoldersAsync(Guid owner) => Folders.Where(x => x.OwnerId == owner).ToListAsync();
    public Task<Folder?> FolderAsync(Guid owner, Guid id) =>
        Folders.SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == owner);
    void IWorkspaceSession.Add(SavedDocument document) => Documents.Add(document);
    void IWorkspaceSession.Add(Folder folder) => Folders.Add(folder);
    void IWorkspaceSession.Remove(SavedDocument document) => Documents.Remove(document);
    void IWorkspaceSession.Remove(Folder folder) => Folders.Remove(folder);
    public async Task CommitAsync()
    {
        try { await SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            throw new WorkspaceException("Не удалось сохранить данные. Возможно, такое имя уже существует или папка была удалена.");
        }
    }
}
