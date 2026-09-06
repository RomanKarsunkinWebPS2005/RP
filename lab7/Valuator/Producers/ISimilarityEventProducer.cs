namespace Valuator.Producers;

public interface ISimilarityEventProducer
{
    public Task PublishSimilarityEventAsync(string textId,
        double similarityValue,
        CancellationToken cancellationToken = default);
}