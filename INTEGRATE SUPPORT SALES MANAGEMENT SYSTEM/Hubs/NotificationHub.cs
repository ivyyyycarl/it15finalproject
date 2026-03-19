using Microsoft.AspNetCore.SignalR;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task SendDataChanged(string entityType, string action)
        {
            await Clients.Others.SendAsync("DataChanged", entityType, action);
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }

    public interface IDataChangeNotifier
    {
        Task NotifyDataChanged(string entityType, string action);
    }

    public class DataChangeNotifier : IDataChangeNotifier
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public DataChangeNotifier(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyDataChanged(string entityType, string action)
        {
            await _hubContext.Clients.All.SendAsync("DataChanged", entityType, action);
        }
    }
}
