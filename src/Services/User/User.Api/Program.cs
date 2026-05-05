using ApiResponses;
using Messaging.Contracts;
using Messaging.RabbitMQ;
using Serilog;
using User.Application;
using User.Infrastructure;
using User.Infrastructure.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers();
builder.Services.AddBaseResponseValidation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRabbitMqConsumer<UserRegisteredEvent, UserRegisteredConsumer>(builder.Configuration, "user.user.registered.queue");

var app = builder.Build();
await app.Services.EnsureDatabaseCreatedAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseAuthorization();
app.MapControllers();
app.Run();
