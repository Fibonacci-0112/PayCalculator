using PaycheckCalculator.TaxRules.Registry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IRulePackageRegistry, InMemoryRulePackageRegistry>();

var app = builder.Build();

app.MapGet("/", () => Results.Text(
    "Paycheck Tax Planner — Rule Admin\n\n" +
    "Workflow:\n" +
    "  1. POST /admin/import   — Import source document (Pub 15-T table, state schedule, local table).\n" +
    "  2. POST /admin/normalize — Normalize to canonical rule model.\n" +
    "  3. POST /admin/review    — Mark as human-reviewed by a tax-rule maintainer.\n" +
    "  4. POST /admin/validate  — Run golden + validation cases against the package.\n" +
    "  5. POST /admin/sign      — Sign the validated package.\n" +
    "  6. POST /admin/publish   — Publish to the rule registry.\n" +
    "  7. POST /admin/rollback  — Roll back a previously published version.\n",
    contentType: "text/plain"));

app.MapPost("/admin/import", () => Results.Accepted(value: new { Status = "ImportQueued" }));
app.MapPost("/admin/normalize", () => Results.Accepted(value: new { Status = "Normalized" }));
app.MapPost("/admin/review", () => Results.Accepted(value: new { Status = "Reviewed" }));
app.MapPost("/admin/validate", () => Results.Accepted(value: new { Status = "Validated" }));
app.MapPost("/admin/sign", () => Results.Accepted(value: new { Status = "Signed" }));
app.MapPost("/admin/publish", () => Results.Accepted(value: new { Status = "Published" }));
app.MapPost("/admin/rollback", () => Results.Accepted(value: new { Status = "RolledBack" }));

app.Run();
