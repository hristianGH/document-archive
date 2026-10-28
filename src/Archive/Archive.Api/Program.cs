using Archive.Api.Startup;
using Archive.Core.Cabinets;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.RegisterServices();

var app = builder.Build();

app.Logger.LogInformation("Loaded {CabinetCount} cabinets from {Path}",
    app.Services.GetRequiredService<CabinetCatalog>().All.Count, app.Configuration["Cabinets:Path"]);

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();

app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Archive API"));

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

await app.RunAsync();
