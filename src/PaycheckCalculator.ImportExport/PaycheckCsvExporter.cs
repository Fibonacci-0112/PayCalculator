using System.Globalization;
using System.Text;
using PaycheckCalculator.Core.Calculations;

namespace PaycheckCalculator.ImportExport;

public sealed class PaycheckCsvExporter
{
    public string Export(CalculationResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Section,Label,Amount,Currency");
        sb.AppendLine($"Summary,Gross Pay,{Decimal(result.GrossPay.Amount)},{result.GrossPay.Currency}");
        sb.AppendLine($"Summary,Net Pay,{Decimal(result.NetPay.Amount)},{result.NetPay.Currency}");
        sb.AppendLine($"Summary,Federal Taxable Wages,{Decimal(result.FederalTaxableWages.Amount)},USD");
        foreach (var t in result.Taxes)
        {
            sb.AppendLine($"Tax,{Escape(t.TaxType)},{Decimal(t.TaxAmount.Amount)},USD");
        }
        foreach (var d in result.Deductions)
        {
            sb.AppendLine($"Deduction,{Escape(d.Label)},{Decimal(d.Amount.Amount)},USD");
        }
        foreach (var w in result.Warnings)
        {
            sb.AppendLine($"Warning,{Escape(w.Category.ToString())},,");
        }
        return sb.ToString();
    }

    private static string Decimal(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Escape(string s) =>
        s.Contains(',') || s.Contains('"') ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
}
