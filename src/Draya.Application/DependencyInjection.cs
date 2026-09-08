using Draya.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Draya.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddValidatorsFromAssembly(assembly);

        // Exams AI Background Processing
        services.AddSingleton<Exams.Services.IExamGenerationTaskQueue>(ctx => new Exams.Services.ExamGenerationTaskQueue(100));
        services.AddScoped<Exams.Services.IExamGenerationService, Exams.Services.ExamGenerationService>();

        services.AddSingleton<Exams.Services.IExamGradingTaskQueue>(ctx => new Exams.Services.ExamGradingTaskQueue(100));
        services.AddScoped<Exams.Services.IExamGradingService, Exams.Services.ExamGradingService>();

        services.AddScoped<Reports.Services.IReportGenerationService, Reports.Services.ReportGenerationService>();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
