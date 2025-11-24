using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Workflow.Core.Builder;
using Workflow.Core.Engine;
using Workflow.Core.Events;
using Workflow.Core.Interfaces;
using Workflow.Core.Models;
using Workflow.Core.Persistence;
using Workflow.Core.ActivityHandlers;
using Workflow.Core.Scripting;

namespace SampleTest001;

/// <summary>
/// Sample demonstrating the Workflow Engine with a Purchase Order Approval Process
/// Similar to common business process automation scenarios in systems like Kofax Total Agility
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Workflow Engine Demo - Purchase Order Approval Process ===\n");

        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var useInMemory = configuration.GetValue<bool>("UseInMemoryDatabase", true);
        var connectionString = configuration.GetConnectionString("WorkflowDatabase");

        // Setup repository based on configuration
        IWorkflowRepository repository;
        WorkflowDbContext? dbContext = null;

        if (useInMemory || string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Using In-Memory Repository\n");
            repository = new InMemoryWorkflowRepository();
        }
        else
        {
            Console.WriteLine($"Using SQL Server Repository\n");
            Console.WriteLine($"Connection: {MaskConnectionString(connectionString)}\n");

            var optionsBuilder = new DbContextOptionsBuilder<WorkflowDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            dbContext = new WorkflowDbContext(optionsBuilder.Options);

            // Ensure database is created and migrations are applied
            Console.WriteLine("Ensuring database is created...");
            await dbContext.Database.EnsureCreatedAsync();
            Console.WriteLine("Database ready!\n");

            repository = new SqlServerWorkflowRepository(dbContext);
        }

        var eventPublisher = new WorkflowEventPublisher();
        var activityHandlerFactory = new ActivityHandlerFactory();
        var scriptExecutorFactory = new ScriptExecutorFactory();

        // Register custom activity handlers
        activityHandlerFactory.RegisterHandler(new HumanTaskHandler());
        activityHandlerFactory.RegisterHandler(new ServiceTaskHandler(null!));
        activityHandlerFactory.RegisterHandler(new ScriptTaskHandler(scriptExecutorFactory));
        activityHandlerFactory.RegisterHandler(new DecisionHandler());

        // Register event handler for logging
        eventPublisher.RegisterEventHandler(new ConsoleEventHandler());

        var engine = new WorkflowEngine(repository, activityHandlerFactory, eventPublisher);

        // Build a Purchase Order Approval Workflow
        var workflowDef = BuildPurchaseOrderWorkflow();
        await repository.SaveWorkflowDefinitionAsync(workflowDef);

        Console.WriteLine($"Created workflow: {workflowDef.Name} (v{workflowDef.Version})");
        Console.WriteLine($"Activities: {workflowDef.Activities.Count}");
        Console.WriteLine($"Transitions: {workflowDef.Transitions.Count}\n");

        // Start a workflow instance with a purchase order
        Console.WriteLine("--- Starting Purchase Order Workflow ---\n");

        var initialVariables = new Dictionary<string, object?>
        {
            ["purchaseOrderId"] = "PO-12345",
            ["amount"] = 5000m,
            ["requestedBy"] = "john.doe@company.com",
            ["vendor"] = "Acme Corporation",
            ["description"] = "Office supplies and equipment"
        };

        var instance = await engine.StartWorkflowAsync(
            workflowDef.Id,
            initialVariables,
            "john.doe@company.com"
        );

        Console.WriteLine($"\nWorkflow Instance ID: {instance.Id}");
        Console.WriteLine($"Current State: {instance.State}");
        Console.WriteLine($"Active Activities: {instance.ActiveActivities.Count}");

        // Simulate completing the manager approval (human task)
        Console.WriteLine("\n--- Simulating Manager Approval ---");
        var managerApprovalActivity = instance.ActiveActivities.FirstOrDefault();
        if (managerApprovalActivity != null)
        {
            Console.WriteLine($"Activity: {workflowDef.Activities.First(a => a.Id == managerApprovalActivity.ActivityDefinitionId).Name}");
            Console.WriteLine($"Assigned to: {managerApprovalActivity.AssignedToGroup}");

            await engine.CompleteActivityAsync(
                managerApprovalActivity.Id,
                new Dictionary<string, object?> { ["approved"] = true, ["comments"] = "Approved by manager" },
                "manager@company.com"
            );
        }

        // Check workflow state
        instance = await engine.GetWorkflowInstanceAsync(instance.Id) ?? instance;
        Console.WriteLine($"\nWorkflow State: {instance.State}");
        Console.WriteLine($"Active Activities: {instance.ActiveActivities.Count}");

        // Display workflow history
        Console.WriteLine("\n--- Workflow Execution History ---");
        foreach (var activity in instance.History)
        {
            var activityDef = workflowDef.Activities.FirstOrDefault(a => a.Id == activity.ActivityDefinitionId);
            Console.WriteLine($"✓ {activityDef?.Name ?? "Unknown"} - {activity.State} (Duration: {(activity.CompletedAt - activity.StartedAt)?.TotalSeconds:F2}s)");
        }

        Console.WriteLine("\n--- Demo Complete ---");
        Console.WriteLine("\nThis workflow engine supports:");
        Console.WriteLine("• Multiple activity types (Human Tasks, Service Tasks, Script Tasks, Decisions)");
        Console.WriteLine("• State management and persistence");
        Console.WriteLine("• Event-driven architecture");
        Console.WriteLine("• Fluent workflow builder API");
        Console.WriteLine("• Retry and error handling");
        Console.WriteLine("• Sub-workflows and parallel execution");
    }

    private static WorkflowDefinition BuildPurchaseOrderWorkflow()
    {
        var builder = new WorkflowBuilder("Purchase Order Approval", "1.0.0")
            .WithDescription("Automated approval workflow for purchase orders")
            .WithVariable("purchaseOrderId", null)
            .WithVariable("amount", 0m)
            .WithVariable("requestedBy", null)
            .WithVariable("vendor", null)
            .WithVariable("description", null)
            .WithVariable("approved", false)
            .WithVariable("finalApproved", false);

        // Start
        builder.AddStartActivity("Start")
            .WithDescription("Workflow start point")
            .ThenHumanTask("Manager Approval")
            .WithDescription("Manager reviews and approves purchase order")
            .AssignToGroup("Managers")
            .WithInputMapping("purchaseOrderId", "purchaseOrderId")
            .WithInputMapping("amount", "amount")
            .WithInputMapping("vendor", "vendor")
            .WithOutputMapping("approved", "approved")
            .WithTimeout(86400) // 24 hours
            .ThenDecision("Check Amount")
            .WithDescription("Determine if finance approval is needed")
            .WithConfiguration("decisionExpression", "amount > 1000")
            .EndActivity();

        // Finance Approval (for high amounts)
        builder.AddHumanTask("Finance Approval")
            .WithDescription("Finance team approves high-value purchases")
            .AssignToGroup("Finance")
            .WithInputMapping("purchaseOrderId", "purchaseOrderId")
            .WithInputMapping("amount", "amount")
            .WithOutputMapping("approved", "finalApproved")
            .ThenServiceTask("Create Purchase Order")
            .WithDescription("Create purchase order in ERP system")
            .WithService("ERPService", "CreatePurchaseOrder")
            .WithInputMapping("purchaseOrderId", "purchaseOrderId")
            .WithInputMapping("amount", "amount")
            .WithInputMapping("vendor", "vendor")
            .WithRetry(3)
            .ThenServiceTask("Notify Requester")
            .WithDescription("Send approval notification to requester")
            .WithService("NotificationService", "SendEmail")
            .WithInputMapping("requestedBy", "requestedBy")
            .ThenEnd()
            .WithDescription("Workflow completed successfully")
            .EndActivity();

        // Transitions
        builder.AddTransition("Check Amount", "Finance Approval")
            .WithName("High Value")
            .When("amount > 1000")
            .WithPriority(1);

        builder.AddTransition("Check Amount", "Create Purchase Order")
            .WithName("Standard Value")
            .AsDefault();

        return builder.Build();
    }

    private static string MaskConnectionString(string connectionString)
    {
        // Simple masking to hide sensitive info in connection strings
        var parts = connectionString.Split(';');
        var masked = parts.Select(part =>
        {
            if (part.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                part.Contains("Pwd", StringComparison.OrdinalIgnoreCase))
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    return $"{keyValue[0]}=***";
                }
            }
            return part;
        });
        return string.Join(";", masked);
    }
}

/// <summary>
/// Simple event handler that logs to console
/// </summary>
public class ConsoleEventHandler : IWorkflowEventHandler
{
    public Task HandleEventAsync(WorkflowEvent @event)
    {
        var timestamp = @event.Timestamp.ToString("HH:mm:ss");
        Console.WriteLine($"[{timestamp}] {@event.EventType} - Workflow: {@event.WorkflowInstanceId}");
        return Task.CompletedTask;
    }
}