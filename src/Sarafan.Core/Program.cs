var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Results.Redirect("/api/status/status"));

app.MapGet(
        "/api/status/status",
        () => Results.Ok(new ServiceStatus("Sarafan.Core", "ok")))
    .WithName("GetServiceStatus");

app.Run();

internal sealed record ServiceStatus(string Service, string Status);
