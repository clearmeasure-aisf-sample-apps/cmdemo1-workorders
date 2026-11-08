using System.Data.Common;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ClearMeasure.Bootcamp.IntegrationTests.TestSupport;

/// <summary>
/// A <see cref="DataContext"/> that counts the SQL commands it executes.
/// </summary>
public sealed class CountingDataContext(IDatabaseConfiguration configuration) : DataContext(configuration)
{
    /// <summary>Commands executed on this context.</summary>
    public CommandCounter Commands { get; } = new();

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(Commands);
        base.OnConfiguring(optionsBuilder);
    }

    /// <summary>Counts reader, non-query and scalar commands and keeps their text.</summary>
    public sealed class CommandCounter : DbCommandInterceptor
    {
        /// <summary>Commands that returned a reader.</summary>
        public int ReaderCommands { get; private set; }

        /// <summary>Non-query and scalar commands.</summary>
        public int OtherCommands { get; private set; }

        /// <summary>Command text in execution order.</summary>
        public List<string> Texts { get; } = [];

        /// <summary>Sets the counters back to zero.</summary>
        public void Reset()
        {
            ReaderCommands = 0;
            OtherCommands = 0;
            Texts.Clear();
        }

        /// <inheritdoc />
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ReaderCommands++;
            Texts.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        /// <inheritdoc />
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            OtherCommands++;
            Texts.Add(command.CommandText);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        /// <inheritdoc />
        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            OtherCommands++;
            Texts.Add(command.CommandText);
            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
