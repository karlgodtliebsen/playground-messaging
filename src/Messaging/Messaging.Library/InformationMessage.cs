using Wolverine.Attributes;

namespace Messaging.Library;

[MessageIdentity("information-message", Version = 1)]
public record InformationMessage(string SenderId, string Content);