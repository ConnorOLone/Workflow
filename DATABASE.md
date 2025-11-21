# SQL Server Database Backend

The workflow engine now supports persistent storage using SQL Server with Entity Framework Core.

## Features

- **Persistent Workflow Storage**: All workflow definitions, instances, and activity instances are stored in SQL Server
- **Automatic Migrations**: Database schema is automatically created on first run
- **Flexible Configuration**: Switch between in-memory and SQL Server backends via configuration
- **Full Transaction Support**: All operations are transactional and ACID-compliant

## Database Schema

The workflow engine uses three main tables:

### WorkflowDefinitions
Stores workflow blueprints/templates:
- Id (PK)
- Name, Description, Version
- StartActivityId
- ActivitiesJson (serialized activity definitions)
- TransitionsJson (serialized transitions)
- VariablesJson (default variables)
- MetadataJson
- IsActive flag

### WorkflowInstances
Stores runtime workflow executions:
- Id (PK)
- WorkflowDefinitionId (FK)
- State (NotStarted, Running, Suspended, Completed, Failed, Cancelled)
- CreatedAt, StartedAt, CompletedAt
- InitiatedBy, BusinessKey
- VariablesJson (runtime variables)
- ParentWorkflowInstanceId (for sub-workflows)

### ActivityInstances
Stores individual activity executions:
- Id (PK)
- WorkflowInstanceId (FK)
- ActivityDefinitionId
- State (Ready, Running, WaitingForInput, Completed, Failed, etc.)
- AssignedTo, AssignedToGroup
- InputJson, OutputJson
- Timestamps and error information

## Configuration

### Using In-Memory Storage (Default)

```json
{
  "UseInMemoryDatabase": true
}
```

### Using SQL Server

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "WorkflowDatabase": "Server=(localdb)\\mssqllocaldb;Database=WorkflowEngine;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "UseInMemoryDatabase": false
}
```

#### Connection String Examples

**LocalDB (Development)**:
```
Server=(localdb)\\mssqllocaldb;Database=WorkflowEngine;Trusted_Connection=True;MultipleActiveResultSets=true
```

**SQL Server with Windows Authentication**:
```
Server=localhost;Database=WorkflowEngine;Trusted_Connection=True;MultipleActiveResultSets=true
```

**SQL Server with SQL Authentication**:
```
Server=localhost;Database=WorkflowEngine;User Id=workflowuser;Password=your_password;MultipleActiveResultSets=true
```

**Azure SQL Database**:
```
Server=tcp:yourserver.database.windows.net,1433;Initial Catalog=WorkflowEngine;Persist Security Info=False;User ID=yourusername;Password=yourpassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

## Database Migrations

### Create a New Migration

```bash
cd Workflow.Core
dotnet ef migrations add MigrationName --output-dir Persistence/Migrations
```

### Apply Migrations

The application automatically applies migrations on startup using `EnsureCreatedAsync()`.

For production, use explicit migrations:

```bash
cd Workflow.Core
dotnet ef database update
```

### Rollback Migration

```bash
cd Workflow.Core
dotnet ef database update PreviousMigrationName
```

### Generate SQL Script

```bash
cd Workflow.Core
dotnet ef migrations script --output migration.sql
```

## Usage in Code

### Setting Up SQL Server Repository

```csharp
using Microsoft.EntityFrameworkCore;
using Workflow.Core.Persistence;

// Configure DbContext
var optionsBuilder = new DbContextOptionsBuilder<WorkflowDbContext>();
optionsBuilder.UseSqlServer(connectionString);

var dbContext = new WorkflowDbContext(optionsBuilder.Options);

// Ensure database is created
await dbContext.Database.EnsureCreatedAsync();

// Create repository
var repository = new SqlServerWorkflowRepository(dbContext);

// Use with workflow engine
var engine = new WorkflowEngine(repository, activityHandlerFactory, eventPublisher);
```

### Switching Repositories

The workflow engine uses the `IWorkflowRepository` interface, making it easy to switch between implementations:

```csharp
IWorkflowRepository repository;

if (useInMemory)
{
    repository = new InMemoryWorkflowRepository();
}
else
{
    var dbContext = new WorkflowDbContext(options);
    repository = new SqlServerWorkflowRepository(dbContext);
}
```

## Performance Considerations

### Indexing
The schema includes indexes on:
- WorkflowDefinitions: Name, IsActive
- WorkflowInstances: WorkflowDefinitionId, State, BusinessKey, CreatedAt
- ActivityInstances: WorkflowInstanceId, State, AssignedTo, AssignedToGroup

### JSON Storage
Complex objects (Activities, Transitions, Variables) are stored as JSON in `nvarchar(max)` columns for flexibility.

For high-performance scenarios, consider:
- Creating computed columns with indexes on frequently queried JSON properties
- Using JSON functions in queries (SQL Server 2016+)
- Implementing table-per-type for activities if needed

### Connection Pooling
Entity Framework Core automatically uses connection pooling. Configure pool size in connection string:
```
Server=...;Min Pool Size=10;Max Pool Size=100
```

## Troubleshooting

### "Cannot open database" Error
Ensure SQL Server is running and connection string is correct.

For LocalDB:
```bash
sqllocaldb start mssqllocaldb
sqllocaldb info mssqllocaldb
```

### Migration Issues
Delete database and recreate:
```bash
cd Workflow.Core
dotnet ef database drop
dotnet ef database update
```

### Performance Issues
Enable SQL logging:
```csharp
optionsBuilder.UseSqlServer(connectionString)
    .LogTo(Console.WriteLine, LogLevel.Information)
    .EnableSensitiveDataLogging();
```

## Security

### Best Practices
1. **Use Windows Authentication** in production when possible
2. **Store connection strings securely** (Azure Key Vault, User Secrets)
3. **Limit database user permissions** (principle of least privilege)
4. **Enable encryption** for sensitive data
5. **Use parameterized queries** (EF Core does this automatically)

### User Secrets (Development)
```bash
cd SampleTest001
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:WorkflowDatabase" "your-connection-string"
```

## Production Deployment

### Recommended Configuration
1. Use a dedicated database user with minimal permissions
2. Enable connection pooling
3. Configure appropriate timeouts
4. Enable query logging for troubleshooting
5. Set up database backups
6. Monitor database performance

### Example Production Connection String
```json
{
  "ConnectionStrings": {
    "WorkflowDatabase": "Server=prod-sql-server;Database=WorkflowEngine;User Id=workflow_app;Password=${WORKFLOW_DB_PASSWORD};MultipleActiveResultSets=true;Connection Timeout=30;Min Pool Size=10;Max Pool Size=100"
  }
}
```

## Backup and Recovery

### Backup Script
```sql
BACKUP DATABASE WorkflowEngine
TO DISK = 'C:\\Backups\\WorkflowEngine_Full.bak'
WITH FORMAT, INIT, NAME = 'Full Backup of WorkflowEngine';
```

### Restore Script
```sql
RESTORE DATABASE WorkflowEngine
FROM DISK = 'C:\\Backups\\WorkflowEngine_Full.bak'
WITH REPLACE;
```
