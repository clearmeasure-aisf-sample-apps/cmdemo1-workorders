using ClearMeasure.Bootcamp.DataAccess.TestSupport;

namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>Registers test-support options, data service and middleware.</summary>
public static class TestSupportExtensions
{
    /// <summary>Registers <see cref="TestSupportOptions"/> from DI configuration and <see cref="TestSupportDataService"/>.</summary>
    public static void AddTestSupport(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        // Resolve from IConfiguration at request time so WebApplicationFactory config overrides apply.
        services.AddSingleton(sp => TestSupportOptions.FromConfiguration(sp.GetRequiredService<IConfiguration>()));
        services.AddScoped<TestSupportDataService>();
    }

    /// <summary>Inserts <see cref="TestSupportMiddleware"/> ahead of API controllers.</summary>
    public static void UseTestSupport(this IApplicationBuilder app) =>
        app.UseMiddleware<TestSupportMiddleware>();

    /// <summary>Maps the test-support minimal APIs.</summary>
    public static void MapTestSupport(this WebApplication app) =>
        app.MapTestSupportEndpoints();
}
