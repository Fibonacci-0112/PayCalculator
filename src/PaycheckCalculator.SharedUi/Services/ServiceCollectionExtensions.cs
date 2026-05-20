using Microsoft.Extensions.DependencyInjection;
using PaycheckCalculator.Jurisdictions.Resolver;
using PaycheckCalculator.Payroll.Engine;
using PaycheckCalculator.Projections.Optimization;
using PaycheckCalculator.SelfEmployment;
using PaycheckCalculator.TaxRules.Registry;

namespace PaycheckCalculator.SharedUi.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPaycheckCalculatorCore(this IServiceCollection services)
    {
        services.AddSingleton<IRulePackageRegistry, InMemoryRulePackageRegistry>();
        services.AddSingleton<IPaycheckCalculator, PaycheckPipeline>();
        services.AddSingleton<IPaycheckScenarioStore, InMemoryPaycheckScenarioStore>();
        services.AddSingleton<IJurisdictionResolver, ManualJurisdictionResolver>();
        services.AddSingleton<OptimizationEngine>();
        services.AddSingleton<ScheduleSeCalculator>();
        services.AddScoped<ScenarioEditorState>();
        return services;
    }
}
