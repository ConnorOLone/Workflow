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

## 📜 Script Task Execution

The workflow engine supports executing scripts in multiple languages with full security sandboxing and variable access.

### **Supported Languages**

#### **PowerShell**
- Full PowerShell 7.4 support
- Constrained language mode for security
- Access workflow variables via `$variableName`
- Automatic variable modification tracking

**Example PowerShell Script:**
```powershell
# Access workflow variables
$total = $orderAmount * $quantity

# Apply business logic
if ($total -gt 1000) {
    $requiresApproval = $true
} else {
    $requiresApproval = $false
}

# Modify variables (automatically captured back to workflow)
$calculatedTotal = $total * 1.1  # Add 10% markup

# Return value
$calculatedTotal
```

#### **C# (Roslyn)**
- Full C# 12 support with Roslyn scripting
- Access variables via `Variables` dictionary or helper methods
- Supports LINQ and modern C# features
- Script compilation caching for performance

**Example C# Script:**
```csharp
// Access workflow variables using Variables dictionary
var orderAmount = (decimal)Variables["orderAmount"];
var quantity = (int)Variables["quantity"];
var total = orderAmount * quantity;

// Use helper methods for type-safe access
var customerType = Get<string>("customerType");

// Apply business logic with LINQ
var items = Variables["items"] as List<string>;
var filteredItems = items.Where(x => x.StartsWith("A")).ToList();

// Modify variables
Set("filteredItems", filteredItems);
Set("total", total);

// Return value
return total > 1000;
```

### **Variable Access**

Scripts have full access to workflow variables and activity inputs:

| Language | Read Variable | Write Variable | Type Conversion |
|----------|--------------|----------------|-----------------|
| PowerShell | `$variableName` | `$variableName = value` | Automatic |
| C# | `Variables["name"]` | `Variables["name"] = value` | Manual cast |
| C# (Helper) | `Get<T>("name")` | `Set("name", value)` | Automatic |

### **Security & Sandboxing**

Scripts run in secure sandboxed environments by default:

**PowerShell:**
- Constrained language mode enabled
- Dangerous cmdlets blocked: `Invoke-Expression`, `Start-Process`, etc.
- File system cmdlets removed by default
- Network cmdlets removed by default

**C#:**
- Whitelist-only assemblies and namespaces
- No file I/O by default (`System.IO` not imported)
- No network access by default (`System.Net` not imported)
- No process execution (`System.Diagnostics.Process` blocked)

### **Configuration Options**

Configure script execution via activity configuration:

```json
{
  "script": "return Variables[\"x\"] + Variables[\"y\"];",
  "language": "csharp",
  "timeoutSeconds": 30,
  "allowFileSystem": false,
  "allowNetwork": false,
  "allowedNamespaces": "System.Text.Json,System.Xml",
  "allowedAssemblies": "System.Text.Json.dll"
}
```

**Available Options:**

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `script` | string | *required* | The script code to execute |
| `language` | string | `"csharp"` | Script language: `powershell`, `csharp`, `ps`, `cs` |
| `timeoutSeconds` | int | `30` | Maximum execution time |
| `allowFileSystem` | bool | `false` | Allow file system access |
| `allowNetwork` | bool | `false` | Allow network access |
| `allowedNamespaces` | string | `""` | Comma-separated namespaces (C# only) |
| `allowedAssemblies` | string | `""` | Comma-separated assemblies (C# only) |

### **Workflow Builder Example**

```csharp
var workflow = new WorkflowBuilder("Order Processing", "1.0.0")
    .WithVariable("orderAmount", 0m)
    .WithVariable("quantity", 0)

    .AddStartActivity("Start")
        .ThenScriptTask("Calculate Total")

    .AddScriptTask("Calculate Total")
        .WithConfiguration("language", "csharp")
        .WithConfiguration("script", @"
            var amount = (decimal)Variables[""orderAmount""];
            var qty = (int)Variables[""quantity""];
            var total = amount * qty;
            Set(""total"", total);
            return total;
        ")
        .ThenEnd()

    .Build();
```

### **Return Values & Output**

Script execution results are available in activity output:

```csharp
{
    "scriptResult": <return_value>,           // Last output or returned value
    "executionTime": 123.45,                  // Execution time in milliseconds
    "language": "csharp",                     // Language used
    "modified_variableName": <new_value>      // Each modified variable
}
```

Modified variables are automatically updated in the workflow context.

### **Error Handling**

Scripts that fail return detailed error information:

```csharp
{
    "Success": false,
    "ErrorMessage": "Compilation error: CS1002: ; expected",
    "ErrorStackTrace": "..."
}
```

**Common Errors:**
- **Compilation errors** (C#): Syntax errors, missing semicolons, unknown types
- **Runtime errors**: Null references, divide by zero, invalid casts
- **Timeout errors**: Script exceeded `timeoutSeconds` limit
- **Security errors**: Attempted to use blocked cmdlets or namespaces

### **Performance Considerations**

1. **C# Script Caching**: Compiled scripts are cached for repeat executions
2. **PowerShell Overhead**: Each execution creates a new runspace (slower than C#)
3. **Recommendation**: Use C# for compute-intensive operations, PowerShell for simple logic

### **Best Practices**

1. **Keep scripts short** - Complex logic should be in service tasks or activities
2. **Use timeouts** - Always set reasonable timeout values
3. **Handle nulls** - Check for null variables before using them
4. **Test scripts** - Test scripts in isolation before adding to workflows
5. **Document scripts** - Add comments explaining business logic
6. **Prefer C# for complex logic** - Better performance and type safety
7. **Use PowerShell for simple tasks** - Quick variable manipulation and decisions

### **Example Use Cases**

**1. Dynamic Pricing Calculation:**
```csharp
var basePrice = Get<decimal>("basePrice");
var customerType = Get<string>("customerType");
var discount = customerType == "Premium" ? 0.2m : 0.1m;
var finalPrice = basePrice * (1 - discount);
Set("finalPrice", finalPrice);
return finalPrice;
```

**2. Data Validation:**
```powershell
$email = $userEmail
$isValid = $email -match '^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$'
$isValid
```

**3. Conditional Routing:**
```csharp
var items = Variables["orderItems"] as List<object>;
var total = Get<decimal>("orderTotal");
return items.Count > 10 || total > 5000;  // Route to special handling
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
