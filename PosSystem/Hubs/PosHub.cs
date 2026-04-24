using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace PosSystem.Hubs
{
    [Authorize]
    public class PosHub : Hub
    {
        public async Task JoinBranch(string branchId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"branch-{branchId}");
        }

        public async Task JoinKitchen(string branchId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"kitchen-{branchId}");
        }
    }
}
