using Messaging.Library;

using RabbitMQ.Client;

using System.Text;

using Wolverine;
using Wolverine.RabbitMQ.Internal;

namespace Messaging.RabbitMq.Library.LegacySupport;

/// <summary>
/// LegacyTypes Header - Mapper
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

        if (envelope.Message is IMessageBase message)
        {
            outgoing.Headers["SendBy"] = message.ApplicationName ?? "";
            outgoing.Headers["MachineName"] = message.MachineName ?? "";
            outgoing.Headers["Sent-Timestamp"] = DateTimeOffset.UtcNow.ToString("O");//Make an IDateTimeProvider
            outgoing.Headers["Timestamp"] = message.TimeStamp.ToString("O");
            outgoing.Headers["Version"] = message.Version.ToString();
            outgoing.Headers["CorrelationId"] = message.CorrelationId.ToString();
        }

    }

    public IEnumerable<string> AllHeaders()
    {
        yield break;
        // Let Wolverine know which headers we care about
    }

    private static string? ReadHeader(IDictionary<string, object> headers, string key)
    {
        if (!headers.TryGetValue(key, out var value)) return null;

        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            ReadOnlyMemory<byte> mem => Encoding.UTF8.GetString(mem.ToArray()),
            string s => s,
            _ => value.ToString()
        };
    }
}



