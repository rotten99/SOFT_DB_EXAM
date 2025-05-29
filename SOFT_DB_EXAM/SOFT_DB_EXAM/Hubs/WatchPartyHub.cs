
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SOFT_DB_EXAM.Hubs
{
    public class WatchPartyHub : Hub
    {
        // Client joins a group for a specific party
        public async Task JoinParty(int partyId, string username)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"watchparty:{partyId}");
            await Clients.Group($"watchparty:{partyId}").SendAsync("ReceiveMessage", new
            {
                user = "System",
                message = $"{username} joined watch party {partyId}.",
                timestamp = DateTime.UtcNow
            });
        }


        
        public async Task LeaveParty(int partyId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"watchparty:{partyId}");
        }

        // Broadcast message to party
        public async Task SendMessageToParty(int partyId, string username, string message)
        {
            var payload = new
            {
                User = username,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            await Clients.Group($"watchparty:{partyId}").SendAsync("ReceiveMessage", payload);
        }
    }
}
