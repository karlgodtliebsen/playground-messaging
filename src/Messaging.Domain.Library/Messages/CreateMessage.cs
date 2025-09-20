namespace Messaging.Domain.Library.Messages;

public class CreateMessage
{
    public Guid SenderId { get; set; }
    public string Content { get; set; } = null!;
}