using Application.Services.GameHub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.Hubs;

[Authorize]
public class GameHub : Hub<IGameClient>
{
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var gameId = httpContext?.Request.Query["gameId"].ToString();

        if (!string.IsNullOrEmpty(gameId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"game:{gameId}");
            // Game state snapshot delivery is handled in Plan 03
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // SignalR automatically cleans up group membership on disconnect
        await base.OnDisconnectedAsync(exception);
    }
}
