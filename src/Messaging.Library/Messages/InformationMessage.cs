using Wolverine.Attributes;

namespace Messaging.Library.Messages;

[MessageIdentity("information-message", Version = 1)]
public record InformationMessage(string SenderId, string Content);