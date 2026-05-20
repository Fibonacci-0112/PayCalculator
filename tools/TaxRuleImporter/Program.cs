using System.Text.Json;
using PaycheckCalculator.TaxRules.Federal2026;

var outputPath = args.Length > 0 ? args[0] : Path.Combine(Directory.GetCurrentDirectory(), "rules", "2026", "federal", "federal-2026-pub15t.json");
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

var ruleSet = FederalRule2026.ToRuleSet();
var json = JsonSerializer.Serialize(ruleSet, new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
});
File.WriteAllText(outputPath, json);
Console.WriteLine($"Wrote {outputPath} ({new FileInfo(outputPath).Length} bytes).");
