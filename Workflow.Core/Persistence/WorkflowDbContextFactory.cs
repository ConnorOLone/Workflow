using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Workflow.Core.Persistence;

/// <summary>
/// Design-time factory for creating WorkflowDbContext during migrations
/// </summary>
public class WorkflowDbContextFactory : IDesignTimeDbContextFactory<WorkflowDbContext>
{
    public WorkflowDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WorkflowDbContext>();

        // Use a default connection string for migrations
        // This can be overridden in the actual application
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=WorkflowEngine;Trusted_Connection=True;MultipleActiveResultSets=true",
            b => b.MigrationsAssembly("Workflow.Core"));

        return new WorkflowDbContext(optionsBuilder.Options);
    }
}
