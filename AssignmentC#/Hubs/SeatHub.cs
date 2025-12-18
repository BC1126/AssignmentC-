using Microsoft.AspNetCore.SignalR;

namespace AssignmentC_.Hubs;

public class SeatHub : Hub
{
    public async Task JoinShowtime(int showTimeId)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            $"showtime-{showTimeId}"
        );
    }

    public async Task LeaveShowtime(int showTimeId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            $"showtime-{showTimeId}"
        );
    }
}
