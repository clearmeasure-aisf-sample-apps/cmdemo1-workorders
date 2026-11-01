using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.DataAccess;

/// <summary>
/// Reads the highest applied DbUp script number from the schema journal.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global -- registered in UIServiceRegistry
public sealed class DatabaseMigrationVersionReader(
    DbContext dbContext,
    ILogger<DatabaseMigrationVersionReader> logger)
{
    /// <summary>
    /// Returns the highest leading script number in the journal, or null when the journal cannot be read.
    /// </summary>
    public async Task<int?> ReadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = UsesSqlite()
                ? "SELECT ScriptName AS Value FROM SchemaVersions"
                : "SELECT ScriptName AS Value FROM dbo.SchemaVersions";
            var names = await dbContext.Database.SqlQueryRaw<string>(sql).ToListAsync(cancellationToken);
            return HighestScriptNumber(names);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not read the DbUp schema journal");
            return null;
        }
    }

    private bool UsesSqlite()
    {
        var provider = dbContext.Database.ProviderName;
        if (provider is not null)
        {
            return provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase);
        }

        var connectionString = dbContext.Database.GetConnectionString();
        return connectionString is not null
            && connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase);
    }

    private static int? HighestScriptNumber(IEnumerable<string> scriptNames)
    {
        int? highest = null;
        foreach (var scriptName in scriptNames)
        {
            if (!TryParseScriptNumber(scriptName, out var number))
            {
                continue;
            }

            if (highest is null || number > highest)
            {
                highest = number;
            }
        }

        return highest;
    }

    private static bool TryParseScriptNumber(string scriptName, out int number)
    {
        number = 0;
        var fileName = scriptName.Replace('\\', '/');
        var slash = fileName.LastIndexOf('/');
        if (slash >= 0)
        {
            fileName = fileName[(slash + 1)..];
        }

        var digits = 0;
        while (digits < fileName.Length && char.IsDigit(fileName[digits]))
        {
            digits++;
        }

        if (digits == 0)
        {
            return false;
        }

        return int.TryParse(fileName.AsSpan(0, digits), NumberStyles.None, CultureInfo.InvariantCulture, out number);
    }
}
