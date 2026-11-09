using System.Data.Common;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ClearMeasure.Bootcamp.IntegrationTests.TestSupport;

/// <summary>
/// A <see cref="DataContext"/> that counts EF commands only while a <see cref="QueryBudgetScope"/> is open.
/// </summary>
public sealed class QueryBudgetContext(IDatabaseConfiguration configuration) : DataContext(configuration)
{
    private readonly QueryBudgetCounter _counter = new();

    /// <summary>
    /// Starts counting commands on this context. Dispose the scope before measuring another query.
    /// </summary>
    public QueryBudgetScope Begin()
    {
        return _counter.Begin();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_counter);
        base.OnConfiguring(optionsBuilder);
    }
}

/// <summary>
/// Commands executed through one <see cref="QueryBudgetContext"/> between <see cref="QueryBudgetContext.Begin"/>
/// and <see cref="Dispose"/>. Commands outside that window are ignored.
/// </summary>
public sealed class QueryBudgetScope(Action<QueryBudgetScope> onDispose) : IDisposable
{
    private readonly List<string> _texts = [];
    private bool _disposed;

    public int ReaderCount { get; private set; }

    public int OtherCount { get; private set; }

    public int CommandCount => ReaderCount + OtherCount;

    public IReadOnlyList<string> CommandTexts => _texts;

    public string Describe()
    {
        string body = _texts.Count == 0 ? "(no commands)" : string.Join(Environment.NewLine, _texts);
        return $"readers={ReaderCount}, other={OtherCount}{Environment.NewLine}{body}";
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        onDispose(this);
    }

    internal void AddReader(string commandText)
    {
        Add(reader: true, commandText);
    }

    internal void AddOther(string commandText)
    {
        Add(reader: false, commandText);
    }

    private void Add(bool reader, string commandText)
    {
        if (_disposed)
        {
            return;
        }

        _texts.Add(commandText);
        if (reader)
        {
            ReaderCount++;
        }
        else
        {
            OtherCount++;
        }
    }
}

internal sealed class QueryBudgetCounter : DbCommandInterceptor
{
    private QueryBudgetScope? _scope;

    public QueryBudgetScope Begin()
    {
        if (_scope is not null)
        {
            throw new InvalidOperationException("A query budget scope is already open on this context.");
        }

        var scope = new QueryBudgetScope(Release);
        _scope = scope;
        return scope;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        _scope?.AddReader(command.CommandText);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        _scope?.AddReader(command.CommandText);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData,
        InterceptionResult<int> result)
    {
        _scope?.AddOther(command.CommandText);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        _scope?.AddOther(command.CommandText);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData,
        InterceptionResult<object> result)
    {
        _scope?.AddOther(command.CommandText);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        _scope?.AddOther(command.CommandText);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void Release(QueryBudgetScope scope)
    {
        if (ReferenceEquals(_scope, scope))
        {
            _scope = null;
        }
    }
}
