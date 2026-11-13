using ClearMeasure.Bootcamp.DataAccess.TestSupport;

namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>Registers test-support options, data service and middleware.</summary>
public static class TestSupportExtensions
{
    /// <summary>Binds <see cref="TestSupportOptions"/> and registers <see cref="ITestSupportDataService"/>.</summary>
    public static IServiceCollection AddTestSupport(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TestSupportOptions>(configuration.GetSection(TestSupportOptions.SectionName));
        services.AddScoped<TestSupportDataService>();
        return services;
    }

    /// <summary>Inserts <see cref="TestSupportMiddleware"/> ahead of API controllers.</summary>
    public static IApplicationBuilder UseTestSupport(this IApplicationBuilder app) =>
        app.UseMiddleware<TestSupportMiddleware>();

    /// <summary>Maps the test-support minimal APIs.</summary>
    public static WebApplication MapTestSupport(this WebApplication app)
    {
        app.MapTestSupportEndpoints();
        return app;
    }
}
