namespace Shared.Events;

public class RankCalculatedEvent : BaseEvent
{
    public double Rank { get; set; }

    public RankCalculatedEvent(string textId, double rank) 
        : base("RankCalculated", textId)
    {
        Rank = rank;
    }
}