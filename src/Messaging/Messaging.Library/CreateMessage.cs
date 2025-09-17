namespace Messaging.Library;

public class CreateMessage
{
    public Guid SenderId { get; set; }
    public string Content { get; set; } = null!;
}