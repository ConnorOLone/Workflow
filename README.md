# Workflow Engine - Complete Business Process Automation Platform

A modern, enterprise-grade workflow engine for .NET 9.0, similar to Kofax Total Agility, with visual designer and SQL Server persistence.

## 🎯 Overview

This workflow engine provides complete business process automation capabilities including:
- Visual workflow designer with drag-and-drop interface
- Multiple activity types (Human Tasks, Service Tasks, Scripts, Decisions)
- Persistent storage with SQL Server or in-memory
- RESTful API for workflow management
- Event-driven architecture
- State management and execution tracking

## 📁 Project Structure

```
Workflow/
├── Workflow.Core/                  # Core workflow engine library
│   ├── Models/                     # Domain models
│   ├── Interfaces/                 # Abstractions
│   ├── Engine/                     # Execution engine
│   ├── ActivityHandlers/           # Activity type implementations
│   ├── Persistence/                # Repository implementations
│   │   ├── Entities/              # EF Core entities
│   │   └── Migrations/            # Database migrations
│   ├── Events/                     # Event system
│   └── Builder/                    # Fluent API
│
├── Workflow.Designer/              # Web-based visual designer
│   ├── Controllers/                # REST API endpoints
│   └── wwwroot/                    # Frontend assets
│       ├── index.html
│       ├── css/
│       └── js/
│
└── SampleTest001/                  # Console demo application
    └── Program.cs
```

## 🚀 Quick Start

### **1. Run the Visual Designer**

```bash
cd Workflow.Designer
dotnet run
```

Open browser to: **http://localhost:5000**

### **2. Create a Workflow**

1. Drag activities from the toolbox onto the canvas
2. Right-click to connect activities
3. Configure properties for each activity
4. Save the workflow

### **3. Start a Workflow Instance**

Click "Start Instance" button and provide:
- Initial variables (JSON)
- Initiator information

### **4. Monitor Execution**

Check the API at: **http://localhost:5000/swagger**

## 📚 Documentation

- [**DESIGNER.md**](DESIGNER.md) - Visual designer usage guide
- [**DATABASE.md**](DATABASE.md) - SQL Server setup and configuration
- [**API Documentation**](http://localhost:5000/swagger) - OpenAPI/Swagger UI

## ✨ Key Features

### **Workflow Definition**
- **Activity Types**: Start, End, Human Task, Service Task, Script Task, Decision
- **Transitions**: Conditional routing between activities
- **Variables**: Global workflow state
- **Versioning**: Track workflow versions
- **Metadata**: Extensible configuration

### **Execution Engine**
- **State Management**: Track workflow and activity states
- **Event Publishing**: Hooks for monitoring and integration
- **Retry Logic**: Automatic retry on failures
- **Timeout Support**: Activity-level timeouts
- **Error Handling**: Comprehensive error management

### **Persistence**
- **SQL Server**: Production-ready persistence
- **In-Memory**: Fast development and testing
- **EF Core**: Entity Framework Core integration
- **Migrations**: Database version management

### **Visual Designer**
- **HTML5 Canvas**: Modern, responsive UI
- **Drag & Drop**: Intuitive workflow creation
- **Properties Panel**: Real-time configuration
- **Context Menus**: Quick actions
- **Workflow Library**: Save and load workflows

## 🏗️ Architecture

### **Domain Models**

```csharp
WorkflowDefinition (Blueprint)
├── Activities[]
│   ├── Type (Start, HumanTask, ServiceTask, etc.)
│   ├── Configuration
│   ├── InputMappings
│   └── OutputMappings
└── Transitions[]
    ├── FromActivityId
    ├── ToActivityId
    └── Condition

WorkflowInstance (Runtime)
├── State (Running, Completed, Failed, etc.)
├── Variables
├── ActiveActivities[]
└── History[]
```

### **Core Interfaces**

```csharp
IWorkflowEngine          // Main execution engine
IWorkflowRepository      // Data persistence
IActivityHandler         // Activity type handler
IWorkflowEventPublisher  // Event system
IActivityHandlerFactory  // Handler registration
```

## 🔧 Configuration

### **Connection String**

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "WorkflowDatabase": "Server=localhost;Database=WorkflowEngine;Trusted_Connection=True"
  },
  "UseInMemoryDatabase": false
}
```

### **In-Memory vs SQL Server**

```csharp
// In-Memory (Development)
builder.Services.AddSingleton<IWorkflowRepository, InMemoryWorkflowRepository>();

// SQL Server (Production)
builder.Services.AddDbContext<WorkflowDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddScoped<IWorkflowRepository, SqlServerWorkflowRepository>();
```

## 💻 Code Examples

### **Creating a Workflow Programmatically**

```csharp
var workflow = new WorkflowBuilder("Purchase Order Approval", "1.0.0")
    .WithDescription("Automated approval workflow")
    .WithVariable("amount", 0m)
    .WithVariable("approved", false)

    .AddStartActivity("Start")
        .ThenHumanTask("Manager Approval")
        .AssignToGroup("Managers")
        .WithTimeout(86400)
        .ThenDecision("Check Amount")

    .AddDecision("Check Amount")
        .WithConfiguration("expression", "amount > 1000")
        .EndActivity()

    .AddHumanTask("Finance Approval")
        .AssignToGroup("Finance")
        .ThenEnd()

    .AddTransition("Check Amount", "Finance Approval")
        .When("amount > 1000")
        .WithPriority(1)
        .EndTransition()

    .Build();
