using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Blazored.LocalStorage;

namespace SupportSalesManagement.Frontend.Services
{
    public class FinanceService : IFinanceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public FinanceService(HttpClient httpClient, ILocalStorageService localStorage)
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

        public async Task<decimal> GetInventoryValuationAsync()
        {
            try
            {
                await SetAuthorizationHeader();
                var result = await _httpClient.GetFromJsonAsync<JsonElement>("/api/Dashboard/inventory-valuation", _jsonOptions);
                if (result.TryGetProperty("value", out var value))
                {
                    return value.GetDecimal();
                }
                return 0.00m;
            }
            catch
            {
                return 0.00m;
            }
        }

        public async Task LogTransactionAsync(TransactionDto transaction)
        {
            try
            {
                await SetAuthorizationHeader();
                await _httpClient.PostAsJsonAsync("/api/erp/finance/transactions", transaction);
            }
            catch
            {
            }
        }
    }
}
