using System.Net.Http.Json;
using System.Text.Json;
using SupportSalesManagement.Frontend.Models;
using Blazored.LocalStorage;

namespace SupportSalesManagement.Frontend.Services
{
    public interface IDashboardService
    {
        Task<List<TimeSeriesData>> GetUserRegistrationsTrendAsync();
        Task<List<CategoryData>> GetSalesByCategoryAsync();
        Task<List<CategoryData>> GetTicketStatusDistributionAsync();
        Task<List<AgentPerformanceData>> GetAgentThroughputAsync();
        Task<List<TimeSeriesData>> GetOrderTrendAsync();
        Task<List<CategoryData>> GetTopSellingProductsAsync();
        Task<decimal> GetStockHealthPercentageAsync();
        Task<List<TimeSeriesData>> GetSalesTrendAsync();
        Task<string> GetErpSyncStatusAsync();
    }

    public class DashboardService : IDashboardService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public DashboardService(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
        }

        private async Task SetAuthorizationHeader()
        {
            try
            {
                var token = await _localStorage.GetItemAsStringAsync("authToken");
                if (!string.IsNullOrEmpty(token))
                {
                    token = token.Trim('"');
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
            }
            catch { }
        }

        public async Task<List<TimeSeriesData>> GetUserRegistrationsTrendAsync()
        {
            try
            {
                await SetAuthorizationHeader();
                var result = await _httpClient.GetFromJsonAsync<List<TimeSeriesData>>("/api/Dashboard/user-registrations-trend", _jsonOptions);
                return result ?? new List<TimeSeriesData>();
            }
            catch
            {
                return new List<TimeSeriesData>();
            }
        }

        public async Task<List<CategoryData>> GetSalesByCategoryAsync()
        {
            try
            {
                await SetAuthorizationHeader();
                var result = await _httpClient.GetFromJsonAsync<List<CategoryData>>("/api/Dashboard/sales-by-category", _jsonOptions);
                return result ?? new List<CategoryData>();
            }
            catch
            {
                return new List<CategoryData>();
            }
        }

        public async Task<List<CategoryData>> GetTicketStatusDistributionAsync()
        {
            try
            {
                await SetAuthorizationHeader();
                var result = await _httpClient.GetFromJsonAsync<List<CategoryData>>("/api/Dashboard/ticket-status-distribution", _jsonOptions);
                return result ?? new List<CategoryData>();
            }
            catch
            {
                return new List<CategoryData>();
            }
        }

        public async Task<List<AgentPerformanceData>> GetAgentThroughputAsync()
        {
            try
            {
                await SetAuthorizationHeader();
                var result = await _httpClient.GetFromJsonAsync<List<AgentPerformanceData>>("/api/Dashboard/agent-throughput", _jsonOptions);
                return result ?? new List<AgentPerformanceData>();
            }
            catch
            {
                return new List<AgentPerformanceData>();
            }
        }

        public async Task<List<TimeSeriesData>> GetSalesTrendAsync()
        {
            try
            {
                await SetAuthorizationHeader();
                var result = await _httpClient.GetFromJsonAsync<List<TimeSeriesData>>("/api/Dashboard/sales-trend", _jsonOptions);
                return result ?? new List<TimeSeriesData>();
            }
            catch
            {
                return new List<TimeSeriesData>();
            }
        }

        public async Task<List<TimeSeriesData>> GetOrderTrendAsync()
        {
            try
            {
                await SetAuthorizationHeader();
                var result = await _httpClient.GetFromJsonAsync<List<TimeSeriesData>>("/api/Dashboard/order-trend", _jsonOptions);
                return result ?? new List<TimeSeriesData>();
            }
            catch
            {
                return new List<TimeSeriesData>();
            }
        }

        public async Task<List<CategoryData>> GetTopSellingProductsAsync()
        {
            try
            {
                await SetAuthorizationHeader();
                var result = await _httpClient.GetFromJsonAsync<List<CategoryData>>("/api/Dashboard/top-selling-products", _jsonOptions);
                return result ?? new List<CategoryData>();
            }
            catch
            {
                return new List<CategoryData>();
            }
        }

        public async Task<decimal> GetStockHealthPercentageAsync()
        {
            try
            {
                await SetAuthorizationHeader();
                var result = await _httpClient.GetFromJsonAsync<JsonElement>("/api/Dashboard/stock-health", _jsonOptions);
                if (result.TryGetProperty("value", out var value))
                {
                    return value.GetDecimal();
                }
                return 0m;
            }
            catch
            {
                return 0m;
            }
        }

        public async Task<string> GetErpSyncStatusAsync()
        {
            try
            {
                await SetAuthorizationHeader();
                var response = await _httpClient.GetAsync("api/erp/inventory");
                return response.IsSuccessStatusCode ? "Connected" : "Disconnected";
            }
            catch
            {
                return "Disconnected";
            }
        }
    }

    public class TimeSeriesData
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
    }

    public class CategoryData
    {
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public class AgentPerformanceData
    {
        public string AgentName { get; set; } = string.Empty;
        public int TicketsResolved { get; set; }
        public int CallsHandled { get; set; }
    }
}
