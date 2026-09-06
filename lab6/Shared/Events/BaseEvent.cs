namespace Shared.Events;

public abstract class BaseEvent
{
    public string Type { get; set; }
    public string TextId { get; set; }
    public DateTime Timestamp { get; set; }

    protected BaseEvent(string type, string textId)
    {
        Type = type;
        TextId = textId;
        Timestamp = DateTime.UtcNow;
    }
}