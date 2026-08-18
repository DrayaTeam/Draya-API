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

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        services.AddValidatorsFromAssembly(assembly);

        // Exams AI Background Processing
        services.AddSingleton<Exams.Services.IExamGenerationTaskQueue>(ctx => new Exams.Services.ExamGenerationTaskQueue(100));
        services.AddScoped<Exams.Services.IExamGenerationService, Exams.Services.ExamGenerationService>();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
