using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Messaging;

namespace ClearMeasure.Bootcamp.UnitTests.Core;

/// <summary>
/// Builds the envelope a hostile or outdated client could post for a type outside <see cref="RemotableMessageCatalog"/>,
/// which <see cref="WebServiceMessage"/> itself refuses to create.
/// </summary>
internal static class ForeignWebServiceMessage
{
    public static WebServiceMessage Carrying(object payload) => new()
    {
        Body = JsonSerializer.Serialize(payload, payload.GetType()),
        TypeName = RemotableMessageCatalog.TypeNameOf(payload.GetType())
    };
}