```

### **Starting a Workflow**

```csharp
var instance = await engine.StartWorkflowAsync(
    workflowDefinitionId,
    new Dictionary<string, object?>
    {
        ["purchaseOrderId"] = "PO-12345",
        ["amount"] = 5000m,
        ["requestedBy"] = "john.doe@company.com"
    },
    initiatedBy: "john.doe@company.com"
);
```

### **Completing a Human Task**

```csharp
await engine.CompleteActivityAsync(
    activityInstanceId,
    new Dictionary<string, object?>
    {
        ["approved"] = true,
        ["comments"] = "Approved"
    },
    completedBy: "manager@company.com"
);
```

## 🌐 REST API

### **Workflow Definitions**

```http
GET    /api/workflowdefinitions
POST   /api/workflowdefinitions
GET    /api/workflowdefinitions/{id}
PUT    /api/workflowdefinitions/{id}
DELETE /api/workflowdefinitions/{id}
```

### **Workflow Instances**

```http
POST /api/workflowinstances/start
GET  /api/workflowinstances/{id}
POST /api/workflowinstances/{id}/suspend
POST /api/workflowinstances/{id}/resume
POST /api/workflowinstances/{id}/cancel
POST /api/workflowinstances/activities/{id}/complete
```

## 🎨 Activity Types

| Type | Icon | Purpose | Waits for Input |
|------|------|---------|----------------|
| Start | ▶️ | Entry point | No |
| End | ⏹️ | Exit point | No |
| HumanTask | 👤 | Manual approval/review | Yes |
| ServiceTask | ⚙️ | API calls, external services | No |
| ScriptTask | 📝 | Inline C#/JavaScript code | No |
| Decision | 🔀 | Conditional routing | No |
| ParallelGateway | ➕ | Concurrent execution | No |
| ExclusiveGateway | ❌ | Exclusive choice | No |

## 📊 Database Schema

**WorkflowDefinitions**
- Stores workflow blueprints
- JSON-serialized activities and transitions

**WorkflowInstances**
- Runtime workflow executions
- Current state and variables
- Execution history

**ActivityInstances**
- Individual activity executions
- Input/output data
- Assignments and timestamps

## 🔐 Security Considerations

1. **Authentication**: Add JWT/OAuth to API endpoints
2. **Authorization**: Implement role-based access control
3. **Input Validation**: Validate all user inputs
4. **SQL Injection**: EF Core uses parameterized queries
5. **XSS Protection**: Sanitize HTML in designer
6. **Connection Strings**: Store securely (Azure Key Vault, User Secrets)

## 🚦 Performance

### **Optimization Tips**
- Use SQL Server for production (not in-memory)
- Enable connection pooling
- Index frequently queried fields
- Consider caching for workflow definitions
- Use async/await throughout
- Monitor with Application Insights

### **Scalability**
- Horizontally scale web API with load balancer
- Use distributed cache (Redis) for state
- Consider message queue for long-running workflows
- Implement workflow partitioning for high volume

## 🧪 Testing

### **Unit Tests**
```bash
cd Workflow.Core.Tests
dotnet test
```

### **Integration Tests**
```bash
cd Workflow.Designer.Tests
dotnet test
```

## 📦 Deployment

### **Docker**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Workflow.Designer.dll"]
```

### **Azure App Service**
```bash
dotnet publish -c Release
az webapp deploy --resource-group myResourceGroup --name myapp --src-path ./publish.zip
```

### **Kubernetes**
See `k8s/` folder for deployment manifests

## 🛠️ Development

### **Prerequisites**
- .NET 9.0 SDK
- SQL Server (or LocalDB)
- Node.js (for frontend tooling, optional)
- Visual Studio 2022 or VS Code

### **Building**
```bash
dotnet build
```

### **Running Tests**
```bash
dotnet test
```

### **Database Migrations**
```bash
cd Workflow.Core
dotnet ef migrations add MigrationName
dotnet ef database update
```

## 📝 Roadmap

- [x] Core workflow engine
- [x] Activity handlers (Human, Service, Script, Decision)
- [x] State management and persistence
- [x] Event system
- [x] Fluent API builder
- [x] SQL Server backend
- [x] Visual designer UI
- [x] REST API
- [ ] Authentication & authorization
- [ ] Workflow versioning UI
- [ ] Real-time execution monitoring
- [ ] Parallel gateway support
- [ ] Sub-workflow execution
- [ ] Timer-based triggers
- [ ] SLA and deadline management
- [ ] Workflow templates library
- [ ] Mobile-responsive designer
- [ ] Audit logging
- [ ] Performance metrics dashboard

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## 📄 License

MIT License - See LICENSE file for details

## 🙏 Acknowledgments

Inspired by:
- Kofax Total Agility
- Camunda BPM
- Windows Workflow Foundation

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/yourrepo/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourrepo/discussions)
- **Documentation**: See `docs/` folder

## 🎓 Examples

Check the `examples/` folder for:
- Purchase Order Approval
- Employee Onboarding
- Document Review Process
- Customer Support Ticket
- Invoice Processing

---

**Made with ❤️ using .NET 9.0**
