using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SpecialAssignment.Hubs;

// Dashboard clients subscribe here. Reporter browsers use plain HTTP POST
// (no auth) while dashboard streams require authorization.
[Authorize]
public class MonitoringHub : Hub
{
    public async Task SubscribeReport(string reportId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"report-{reportId}");
        await Clients.Caller.SendAsync("Subscribed", reportId);
    }

    public async Task UnsubscribeReport(string reportId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"report-{reportId}");
    }

    public async Task JoinDashboard()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "dashboard");
    }
}
