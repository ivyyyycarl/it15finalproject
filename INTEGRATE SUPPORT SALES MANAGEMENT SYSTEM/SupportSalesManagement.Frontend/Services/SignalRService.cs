using Microsoft.AspNetCore.SignalR.Client;
using Blazored.LocalStorage;

namespace SupportSalesManagement.Frontend.Services
{
    public class SignalRService : IAsyncDisposable
    {
        private HubConnection? _hubConnection;
        private readonly ILocalStorageService _localStorage;
        private readonly string _baseUrl;

        public event Action<string, string>? OnDataChanged;

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public SignalRService(ILocalStorageService localStorage, string baseUrl)
        {
            _localStorage = localStorage;
            _baseUrl = baseUrl;
        }

        public async Task StartAsync()
        {
            if (_hubConnection != null) return;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_baseUrl}hubs/notification", options =>
                {
                    options.AccessTokenProvider = async () =>
                    {
                        return await _localStorage.GetItemAsync<string>("authToken");
                    };
                })
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<string, string>("DataChanged", (entityType, action) =>
            {
                OnDataChanged?.Invoke(entityType, action);
            });

            try
            {
                await _hubConnection.StartAsync();
                Console.WriteLine("[SignalR] Connected to notification hub");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Connection failed: {ex.Message}");
            }
        }

        public async Task StopAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
        }
    }
}
