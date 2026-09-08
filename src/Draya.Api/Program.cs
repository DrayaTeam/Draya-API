using Draya.Api.Middleware;
using Draya.Application;
using Draya.Infrastructure;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    typeof(Draya.Application.DependencyInjection).Assembly,
    typeof(Program).Assembly
));
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // Explicitly list allowed origins.
        // Local dev (port 4200 / any localhost) + Vercel production frontend.
        // NOTE: WebSocket-based SignalR transports (wss://) also require the API to be
        // accessible via HTTPS from the browser. Until a TLS certificate is provisioned
        // on the API host, the frontend must keep using LongPolling as its SignalR fallback
        // or connect directly to https://draya-api.<domain>/hubs/... once HTTPS is live.
        policy
            .WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200",
                "http://localhost:3000",
                "https://localhost:3000",
                "https://draya-lms.vercel.app"
            )
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Draya API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT token below (no need to type 'Bearer ' prefix)."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<Draya.Api.Notifications.MaterialNotificationHub>("/hubs/materials");
app.MapHub<Draya.Api.Notifications.ClassroomQaHub>("/hubs/qa");
app.MapHub<Draya.Api.Notifications.ExamGenerationHub>("/hubs/exam-generation");
app.MapHub<Draya.Api.Notifications.ExamGradingHub>("/hubs/exam-grading");
app.MapHub<Draya.Api.Notifications.ReportsNotificationHub>("/hubs/reports");
app.MapHub<Draya.Api.Notifications.NotificationHub>("/hubs/notifications");

// Seed SuperAdmin user on startup
try
{
    await Draya.Infrastructure.Persistence.AdminSeeder.SeedSuperAdminAsync(app.Services);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while seeding the database on startup.");
}

app.Run();