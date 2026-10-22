using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JsonLab.Infrastructure;

public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<WorkspaceDbContext>
{
    public WorkspaceDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<WorkspaceDbContext>().UseSqlite("Data Source=jsonlab-design.db").Options);
}
