using PaycheckCalculator.Core.Calculations;

namespace PaycheckCalculator.SharedUi.Services;

public interface IPaycheckScenarioStore
{
    Task<IReadOnlyList<CalculationResult>> ListAsync(CancellationToken ct = default);
    Task SaveAsync(CalculationResult result, string name, CancellationToken ct = default);
    Task DeleteAsync(Guid scenarioId, CancellationToken ct = default);
}

public sealed class InMemoryPaycheckScenarioStore : IPaycheckScenarioStore
{
    private readonly Dictionary<Guid, (string Name, CalculationResult Result)> _store = new();

    public Task<IReadOnlyList<CalculationResult>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<CalculationResult>>(_store.Values.Select(v => v.Result).ToArray());

    public Task SaveAsync(CalculationResult result, string name, CancellationToken ct = default)
    {
        _store[result.ScenarioId] = (name, result);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid scenarioId, CancellationToken ct = default)
    {
        _store.Remove(scenarioId);
        return Task.CompletedTask;
    }
}
