using RabbitMQ.Client;

using System.Text;

using Wolverine;
using Wolverine.RabbitMQ.Internal;

namespace Messaging.RabbitMq.Library.Configuration;

/// <summary>
/// LegacyTypeMapper
/// We might inject more standard options to add to the header
/// </summary>
/// <param name="mapper"></param>
public sealed class RabbitMqHeaderEnrich(LegacyTypeMapper mapper) : IRabbitMqEnvelopeMapper
{
    public void MapIncomingToEnvelope(Envelope envelope, IReadOnlyBasicProperties incoming)
    {
        envelope.ContentType = incoming.ContentType;
        envelope.CorrelationId = incoming.CorrelationId;
        envelope.MessageType = incoming.Type;
        if (Guid.TryParse(incoming.MessageId, out var id))
            envelope.Id = id;

        // Read legacy headers
        var headers = incoming.Headers;
        if (headers is null) return;

        var typeFullName = ReadHeader(headers!, "TypeFullName");
        var assemblyBase = ReadHeader(headers!, "AssemblyBaseName");
        if (typeFullName is null) return;
        if (!string.IsNullOrWhiteSpace(typeFullName))
        {
            var name = mapper.MapFromLegacy(typeFullName, assemblyBase ?? "");
            envelope.MessageType = name;
        }

        // stash legacy info as envelope headers for diagnostics
        envelope.Headers["legacy-assembly-name"] = assemblyBase ?? "";
        envelope.Headers["legacy-type-name"] = typeFullName ?? "";
    }

    public void MapEnvelopeToOutgoing(Envelope envelope, IBasicProperties outgoing)
    {
        // Keep your existing interop for outbound messages to legacy
        outgoing.Type = envelope.MessageType;
        outgoing.ContentType = "application/json";
        outgoing.MessageId = envelope.Id.ToString();
        outgoing.CorrelationId = envelope.CorrelationId;
        outgoing.Headers ??= new Dictionary<string, object?>();
        var m = envelope.Message;
        if (m is not null)
        {
            var type = m.GetType();
            var (typeFullName, assemblyName) = mapper.MapToLegacy(type.FullName!);

            outgoing.Headers["AssemblyBaseName"] = assemblyName;
            outgoing.Headers["TypeFullName"] = typeFullName;
        }

        //outgoing.Headers["SendBy"] = "TextMessage Producer Console App";
        outgoing.Headers["Timestamp"] = DateTimeOffset.UtcNow.ToString("O");
        outgoing.Headers["CorrelationId"] = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString();

    }

    public IEnumerable<string> AllHeaders()
    {
        yield break;
        // Let Wolverine know which headers we care about
        //yield return "AssemblyBaseName";
        //yield return "TypeFullName";
    }

    private static string? ReadHeader(IDictionary<string, object> headers, string key)
    {
        if (!headers.TryGetValue(key, out var value) || value is null) return null;

        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            ReadOnlyMemory<byte> mem => Encoding.UTF8.GetString(mem.ToArray()),
            string s => s,
            _ => value.ToString()
        };
    }
}



