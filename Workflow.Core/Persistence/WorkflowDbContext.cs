using Microsoft.EntityFrameworkCore;
using Workflow.Core.Persistence.Entities;

namespace Workflow.Core.Persistence;

/// <summary>
/// Entity Framework Core DbContext for workflow engine
/// </summary>
public class WorkflowDbContext : DbContext
{
    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options)
        : base(options)
    {
    }

    public DbSet<WorkflowDefinitionEntity> WorkflowDefinitions { get; set; }
    public DbSet<WorkflowInstanceEntity> WorkflowInstances { get; set; }
    public DbSet<ActivityInstanceEntity> ActivityInstances { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // WorkflowDefinition configuration
        modelBuilder.Entity<WorkflowDefinitionEntity>(entity =>
        {
            entity.ToTable("WorkflowDefinitions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.Version)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.ActivitiesJson)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.TransitionsJson)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.VariablesJson)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.MetadataJson)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsActive);
        });

        // WorkflowInstance configuration
        modelBuilder.Entity<WorkflowInstanceEntity>(entity =>
        {
            entity.ToTable("WorkflowInstances");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.BusinessKey)
                .HasMaxLength(200);

            entity.Property(e => e.InitiatedBy)
                .HasMaxLength(200);

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000);

            entity.Property(e => e.VariablesJson)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.MetadataJson)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            entity.HasOne(e => e.WorkflowDefinition)
                .WithMany(e => e.Instances)
                .HasForeignKey(e => e.WorkflowDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ParentWorkflowInstance)
                .WithMany(e => e.ChildWorkflowInstances)
                .HasForeignKey(e => e.ParentWorkflowInstanceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.WorkflowDefinitionId);
            entity.HasIndex(e => e.State);
            entity.HasIndex(e => e.BusinessKey);
            entity.HasIndex(e => e.CreatedAt);
        });

        // ActivityInstance configuration
        modelBuilder.Entity<ActivityInstanceEntity>(entity =>
        {
            entity.ToTable("ActivityInstances");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.AssignedTo)
                .HasMaxLength(200);

            entity.Property(e => e.AssignedToGroup)
                .HasMaxLength(200);

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000);

            entity.Property(e => e.ErrorStackTrace)
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.InputJson)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.OutputJson)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            entity.HasOne(e => e.WorkflowInstance)
                .WithMany(e => e.ActivityInstances)
                .HasForeignKey(e => e.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.WorkflowInstanceId);
            entity.HasIndex(e => e.State);
            entity.HasIndex(e => e.AssignedTo);
            entity.HasIndex(e => e.AssignedToGroup);
        });
    }
}
