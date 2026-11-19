using AppTemplate.Application;
using AppTemplate.Infrastructure;
using AppTemplate.Web.Common;
using Serilog;

// Startup logger: records failures before configuration is loaded (bad appsettings, missing
// connection string). AddSerilog below replaces it with the configured logger.
// (A plain logger, not CreateBootstrapLogger: the reloadable one can be frozen only once per
// process, which breaks integration tests that start the app more than once.)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, logger) => logger
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Each layer registers its own services. Program.cs only composes them.
    builder.Services
        .AddApplication(builder.Configuration)
        .AddInfrastructure(builder.Configuration)
        .AddWeb();

    var app = builder.Build();
    app.UseWeb();
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}

// Lets the integration tests start the app with WebApplicationFactory<Program>.
public partial class Program;
