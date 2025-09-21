using Messaging.Library;

namespace Messaging.Domain.Library.Messages;

public class CreateMessage : IMessage
{
    public Guid SenderId { get; set; }
    public string Content { get; set; } = null!;
}