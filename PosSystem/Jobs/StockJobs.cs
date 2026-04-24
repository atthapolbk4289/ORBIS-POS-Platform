using Hangfire;
using Microsoft.AspNetCore.SignalR;
using PosSystem.Helpers;
using PosSystem.Hubs;
using System;
using System.Threading.Tasks;

namespace PosSystem.Jobs
{
    public class StockJobs
    {
        private readonly ISqlHelper _sql;
        private readonly IHubContext<PosHub> _hub;

        public StockJobs(ISqlHelper sql, IHubContext<PosHub> hub)
        {
            _sql = sql;
            _hub = hub;
        }

        public class ProductIdResult
        {
            public Guid ProductId { get; set; }
        }

        [AutomaticRetry(Attempts = 3)]
        public async Task ReleaseExpiredReservationsAsync()
        {
            // Execute SP_ReleaseExpiredReservations
            await _sql.ExecuteStoredProcAsync("SP_ReleaseExpiredReservations");
            
            // Emit SignalR for affected products
            var sqlQuery = "SELECT DISTINCT ProductId FROM StockReservations WHERE Status='RELEASED' AND ReleasedAt >= DATEADD(MINUTE,-6,GETUTCDATE())";
            var affected = await _sql.QueryAsync<ProductIdResult>(sqlQuery);
            
            foreach (var result in affected)
            {
                await _hub.Clients.All.SendAsync("StockUpdated", result.ProductId);
            }
        }
        
        public async Task GenerateDailyReportAsync()
        {
            // Generate and cache daily report at 06:00
            await Task.CompletedTask;
        }
    }
}
