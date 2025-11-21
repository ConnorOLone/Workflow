using Microsoft.EntityFrameworkCore;
using Workflow.Core.ActivityHandlers;
using Workflow.Core.Engine;
using Workflow.Core.Events;
using Workflow.Core.Interfaces;
using Workflow.Core.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Temporarily disabled due to OpenAPI version conflict
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure database
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase", true);
var connectionString = builder.Configuration.GetConnectionString("WorkflowDatabase");

if (useInMemory || string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddSingleton<IWorkflowRepository, InMemoryWorkflowRepository>();
}
else
{
    builder.Services.AddDbContext<WorkflowDbContext>(options =>
        options.UseSqlServer(connectionString));
    builder.Services.AddScoped<IWorkflowRepository, SqlServerWorkflowRepository>();
}

// Register workflow services
builder.Services.AddSingleton<IWorkflowEventPublisher, WorkflowEventPublisher>();
builder.Services.AddSingleton<IActivityHandlerFactory>(sp =>
{
    var factory = new ActivityHandlerFactory();
    factory.RegisterHandler(new HumanTaskHandler());
    factory.RegisterHandler(new ServiceTaskHandler(sp));
    factory.RegisterHandler(new ScriptTaskHandler());
    factory.RegisterHandler(new DecisionHandler());
    return factory;
});

builder.Services.AddScoped<IWorkflowEngine, WorkflowEngine>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // Temporarily disabled due to OpenAPI version conflict
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Serve the designer UI
app.MapFallbackToFile("index.html");

app.Run();
