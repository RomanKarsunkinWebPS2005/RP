using Microsoft.AspNetCore.SignalR;

namespace Valuator.Hubs;

public class SummaryHub : Hub
{
    private readonly ILogger<SummaryHub> _logger;

    public SummaryHub(ILogger<SummaryHub> logger)
    {
        _logger = logger;
    }

    public async Task SubscribeToRankUpdate(string textId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Text_{textId}");
        _logger.LogInformation("Клиент {ConnectionId} подписан на Text_{TextId}", Context.ConnectionId, textId);
    }

    public async Task UnsubscribeFromRankUpdate(string textId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Text_{textId}");
        _logger.LogInformation("Клиент {ConnectionId} отписан от Text_{TextId}", Context.ConnectionId, textId);
    }
}
