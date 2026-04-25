using Homeless.Application;
using Homeless.Infrastructure;
using Homeless.Worker.Maintenance;

var builder = Host.CreateApplicationBuilder(args);

// Add shared Application and Infrastructure DI (like Entity Framework, RabbitMQ, Redis, Identity)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// Register the background worker
builder.Services.AddHostedService<TenantMaintenanceWorker>();

var host = builder.Build();
host.Run();
