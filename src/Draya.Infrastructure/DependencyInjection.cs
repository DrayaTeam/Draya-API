using Draya.Application.Common.Interfaces;
using Draya.Domain.Identity;
using Draya.Domain.Subscriptions;
using Draya.Infrastructure.Identity;
using Draya.Infrastructure.Identity.Repositories;
using Draya.Infrastructure.Identity.Services;
using Draya.Infrastructure.Persistence;
using Draya.Infrastructure.Subscriptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Draya.Infrastructure;

using Draya.Application.Materials.RAG;
using Draya.Infrastructure.Materials.RAG;
using Draya.Infrastructure.Materials.RAG.Extractors;
using Qdrant.Client;
using Polly;
using Polly.Extensions.Http;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // EF Core
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Identity
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Repositories
        services.AddScoped<ITeacherRepository, TeacherRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUsageCounterRepository, UsageCounterRepository>();
        services.AddScoped<Domain.Wallets.ITeacherWalletRepository, Wallets.TeacherWalletRepository>();
        services.AddScoped<Domain.Wallets.IWalletTransactionRepository, Wallets.WalletTransactionRepository>();
        services.AddScoped<Domain.Wallets.IWithdrawalRequestRepository, Wallets.WithdrawalRequestRepository>();
        services.AddScoped<Domain.Wallets.ITeacherPayoutAccountRepository, Wallets.TeacherPayoutAccountRepository>();
        services.AddScoped<Domain.Payments.IPaymentTransactionRepository, Payments.PaymentTransactionRepository>();
        services.AddScoped<Domain.Admin.IPlatformSettingRepository, Admin.PlatformSettingRepository>();
        services.AddScoped<Domain.Admin.IFinancialOverviewRepository, Admin.FinancialOverviewRepository>();
        services.AddScoped<Domain.Classrooms.ISubjectRepository, Classrooms.SubjectRepository>();
        services.AddScoped<Domain.Classrooms.IClassroomRepository, Classrooms.ClassroomRepository>();
        services.AddScoped<Domain.Classrooms.IEnrollmentRepository, Classrooms.EnrollmentRepository>();
        services.AddScoped<Domain.Classrooms.IQuestionRepository, Classrooms.QuestionRepository>();
        services.AddScoped<Domain.Materials.IMaterialRepository, Materials.MaterialRepository>();
        
        services.AddScoped<Domain.Classrooms.IClassroomTypeRepository, Classrooms.ClassroomTypeRepository>();
        services.AddScoped<Domain.Classrooms.IGradeLevelRepository, Classrooms.GradeLevelRepository>();
        
        // Services
        services.AddScoped<Application.Materials.IMaterialService, Application.Materials.MaterialService>();
        services.AddScoped<Application.Classrooms.Queries.GetClassroomRoster.IStudentRosterService, Classrooms.StudentRosterService>();
        services.AddScoped<Application.Exams.Services.IAIExamUsageService, Exams.AIExamUsageService>();
        services.AddScoped<Application.Payments.Services.IPaymobWebhookProcessingService, Payments.PaymobWebhookProcessingService>();
        services.AddScoped<Application.Payments.Services.IPaymentRefundService, Payments.PaymentRefundService>();
        services.AddHttpClient<Application.Payments.Services.IPaymobService, Payments.PaymobService>();
        
        // Storage Services
        services.AddScoped<Application.Materials.IMediaStorageService, Materials.CloudinaryMediaStorageService>();
        
        // Background Jobs
        services.AddSingleton<Application.Materials.IBackgroundTaskQueue>(ctx => new Application.Materials.DefaultBackgroundTaskQueue(100));
        services.AddHostedService<Materials.MaterialProcessingBackgroundService>();

        // RAG Pipeline Services
        services.AddScoped<IContentExtractor, PdfContentExtractor>();
        services.AddScoped<IContentExtractor, DocxContentExtractor>();
        services.AddScoped<IContentExtractor, PptxContentExtractor>();
        services.AddScoped<ContentExtractorFactory>();
        services.AddScoped<ITextCleaner, TextCleaner>();
        services.AddScoped<IChunker, FixedSizeChunker>();
        services.AddScoped<IVectorStore, QdrantVectorStore>();
        services.AddScoped<IProcessMaterialRagJob, ProcessMaterialRagJob>();

        // Configure Qdrant Client
        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var host = config["Qdrant:Host"] ?? "localhost";
            var port = int.TryParse(config["Qdrant:Port"], out var p) ? p : 6334;
            var https = bool.TryParse(config["Qdrant:Https"], out var h) && h;
            var apiKey = config["Qdrant:ApiKey"];
            
            return new QdrantClient(host, port, https, apiKey);
        });

        // Configure Embedding Service with Polly Retry
        services.AddHttpClient<IEmbeddingService, BgeM3EmbeddingService>((sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var baseUrl = config["EmbeddingApi:BaseUrl"] ?? "https://router.huggingface.co/hf-inference/models/BAAI/bge-m3/pipeline/feature-extraction";
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                client.BaseAddress = new Uri(baseUrl);
            }

            var apiKey = config["EmbeddingApi:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                var token = apiKey.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) 
                    ? apiKey.Substring("Bearer ".Length).Trim() 
                    : apiKey.Trim();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        })
        .AddPolicyHandler(GetRetryPolicy());

        // Auth & Identity services
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddSingleton<IAccountSecuritySettings, AccountSecuritySettings>();

        // JWT Authentication
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.NameIdentifier
            };
        });

        services.AddAuthorization();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}
