using JsonLab.Application;
using Microsoft.EntityFrameworkCore;

namespace JsonLab.Infrastructure;

public sealed class WorkspaceSessionFactory(IDbContextFactory<WorkspaceDbContext> factory) : IWorkspaceSessionFactory
{
    public async Task<IWorkspaceSession> OpenAsync() => await factory.CreateDbContextAsync();
}
