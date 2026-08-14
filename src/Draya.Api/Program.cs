using Draya.Api.Middleware;
using Draya.Application;
using Draya.Infrastructure;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowPort4200", policy =>
        policy.SetIsOriginAllowed(origin =>
        {
            try
            {
                var uri = new System.Uri(origin);
                return uri.Port == 4200;
            }
            catch
            {
                return false;
            }
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
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
app.UseCors("AllowPort4200");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<Draya.Api.Notifications.MaterialNotificationHub>("/hubs/materials");
app.MapHub<Draya.Api.Notifications.ClassroomQaHub>("/hubs/qa");

// Seed SuperAdmin user on startup
await Draya.Infrastructure.Persistence.AdminSeeder.SeedSuperAdminAsync(app.Services);

app.Run();