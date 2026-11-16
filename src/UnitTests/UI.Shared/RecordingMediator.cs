using Mediator;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

/// <summary>
/// Test double for the source-generated mediator behind <see cref="ClearMeasure.Bootcamp.UI.Shared.Bus"/>: records the
/// last request and notification and answers with a canned response. Streaming is not used by the bus.
/// </summary>
public sealed class RecordingMediator : IMediator
{
    private object? _response;
    private object? _objectResponse;

    public object? LastRequest { get; private set; }
    public object? LastObjectRequest { get; private set; }
    public object? LastNotification { get; private set; }

    public void SetResponse<T>(T response) => _response = response;

    public void SetObjectResponse(object? response) => _objectResponse = response;

    public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        Answer<TResponse>(request);

    public ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default) =>
        Answer<TResponse>(command);

    public ValueTask<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default) =>
        Answer<TResponse>(query);

    public ValueTask<object?> Send(object message, CancellationToken cancellationToken = default)
    {
        LastObjectRequest = message;
        return ValueTask.FromResult(_objectResponse);
    }

    public ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        LastNotification = notification;
        return ValueTask.CompletedTask;
    }

    public ValueTask Publish(object notification, CancellationToken cancellationToken = default)
    {
        LastNotification = notification;
        return ValueTask.CompletedTask;
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamCommand<TResponse> command, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamQuery<TResponse> query, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public IAsyncEnumerable<object?> CreateStream(object message, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    private ValueTask<TResponse> Answer<TResponse>(object request)
    {
        LastRequest = request;
        return ValueTask.FromResult((TResponse)_response!);
    }
}