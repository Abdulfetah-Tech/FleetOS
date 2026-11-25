using Microsoft.AspNetCore.SignalR;

namespace EthioFleet.Api.Hubs;

public sealed class TripHub : Hub
{
    public async Task JoinTripGroup(Guid tripId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"trip-{tripId}");
    }

    public async Task LeaveTripGroup(Guid tripId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"trip-{tripId}");
    }
}