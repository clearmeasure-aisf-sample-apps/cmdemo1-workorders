// ReSharper disable PropertyCanBeMadeInitOnly.Global -- System.Text.Json set-by-convention requires mutable setters

using System.Text.Json;

namespace ClearMeasure.Bootcamp.Core.Messaging;

/// <summary>
/// The envelope the remotable bus puts on the wire: a payload serialized with its source-generated contract and the
/// type name <see cref="RemotableMessageCatalog"/> resolves it back with.
/// </summary>
public class WebServiceMessage
{
    public string Body { get; set; } = "";
    public string TypeName { get; set; } = "";

    public WebServiceMessage()
    {
    }

    public WebServiceMessage(object request)
    {
        Body = GetBody(request);
        TypeName = RemotableMessageCatalog.TypeNameOf(request.GetType());
    }

    public static WebServiceMessage? FromJson(string json) =>
        JsonSerializer.Deserialize(json, RemotableMessageJsonContext.Default.WebServiceMessage);

    public string GetBody(object request) =>
        JsonSerializer.Serialize(request, RemotableMessageCatalog.ContractFor(request.GetType()));

    public string GetJson() => JsonSerializer.Serialize(this, RemotableMessageJsonContext.Default.WebServiceMessage);

    public object GetBodyObject() =>
        JsonSerializer.Deserialize(Body, RemotableMessageCatalog.ContractFor(TypeName))
        ?? throw new InvalidOperationException($"{TypeName} body deserialized to null.");
}
