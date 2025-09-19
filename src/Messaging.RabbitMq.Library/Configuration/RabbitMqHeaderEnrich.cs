using RabbitMQ.Client;

using System.Text;

using Wolverine;
using Wolverine.RabbitMQ.Internal;

namespace Messaging.RabbitMq.Library.Configuration;

public sealed class RabbitMqHeaderEnrich : IRabbitMqEnvelopeMapper
{
    public void MapIncomingToEnvelope(Envelope envelope, IReadOnlyBasicProperties incoming)
    {
        envelope.ContentType = "application/json";
        envelope.CorrelationId = incoming.CorrelationId;

        if (Guid.TryParse(incoming.MessageId, out var id))
            envelope.Id = id;

        // Read legacy headers
        var headers = incoming.Headers;
        if (headers is null) return;

        var typeFullName = ReadHeader(headers!, "TypeFullName");
        var assemblyBase = ReadHeader(headers!, "AssemblyBaseName");

        // Drive Wolverine's type resolution from the legacy full name
        if (!string.IsNullOrWhiteSpace(typeFullName))
            envelope.MessageType = typeFullName;

        // (Optional) stash legacy info as envelope headers for diagnostics
        envelope.Headers["legacy-assembly-name"] = assemblyBase ?? "";
        envelope.Headers["legacy-type-name"] = typeFullName ?? "";
    }

    public void MapEnvelopeToOutgoing(Envelope envelope, IBasicProperties outgoing)
    {
        // Keep your existing interop for outbound messages to legacy
        outgoing.ContentType = "application/json";
        outgoing.MessageId = envelope.Id.ToString();
        outgoing.CorrelationId = envelope.CorrelationId;

        outgoing.Headers ??= new Dictionary<string, object?>();

        // If you tagged your new CLR type with a legacy identity (see next section),
        // envelope.MessageType will already be the legacy full name:
        var legacyType = envelope.MessageType ?? envelope.Message?.GetType().FullName!;

        // Fill the legacy headers expected by the old consumers
        outgoing.Headers["AssemblyBaseName"] = "AssemblyBaseName";
        outgoing.Headers["TypeFullName"] = legacyType;

        outgoing.Headers["SendBy"] = "TextMessage Producer Console App";
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



