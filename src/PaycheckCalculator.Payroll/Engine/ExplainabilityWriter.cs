using PaycheckCalculator.Core.Calculations;

namespace PaycheckCalculator.Payroll.Engine;

public sealed class ExplainabilityWriter
{
    private readonly List<ExplainLine> _lines = new();

    public IReadOnlyList<ExplainLine> Lines => _lines;

    public void Add(ExplainLine line) => _lines.Add(line);
}
