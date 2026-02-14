# Workflow

A lightweight, extensible .NET workflow engine for orchestrating complex business processes with built-in visual designer and script execution capabilities.

## Overview

Workflow is a business process automation engine that enables the creation, execution, and management of workflow processes in .NET applications. It provides both a fluent API for programmatic workflow creation and a web-based visual designer for building workflows through a drag-and-drop interface.

## Features

### Workflow Definition & Building
- **Fluent API Builder**: Programmatic workflow creation using intuitive builder pattern
- **Visual Designer**: Browser-based drag-and-drop workflow designer
- **Version Management**: Built-in workflow versioning support
- **Extensible Metadata**: Custom metadata storage for workflows and activities

### Activity Types
- **Start/End**: Workflow entry and exit points
- **Human Task**: Manual tasks with user/group assignment
- **Service Task**: Automated service and method invocations
- **Script Task**: Inline C# and PowerShell script execution
- **Decision**: Conditional routing based on expressions
- **Gateway Support**: Parallel and exclusive gateways (in development)

### Workflow Execution
- **State Management**: Track workflow states (NotStarted, Running, Suspended, Completed, Failed, Cancelled)
- **Activity States**: Fine-grained activity state tracking
- **Variable Management**: Global workflow variables with input/output mappings
- **Conditional Transitions**: Route workflows based on business rules
- **Retry Logic**: Configurable retry attempts for failed activities

### Script Execution
- **C# Scripts**: Roslyn-based script execution with caching
- **PowerShell Scripts**: PowerShell SDK integration with security constraints
- **Helper Methods**: Built-in `Get<T>()`, `Set()`, `Has()` methods for variable access
- **Timeout Support**: Configurable execution timeouts
- **Security Controls**: Namespace/assembly whitelisting, cmdlet blacklisting

### Persistence
- **In-Memory Repository**: Development and testing
- **SQL Server Repository**: Production-ready persistence with Entity Framework Core
- **Database Migrations**: Included EF Core migrations

### Event System
- **Event Publishing**: Workflow and activity lifecycle events
- **Custom Handlers**: Register custom event handlers for logging and monitoring
- **Event Types**: WorkflowStarted, WorkflowCompleted, ActivityStarted, ActivityFailed, etc.

## Project Structure

```
Workflow/
├── Workflow.Core/           # Core workflow engine library
├── Workflow.Core.Tests/     # Unit tests
├── Workflow.Designer/       # ASP.NET Core web-based designer
└── SampleTest001/          # Sample workflow implementation
```

## Getting Started

### Prerequisites
- .NET 9.0 SDK or later
- SQL Server (optional, for persistent storage)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/ConnorOLone/Workflow.git
cd Workflow
```

2. Build the solution:
```bash
dotnet build
```

3. Run tests:
```bash
dotnet test
```

### Quick Example

```csharp
using Workflow.Core;
using Workflow.Core.Builder;

// Create a simple approval workflow
var builder = new WorkflowBuilder()
    .WithName("Purchase Order Approval")
    .WithVersion("1.0.0");

builder.AddStartActivity("Start");

builder.AddHumanTask("Manager Approval")
    .WithAssignee("manager@company.com")
    .WithDescription("Review and approve purchase order");

builder.AddDecision("Check Amount")
    .WithCondition("amount > 10000");

builder.AddHumanTask("Finance Approval")
    .WithAssignee("finance@company.com")
    .WithDescription("Finance review for high-value orders");

builder.AddServiceTask("Update ERP")
    .WithServiceName("ERPService")
    .WithMethodName("CreatePurchaseOrder");

builder.AddEndActivity("End");

// Define transitions
builder.AddTransition("Start", "Manager Approval");
builder.AddTransition("Manager Approval", "Check Amount");
builder.AddTransition("Check Amount", "Finance Approval")
    .When("amount > 10000");
builder.AddTransition("Check Amount", "Update ERP")
    .When("amount <= 10000");
builder.AddTransition("Finance Approval", "Update ERP");
builder.AddTransition("Update ERP", "End");

var workflowDefinition = builder.Build();

// Execute the workflow
var engine = new WorkflowEngine(repository, eventPublisher);
var instance = await engine.StartWorkflowAsync(workflowDefinition, 
    new Dictionary<string, object> { ["amount"] = 15000 });
```

## Workflow Designer

The Workflow.Designer component provides a web-based interface for creating and managing workflows:

```bash
cd Workflow.Designer
dotnet run
```

Navigate to `https://localhost:5001` to access the designer.

### Designer Features
- Drag-and-drop workflow canvas
- Activity toolbox with all available activity types
- Property editors for configuring activities
- Monaco Editor integration for script editing
- Light/dark theme support
- REST API for workflow management

## Architecture

### Core Components

- **WorkflowEngine**: Main execution engine orchestrating workflow execution
- **WorkflowDefinition**: Blueprint/template for workflow processes
- **WorkflowInstance**: Runtime instance with state management
- **ActivityDefinition**: Template for workflow steps
- **ActivityInstance**: Runtime activity execution state
- **Transition**: Flow definition between activities with conditions

### Design Patterns

- Factory Pattern (ActivityHandlerFactory, ScriptExecutorFactory)
- Builder Pattern (WorkflowBuilder)
- Repository Pattern (IWorkflowRepository)
- Strategy Pattern (IActivityHandler implementations)
- Observer Pattern (Event publishing)

## Technology Stack

- .NET 9.0
- Entity Framework Core 9.0
- ASP.NET Core
- Monaco Editor
- Microsoft.CodeAnalysis.CSharp.Scripting (Roslyn)
- Microsoft.PowerShell.SDK

## Use Cases

- Document approval processes
- Purchase order workflows
- Multi-step business processes
- Automated task orchestration
- Integration workflows between systems
- Human-in-the-loop automation

## Security

The workflow engine includes security features for script execution:

- Configurable execution timeouts
- Optional file system access control
- Optional network access control
- C# namespace/assembly whitelisting
- PowerShell cmdlet blacklisting
- Constrained language mode for PowerShell

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License.

## Contact

Connor O'Lone - [GitHub](https://github.com/ConnorOLone)
