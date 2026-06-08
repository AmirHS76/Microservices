using ApiResponses;
using Chat.Api.Consumers;
using Chat.Api.Hubs;
using Chat.Api.Realtime;
using Chat.Application;
using Chat.Infrastructure;
using Chat.Infrastructure.Consumers;
using Messaging.Contracts;
using Messaging.RabbitMQ;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "microservices.sso";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "microservices.clients";
var jwtKey = builder.Configuration["Jwt:Key"] ?? "CHANGE_THIS_SUPER_SECRET_KEY_1234567890";
var cors = builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:4200";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend-Gateway", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();
builder.Services.AddSingleton<IUserConnectionTracker, UserConnectionTracker>();
builder.Services.AddBaseResponseValidation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRabbitMqPublisher(builder.Configuration);
builder.Services.AddRabbitMqConsumer<UserRegisteredEvent, UserRegisteredConsumer>(builder.Configuration, "chat.user.registered.queue");
builder.Services.AddRabbitMqConsumer<ChatMessageQueuedEvent, ChatMessageQueuedConsumer>(builder.Configuration, "chat.message.persist.queue");

var app = builder.Build();
await app.Services.EnsureDatabaseCreatedAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "API v1");
    });
}

app.UseSerilogRequestLogging();
app.UseCors("AllowFrontend-Gateway");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization();
app.Run();
