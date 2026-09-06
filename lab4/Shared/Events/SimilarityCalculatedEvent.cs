namespace Shared.Events;

public class SimilarityCalculatedEvent : BaseEvent
{
    public double Similarity { get; set; }

    public SimilarityCalculatedEvent(string textId, double similarity) 
        : base("SimilarityCalculated", textId)
    {
        Similarity = similarity;
    }
}