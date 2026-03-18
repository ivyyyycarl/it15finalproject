using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SupportSalesManagement.Frontend.Models;
using SupportSalesManagement.Frontend.Pages.Customer.Components;
using Blazored.LocalStorage;

namespace SupportSalesManagement.Frontend.Services
{
    public class EmailSettingsResponse
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public bool EnableSsl { get; set; }
        public bool EnableEmailNotifications { get; set; }
    }

    public class RealtimeIceServerDto
    {
        public List<string> Urls { get; set; } = [];
        public string? Username { get; set; }
        public string? Credential { get; set; }
    }

    public class ApiClient(HttpClient httpClient, NavigationManager navigationManager, ILocalStorageService localStorage) : IDisposable
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly NavigationManager _navigationManager = navigationManager;
        private readonly ILocalStorageService _localStorage = localStorage;

        public ILocalStorageService LocalStorage => _localStorage;
        public string? LastLoginError { get; private set; }

        // Timeline
        public async Task<List<CustomerTimeline.TimelineItem>?> GetCustomerTimelineAsync(int customerId)
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<CustomerTimeline.TimelineItem>>($"/api/Timeline/customer/{customerId}");
            }
            catch
            {
                return [];
            }
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

        private async Task HandleUnauthorizedAsync()
        {
            try
            {
                await _localStorage.RemoveItemAsync("authToken");
                await _localStorage.RemoveItemAsync("user");
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
            catch { }
            _navigationManager.NavigateTo("/login", forceLoad: true);
        }

        private async Task<HttpResponseMessage> SendWithAuthAsync(Func<Task<HttpResponseMessage>> requestFunc)
        {
            await SetAuthorizationHeader();
            var response = await requestFunc();
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorizedAsync();
            }
            return response;
        }

        private async Task<T?> GetFromJsonWithAuthAsync<T>(string url)
        {
            var response = await SendWithAuthAsync(() => _httpClient.GetAsync(url));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
        }

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        private static string BuildQueryString(Dictionary<string, string?> queryParams)
        {
            var pairs = queryParams
                .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
                .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}");
            return string.Join("&", pairs);
        }

        private static string? ExtractApiErrorMessage(string? content, string fallback)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return fallback;
            }

            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                // Common shape: { "message": "..." }
                if (root.TryGetProperty("message", out var messageElement))
                {
                    var message = messageElement.GetString();
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        if (root.TryGetProperty("detail", out var detailElement))
                        {
                            var detail = detailElement.GetString();
                            if (!string.IsNullOrWhiteSpace(detail))
                            {
                                return $"{message} {detail}";
                            }
                        }
                        return message;
                    }
                }

                // ApiErrorFactory shape: { "success": false, "error": { "message": "...", "details": { ... } } }
                if (root.TryGetProperty("error", out var errorElement) && errorElement.ValueKind == JsonValueKind.Object)
                {
                    if (errorElement.TryGetProperty("details", out var detailsElement) && detailsElement.ValueKind == JsonValueKind.Object)
                    {
                        var detailMessages = new List<string>();
                        foreach (var property in detailsElement.EnumerateObject())
                        {
                            if (property.Value.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var item in property.Value.EnumerateArray())
                                {
                                    var text = item.GetString();
                                    if (!string.IsNullOrWhiteSpace(text))
                                    {
                                        detailMessages.Add(text);
                                    }
                                }
                            }
                            else
                            {
                                var text = property.Value.GetString();
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    detailMessages.Add(text);
                                }
                            }
                        }

                        if (detailMessages.Count > 0)
                        {
                            return string.Join(" | ", detailMessages);
                        }
                    }

                    if (errorElement.TryGetProperty("message", out var nestedMessage))
                    {
                        var message = nestedMessage.GetString();
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            if (errorElement.TryGetProperty("detail", out var nestedDetail))
                            {
                                var detail = nestedDetail.GetString();
                                if (!string.IsNullOrWhiteSpace(detail))
                                {
                                    return $"{message} {detail}";
                                }
                            }
                            return message;
                        }
                    }
                }
            }
            catch
            {
            }

            return fallback;
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest loginRequest)
        {
            LastLoginError = null;
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions);
                    if (result != null)
                    {
                        await _localStorage.SetItemAsync("authToken", result.Token);
                        await _localStorage.SetItemAsync("user", result.User);
                        return result;
                    }
                }

                var content = await response.Content.ReadAsStringAsync();
                LastLoginError = ExtractApiErrorMessage(content, "Invalid email or password");
            }
            catch
            {
                LastLoginError = "Unable to connect to the server. Please try again.";
                return null;
            }
            return null;
        }

        public async Task<(bool Success, string Message)> RegisterAsync(RegisterRequest registerRequest)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/register", registerRequest);
                if (response.IsSuccessStatusCode)
                {
                    return (true, string.Empty);
                }

                var content = await response.Content.ReadAsStringAsync();
                var parsedMessage = ExtractApiErrorMessage(content, string.Empty);
                if (!string.IsNullOrWhiteSpace(parsedMessage))
                {
                    return (false, parsedMessage);
                }

                // Try to parse detailed validation errors for 400 payloads
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest && !string.IsNullOrWhiteSpace(content))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(content);
                        var root = doc.RootElement;

                        // Handle standard ProblemDetails/ValidationProblemDetails
                        if (root.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Object)
                        {
                            var errorMessages = new List<string>();
                            foreach (var property in errorsElement.EnumerateObject())
                            {
                                if (property.Value.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var error in property.Value.EnumerateArray())
                                    {
                                        errorMessages.Add(error.GetString() ?? "Unknown error");
                                    }
                                }
                            }
                            if (errorMessages.Count > 0)
                            {
                                return (false, string.Join(" | ", errorMessages));
                            }
                        }

                        // Fallback to "message" property if present
                        if (root.TryGetProperty("message", out var messageElement))
                        {
                            return (false, messageElement.GetString() ?? "Registration failed");
                        }

                        // Handle ApiErrorFactory envelope: { success:false, error:{ message, details } }
                        if (root.TryGetProperty("error", out var errorElement) && errorElement.ValueKind == JsonValueKind.Object)
                        {
                            if (errorElement.TryGetProperty("details", out var detailsElement) && detailsElement.ValueKind == JsonValueKind.Object)
                            {
                                var detailMessages = new List<string>();
                                foreach (var property in detailsElement.EnumerateObject())
                                {
                                    if (property.Value.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (var item in property.Value.EnumerateArray())
                                        {
                                            var text = item.GetString();
                                            if (!string.IsNullOrWhiteSpace(text))
                                            {
                                                detailMessages.Add(text);
                                            }
                                        }
                                    }
                                }

                                if (detailMessages.Count > 0)
                                {
                                    return (false, string.Join(" | ", detailMessages));
                                }
                            }

                            if (errorElement.TryGetProperty("message", out var nestedMessage))
                            {
                                return (false, nestedMessage.GetString() ?? "Registration failed");
                            }
                        }
                    }
                    catch { /* Fallback to generic status */ }
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    return (false, "This email is already registered. Please login or use Forgot Password.");
                }

                return (false, "Registration failed. Please review your details and try again.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<List<SubscriptionPlanModel>?> GetPublicSubscriptionPlansAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<SubscriptionPlanModel>>("/api/public/subscription/plans", _jsonOptions);
            }
            catch
            {
                return [];
            }
        }

        public async Task<(CompanySubscriptionResponse? Response, string? Error)> SubscribeCompanyAsync(CompanySubscriptionRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/public/subscription/subscribe-company", request);
                if (response.IsSuccessStatusCode)
                {
                    var payload = await response.Content.ReadFromJsonAsync<CompanySubscriptionResponse>(_jsonOptions);
                    return (payload, null);
                }

                var content = await response.Content.ReadAsStringAsync();
                return (null, ExtractApiErrorMessage(content, $"Failed to subscribe company ({(int)response.StatusCode} {response.ReasonPhrase})."));
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        public async Task<(string? CheckoutUrl, string? Error)> CreateSubscriptionCheckoutSessionAsync(CompanySubscriptionRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/public/subscription/checkout-session", request);
                if (response.IsSuccessStatusCode)
                {
                    var payload = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
                    if (payload.TryGetProperty("checkoutUrl", out var urlElement))
                    {
                        var checkoutUrl = urlElement.GetString();
                        if (!string.IsNullOrWhiteSpace(checkoutUrl))
                        {
                            return (checkoutUrl, null);
                        }
                    }
                    return (null, "Checkout URL was not returned.");
                }

                var content = await response.Content.ReadAsStringAsync();
                return (null, ExtractApiErrorMessage(content, $"Failed to create checkout session ({(int)response.StatusCode} {response.ReasonPhrase})."));
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        public async Task<(CompanySubscriptionResponse? Response, string? Error)> ConfirmSubscriptionCheckoutAsync(string sessionId)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/public/subscription/confirm-checkout", new { SessionId = sessionId });
                if (response.IsSuccessStatusCode)
                {
                    var payload = await response.Content.ReadFromJsonAsync<CompanySubscriptionResponse>(_jsonOptions);
                    return (payload, null);
                }

                var content = await response.Content.ReadAsStringAsync();
                return (null, ExtractApiErrorMessage(content, $"Failed to confirm checkout ({(int)response.StatusCode} {response.ReasonPhrase})."));
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(string email)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/reset-password", new { Email = email });
                var payload = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    if (payload.TryGetProperty("message", out var okMsg))
                    {
                        return (true, okMsg.GetString() ?? "Password reset email sent.");
                    }

                    return (true, "Password reset email sent.");
                }

                if (payload.TryGetProperty("message", out var msg))
                {
                    return (false, msg.GetString() ?? "Password reset failed.");
                }
                return (false, "Password reset failed. Please check your email and try again.");
            }
            catch
            {
                return (false, "Unable to connect to the server. Please try again later.");
            }
        }

        public async Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var payload = new
                {
                    UserId = userId,
                    CurrentPassword = currentPassword,
                    NewPassword = newPassword
                };

                var response = await _httpClient.PostAsJsonAsync("/api/auth/change-password", payload);
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Password changed successfully.");
                }

                var content = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(content);
                        if (doc.RootElement.TryGetProperty("message", out var message))
                        {
                            return (false, message.GetString() ?? "Password update failed.");
                        }
                    }
                    catch
                    {
                    }
                }

                return (false, "Unable to change password. Please verify your current password.");
            }
            catch
            {
                return (false, "Unable to connect to the server. Please try again later.");
            }
        }

        public async Task LogoutAsync()
        {
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("user");
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            try
            {
                return await _localStorage.GetItemAsync<User>("user");
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<Call>?> GetCallsAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<Call>>("/api/calls");
            }
            catch
            {
                return [];
            }
        }

        public async Task<Call?> GetCallAsync(int id)
        {
            try
            {
                return await GetFromJsonWithAuthAsync<Call>($"/api/calls/{id}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<Call>?> GetCallsByAgentAsync(int agentId)
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<Call>>($"/api/calls/agent/{agentId}");
            }
            catch
            {
                return [];
            }
        }

        public async Task<List<Call>?> GetCallsByCustomerAsync(int customerId)
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<Call>>($"/api/calls/customer/{customerId}");
            }
            catch
            {
                return [];
            }
        }

        public async Task<Call?> CreateCallAsync(CreateCallRequest request)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PostAsJsonAsync("/api/calls", request));
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Call>(_jsonOptions);
                }
            }
            catch
            {
            }
            return null;
        }

        public async Task<Call?> UpdateCallAsync(int id, UpdateCallRequest request)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PutAsJsonAsync($"/api/calls/{id}", request));
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Call>(_jsonOptions);
                }
            }
            catch
            {
            }
            return null;
        }

        public async Task<bool> StartCallAsync(int id)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PostAsync($"/api/calls/{id}/start", null));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> EndCallAsync(int id)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PostAsync($"/api/calls/{id}/end", null));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<CallSummary?> GetCallSummaryAsync(int agentId)
        {
            try
            {
                return await GetFromJsonWithAuthAsync<CallSummary>($"/api/calls/summary/{agentId}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<RealtimeIceServerDto>?> GetSupportIceServersAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<RealtimeIceServerDto>>("/api/realtime/ice-servers");
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> EscalateTicketAsync(int ticketId)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PostAsync($"/api/tickets/{ticketId}/escalate", null));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // ============================================
        // TICKETS API
        // ============================================

        // Tickets
        public async Task<List<Ticket>?> GetTicketsAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<Ticket>>("/api/tickets");
            }
            catch
            {
                return [];
            }
        }

        public async Task<Ticket?> GetTicketAsync(int id)
        {
            try
            {
                return await GetFromJsonWithAuthAsync<Ticket>($"/api/tickets/{id}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<Ticket?> CreateTicketAsync(CreateTicketRequest request)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PostAsJsonAsync("/api/tickets", request));
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Ticket>(_jsonOptions);
                }
            }
            catch
            {
            }
            return null;
        }

        public async Task<(Ticket? Ticket, string? ErrorMessage)> CreateTicketWithErrorAsync(CreateTicketRequest request)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PostAsJsonAsync("/api/tickets", request));
                if (response.IsSuccessStatusCode)
                {
                    var ticket = await response.Content.ReadFromJsonAsync<Ticket>(_jsonOptions);
                    return (ticket, null);
                }

                var errorBody = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(errorBody))
                {
                    try
                    {
                        var payload = JsonSerializer.Deserialize<JsonElement>(errorBody, _jsonOptions);
                        if (payload.TryGetProperty("message", out var messageProp))
                        {
                            var message = messageProp.GetString();
                            if (!string.IsNullOrWhiteSpace(message))
                            {
                                return (null, message);
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                return (null, $"Ticket creation failed ({(int)response.StatusCode} {response.ReasonPhrase}).");
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        public async Task<Ticket?> UpdateTicketAsync(int id, UpdateTicketRequest request)
        {
            var response = await SendWithAuthAsync(() => _httpClient.PutAsJsonAsync($"/api/tickets/{id}", request));
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Ticket>(_jsonOptions);
            }
            return null;
        }

        public async Task<bool> AssignTicketAsync(int ticketId, int agentId)
        {
            var response = await SendWithAuthAsync(() => _httpClient.PostAsJsonAsync($"/api/tickets/{ticketId}/assign", new { agentId }));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AddTicketCommentAsync(int ticketId, AddCommentRequest request)
        {
            var response = await SendWithAuthAsync(() => _httpClient.PostAsJsonAsync($"/api/tickets/{ticketId}/comments", request));
            return response.IsSuccessStatusCode;
        }

        public async Task<TicketComment?> AddTicketCommentWithResultAsync(int ticketId, AddCommentRequest request)
        {
            var response = await SendWithAuthAsync(() => _httpClient.PostAsJsonAsync($"/api/tickets/{ticketId}/comments", request));
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TicketComment>(_jsonOptions);
        }

        public async Task<List<Ticket>?> GetTicketsByCustomerAsync(int customerId)
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<Ticket>>($"/api/tickets/customer/{customerId}");
            }
            catch
            {
                return [];
            }
        }

        // Customers
        public async Task<List<Customer>?> GetCustomersAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<Customer>>("/api/customers");
            }
            catch
            {
                return [];
            }
        }

        public async Task<PagedResult<Customer>?> GetCustomersPagedAsync(
            int page = 1,
            int pageSize = 25,
            string? search = null,
            string? sortBy = "name",
            string? sortDir = "asc")
        {
            try
            {
                var query = BuildQueryString(new Dictionary<string, string?>
                {
                    ["page"] = page.ToString(),
                    ["pageSize"] = pageSize.ToString(),
                    ["search"] = search,
                    ["sortBy"] = sortBy,
                    ["sortDir"] = sortDir
                });
                return await GetFromJsonWithAuthAsync<PagedResult<Customer>>($"/api/customers/paged?{query}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<Customer?> GetCustomerAsync(int id)
        {
            return await GetFromJsonWithAuthAsync<Customer>($"/api/customers/{id}");
        }

        public async Task<Customer?> CreateCustomerAsync(CreateCustomerRequest request)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PostAsJsonAsync("/api/customers", request));
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Customer>(_jsonOptions);
                }
            }
            catch
            {
            }
            return null;
        }

        public async Task<Customer?> UpdateCustomerAsync(int id, UpdateCustomerRequest request)
        {
            var response = await SendWithAuthAsync(() => _httpClient.PutAsJsonAsync($"/api/customers/{id}", request));
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Customer>(_jsonOptions);
            }
            return null;
        }

        public async Task<Customer?> GetCustomerByUserIdAsync(int userId)
        {
            try
            {
                return await GetFromJsonWithAuthAsync<Customer>($"/api/customers/user/{userId}");
            }
            catch
            {
                return null;
            }
        }

        // Orders
        public async Task<List<Order>?> GetOrdersAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<Order>>("/api/orders") ?? [];
            }
            catch
            {
                return [];
            }
        }

        public async Task<PagedResult<Order>?> GetOrdersPagedAsync(
            int page = 1,
            int pageSize = 25,
            int? customerId = null,
            int? agentId = null,
            string? status = null,
            string? paymentStatus = null,
            string? search = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string? sortBy = "createdAt",
            string? sortDir = "desc")
        {
            try
            {
                var query = BuildQueryString(new Dictionary<string, string?>
                {
                    ["page"] = page.ToString(),
                    ["pageSize"] = pageSize.ToString(),
                    ["customerId"] = customerId?.ToString(),
                    ["agentId"] = agentId?.ToString(),
                    ["status"] = status,
                    ["paymentStatus"] = paymentStatus,
                    ["search"] = search,
                    ["dateFrom"] = dateFrom?.ToString("O"),
                    ["dateTo"] = dateTo?.ToString("O"),
                    ["sortBy"] = sortBy,
                    ["sortDir"] = sortDir
                });
                return await GetFromJsonWithAuthAsync<PagedResult<Order>>($"/api/orders/paged?{query}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<Order>?> GetOrdersByCustomerAsync(int customerId)
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<Order>>($"/api/orders/customer/{customerId}") ?? [];
            }
            catch
            {
                return [];
            }
        }

        public async Task<Order?> GetOrderAsync(int id)
        {
            return await GetFromJsonWithAuthAsync<Order>($"/api/orders/{id}");
        }

        public async Task<Order?> CreateOrderAsync(CreateOrderRequest request)
        {
            var result = await CreateOrderWithResultAsync(request);
            return result.Order;
        }

        public async Task<(Order? Order, string? ErrorMessage)> CreateOrderWithResultAsync(CreateOrderRequest request)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PostAsJsonAsync("/api/orders", request));
                if (response.IsSuccessStatusCode)
                {
                    var order = await response.Content.ReadFromJsonAsync<Order>(_jsonOptions);
                    return (order, null);
                }

                var errorBody = await response.Content.ReadAsStringAsync();
                var errorMessage = ExtractApiErrorMessage(errorBody, $"Order creation failed ({(int)response.StatusCode} {response.ReasonPhrase}).");
                return (null, errorMessage);
            }
            catch (Exception ex)
            {
                return (null, $"Connection error: {ex.Message}");
            }
        }

        public async Task<Order?> UpdateOrderAsync(int id, UpdateOrderRequest request)
        {
            var response = await SendWithAuthAsync(() => _httpClient.PutAsJsonAsync($"/api/orders/{id}", request));
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Order>(_jsonOptions);
            }
            return null;
        }

        public async Task<(bool Success, string Message)> RequestOrderRefundAsync(int orderId, string reason)
        {
            try
            {
                var response = await SendWithAuthAsync(() =>
                    _httpClient.PostAsJsonAsync($"/api/orders/{orderId}/refund-request", new { reason }));

                if (response.IsSuccessStatusCode)
                {
                    var payload = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
                    if (payload.TryGetProperty("message", out var msg))
                    {
                        return (true, msg.GetString() ?? "Refund request submitted.");
                    }
                    return (true, "Refund request submitted.");
                }

                var content = await response.Content.ReadAsStringAsync();
                return (false, ExtractApiErrorMessage(content, "Unable to request refund.") ?? "Unable to request refund.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> ApproveOrderRefundAsync(int orderId, string reason, decimal? amount = null)
        {
            try
            {
                var response = await SendWithAuthAsync(() =>
                    _httpClient.PostAsJsonAsync($"/api/orders/{orderId}/refund/approve", new { reason, amount }));

                if (response.IsSuccessStatusCode)
                {
                    var payload = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
                    if (payload.TryGetProperty("message", out var msg))
                    {
                        return (true, msg.GetString() ?? "Refund approved.");
                    }
                    return (true, "Refund approved.");
                }

                var content = await response.Content.ReadAsStringAsync();
                return (false, ExtractApiErrorMessage(content, "Unable to approve refund.") ?? "Unable to approve refund.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<List<PendingRefundRequest>?> GetPendingRefundRequestsAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<PendingRefundRequest>>("/api/orders/refund-requests/pending") ?? [];
            }
            catch
            {
                return [];
            }
        }

        // Products
        public async Task<List<Product>?> GetProductsAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<Product>>("/api/products");
            }
            catch
            {
                return [];
            }
        }

        public async Task<PagedResult<Product>?> GetProductsPagedAsync(
            int page = 1,
            int pageSize = 25,
            string? search = null,
            string? category = null,
            bool? isActive = null,
            bool lowStockOnly = false,
            string? stockStatus = null,
            string? sortBy = "name",
            string? sortDir = "asc")
        {
            try
            {
                var query = BuildQueryString(new Dictionary<string, string?>
                {
                    ["page"] = page.ToString(),
                    ["pageSize"] = pageSize.ToString(),
                    ["search"] = search,
                    ["category"] = category,
                    ["isActive"] = isActive?.ToString(),
                    ["lowStockOnly"] = lowStockOnly.ToString(),
                    ["stockStatus"] = stockStatus,
                    ["sortBy"] = sortBy,
                    ["sortDir"] = sortDir
                });
                return await GetFromJsonWithAuthAsync<PagedResult<Product>>($"/api/products/paged?{query}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<(string? Url, string? Error)> UploadImageAsync(IBrowserFile file)
        {
            try
            {
                await SetAuthorizationHeader();

                using var content = new MultipartFormDataContent();
                using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB limit
                using var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

                content.Add(fileContent, "file", file.Name);

                var response = await _httpClient.PostAsync("/api/Upload", content);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await HandleUnauthorizedAsync();
                    return (null, "Session expired. Please log in again.");
                }

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                    if (result.TryGetProperty("url", out var urlElement))
                    {
                        return (urlElement.GetString(), null);
                    }
                    return (null, "Invalid response from server.");
                }
                else
                {
                    var statusCode = response.StatusCode;
                    string errorMsg = $"Upload failed with status code {statusCode}.";
                    try
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        errorMsg += $" Details: {errorContent}";
                    }
                    catch { }
                    return (null, errorMsg);
                }
            }
            catch (Exception ex)
            {
                return (null, $"Error uploading file: {ex.Message}");
            }
        }

        public async Task<(Product? Product, string? Error)> CreateProductAsync(CreateProductRequest request)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PostAsJsonAsync("/api/products", request));

                if (response.IsSuccessStatusCode)
                {
                    var product = await response.Content.ReadFromJsonAsync<Product>(_jsonOptions);
                    return (product, null);
                }
                else
                {
                    var statusCode = response.StatusCode;
                    string errorMsg = $"Creation failed with status {statusCode}.";
                    try
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        errorMsg += $" Details: {errorContent}";
                    }
                    catch { }
                    return (null, errorMsg);
                }
            }
            catch (Exception ex)
            {
                return (null, $"Error creating product: {ex.Message}");
            }
        }

        public async Task<(Product? Product, string? Error)> UpdateProductAsync(int id, UpdateProductRequest request)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PutAsJsonAsync($"/api/products/{id}", request));
                if (response.IsSuccessStatusCode)
                {
                    var product = await response.Content.ReadFromJsonAsync<Product>(_jsonOptions);
                    return (product, null);
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    var errorMessage = ExtractApiErrorMessage(errorBody, $"Update failed with status {response.StatusCode}.");
                    return (null, errorMessage);
                }
            }
            catch (Exception ex)
            {
                return (null, $"Error updating product: {ex.Message}");
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.DeleteAsync($"/api/products/{id}"));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Product>?> GetActiveProductsAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<Product>>("/api/products/active");
            }
            catch
            {
                return [];
            }
        }

        // User Management
        public async Task<List<User>?> GetUsersAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<User>>("/api/users");
            }
            catch
            {
                return [];
            }
        }

        public async Task<PagedResult<User>?> GetUsersPagedAsync(
            int page = 1,
            int pageSize = 25,
            string? search = null,
            UserRole? role = null,
            bool? isActive = true,
            string? sortBy = "name",
            string? sortDir = "asc")
        {
            try
            {
                var query = BuildQueryString(new Dictionary<string, string?>
                {
                    ["page"] = page.ToString(),
                    ["pageSize"] = pageSize.ToString(),
                    ["search"] = search,
                    ["role"] = role?.ToString(),
                    ["isActive"] = isActive?.ToString(),
                    ["sortBy"] = sortBy,
                    ["sortDir"] = sortDir
                });
                return await GetFromJsonWithAuthAsync<PagedResult<User>>($"/api/users/paged?{query}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<User?> CreateUserAsync(CreateUserDto userDto)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PostAsJsonAsync("/api/users", userDto, _jsonOptions));

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<User>(_jsonOptions);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = "Unknown error occurred";

                try
                {
                    var errorObj = System.Text.Json.JsonDocument.Parse(errorContent);
                    if (errorObj.RootElement.TryGetProperty("message", out var msgElement))
                    {
                        errorMessage = msgElement.GetString() ?? errorMessage;
                    }
                }
                catch { }

                throw new HttpRequestException(errorMessage);
            }
            catch
            {
                throw;
            }
        }

        public async Task<User?> UpdateUserAsync(int id, UpdateUserDto userDto)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PutAsJsonAsync($"/api/users/{id}", userDto));
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<User>(_jsonOptions);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.DeleteAsync($"/api/users/{id}"));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // ============================================
        // ERP INVENTORY API
        // ============================================

        public async Task<List<InventoryItem>?> GetErpInventoryAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<InventoryItem>>("/api/erp/inventory");
            }
            catch
            {
                return null;
            }
        }

        public async Task<InventoryItem?> GetErpInventoryBySKUAsync(string sku)
        {
            try
            {
                return await GetFromJsonWithAuthAsync<InventoryItem>($"/api/erp/inventory/{sku}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<InventoryItem>?> GetErpInventoryByCategoryAsync(string category)
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<InventoryItem>>($"/api/erp/inventory/category/{category}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<InventoryItem>?> GetErpLowStockItemsAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<InventoryItem>>("/api/erp/inventory/low-stock");
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> UpdateErpInventoryStockAsync(string sku, UpdateInventoryStockRequest request)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PutAsJsonAsync($"/api/erp/inventory/{sku}/stock", request));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // ============================================
        // ERP FINANCE API
        // ============================================

        public async Task<List<FinancialTransaction>?> GetErpTransactionsAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<FinancialTransaction>>("/api/erp/finance/transactions");
            }
            catch
            {
                return null;
            }
        }

        // ============================================
        // STRIPE PAYMENTS
        // ============================================

        public async Task<string?> GetStripePublicKeyAsync()
        {
            var httpResponse = await SendWithAuthAsync(() => _httpClient.GetAsync("/api/payments/config"));
            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorBody = await httpResponse.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Unable to load payment config ({(int)httpResponse.StatusCode}). {errorBody}");
            }

            var response = await httpResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            if (response.ValueKind == JsonValueKind.Object
                && response.TryGetProperty("publishableKey", out var key))
            {
                var publishableKey = key.GetString();
                if (!string.IsNullOrWhiteSpace(publishableKey))
                {
                    return publishableKey;
                }
            }

            throw new InvalidOperationException("Payment config response did not include a publishable key.");
        }

        public async Task<string?> CreatePaymentIntentAsync(decimal amount)
        {
            var response = await SendWithAuthAsync(() => _httpClient.PostAsJsonAsync("/api/payments/create-intent", new { Amount = amount }));
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Unable to create payment intent ({(int)response.StatusCode}). {errorBody}");
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            if (result.ValueKind == JsonValueKind.Object
                && result.TryGetProperty("clientSecret", out var secret))
            {
                var clientSecret = secret.GetString();
                if (!string.IsNullOrWhiteSpace(clientSecret))
                {
                    return clientSecret;
                }
            }

            throw new InvalidOperationException("Payment intent response did not include a clientSecret.");
        }


        public async Task<List<Invoice>?> GetErpInvoicesAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<Invoice>>("/api/erp/finance/invoices");
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<Payment>?> GetErpPaymentsAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<Payment>>("/api/erp/finance/payments");
            }
            catch
            {
                return null;
            }
        }

        public async Task<Invoice?> CreateErpInvoiceAsync(CreateInvoiceRequest request)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PostAsJsonAsync("/api/erp/finance/invoices", request));
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Invoice>(_jsonOptions);
                }
            }
            catch
            {
            }
            return null;
        }

        public async Task<Payment?> RecordErpPaymentAsync(RecordPaymentRequest request)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PostAsJsonAsync("/api/erp/finance/payments", request));
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Payment>(_jsonOptions);
                }
            }
            catch
            {
            }
            return null;
        }

        public async Task<FinancialSummary?> GetErpFinancialSummaryAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<FinancialSummary>("/api/erp/finance/summary");
            }
            catch
            {
                return null;
            }
        }

        // ============================================
        // SETTINGS / EMAIL API
        // ============================================

        public async Task<EmailSettingsResponse?> GetEmailSettingsAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<EmailSettingsResponse>("/api/settings/email");
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool Success, string Message)> SendTestEmailAsync(string testEmail)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PostAsJsonAsync("/api/settings/email/test", new { testEmail }));
                var result = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
                var message = result.TryGetProperty("message", out var msg) ? msg.GetString() ?? "" : "";

                return (response.IsSuccessStatusCode, message);
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> SaveEmailSettingsAsync(object emailSettings)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PutAsJsonAsync("/api/settings/email", emailSettings));
                var result = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
                var message = result.TryGetProperty("message", out var msg) ? msg.GetString() ?? "" : "";
                return (response.IsSuccessStatusCode, message);
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        // ============================================
        // SUPER ADMIN API
        // ============================================

        public async Task<SystemStats?> GetSuperAdminStatsAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<SystemStats>("/api/SuperAdmin/stats");
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<AuditLog>?> GetAuditLogsAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<AuditLog>>("/api/SuperAdmin/audit-logs");
            }
            catch
            {
                return [];
            }
        }

        public async Task<bool> PromoteUserAsync(int userId, UserRole newRole)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PostAsJsonAsync($"/api/SuperAdmin/users/{userId}/promote", newRole));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<User>?> GetSuperAdminUsersAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<User>>("/api/SuperAdmin/users");
            }
            catch
            {
                return [];
            }
        }

        public async Task<List<User>?> GetRecentUserActivityAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<User>>("/api/SuperAdmin/users/recent-activity");
            }
            catch
            {
                return [];
            }
        }

        public async Task<ModuleAccessConfig?> GetModuleAccessConfigAsync()
        {
            var result = await GetModuleAccessConfigWithDetailsAsync();
            return result.Config;
        }

        public async Task<(ModuleAccessConfig? Config, string? Error)> GetModuleAccessConfigWithDetailsAsync()
        {
            var attempted = new List<string>();
            var endpoints = new[] { "/api/module-access", "/api/superadmin/module-access" };

            foreach (var endpoint in endpoints)
            {
                try
                {
                    var response = await SendWithAuthAsync(() => _httpClient.GetAsync(endpoint));
                    if (response.IsSuccessStatusCode)
                    {
                        var config = await response.Content.ReadFromJsonAsync<ModuleAccessConfig>(_jsonOptions);
                        return (config, null);
                    }

                    var body = await response.Content.ReadAsStringAsync();
                    var message = ExtractApiErrorMessage(body, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
                    attempted.Add($"{endpoint}: {message}");
                }
                catch (Exception ex)
                {
                    attempted.Add($"{endpoint}: {ex.Message}");
                }
            }

            var detail = attempted.Count > 0
                ? string.Join(" | ", attempted)
                : "No endpoint attempts were recorded.";
            return (null, detail);
        }

        public async Task<ModuleAccessConfig?> UpdateModuleAccessConfigAsync(ModuleAccessConfig config)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PutAsJsonAsync("/api/SuperAdmin/module-access", config));
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<ModuleAccessConfig>(_jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<SubscriptionPlanModel>?> GetSubscriptionPlansAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<SubscriptionPlanModel>>("/api/superadmin/governance/subscription/plans");
            }
            catch
            {
                return [];
            }
        }

        public async Task<SubscriptionPlanModel?> CreateSubscriptionPlanAsync(UpsertSubscriptionPlanRequest request)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PostAsJsonAsync("/api/superadmin/governance/subscription/plans", request));
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<SubscriptionPlanModel>(_jsonOptions);
                }
            }
            catch
            {
            }
            return null;
        }

        public async Task<SubscriptionPlanModel?> UpdateSubscriptionPlanAsync(int planId, UpsertSubscriptionPlanRequest request)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PutAsJsonAsync($"/api/superadmin/governance/subscription/plans/{planId}", request));
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<SubscriptionPlanModel>(_jsonOptions);
                }
            }
            catch
            {
            }
            return null;
        }

        public async Task<TenantSubscriptionModel?> GetCurrentSubscriptionAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<TenantSubscriptionModel>("/api/superadmin/governance/subscription/current");
            }
            catch
            {
                return null;
            }
        }

        public async Task<TenantSubscriptionModel?> UpdateCurrentSubscriptionAsync(UpdateTenantSubscriptionRequest request)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PutAsJsonAsync("/api/superadmin/governance/subscription/current", request));
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<TenantSubscriptionModel>(_jsonOptions);
                }
            }
            catch
            {
            }
            return null;
        }

        public async Task<TenantSubscriptionModel?> GetMySubscriptionAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<TenantSubscriptionModel>("/api/subscription/current");
            }
            catch
            {
                return null;
            }
        }

        public async Task<SubscriptionUsageOverviewModel?> GetSubscriptionUsageOverviewAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<SubscriptionUsageOverviewModel>("/api/entitlements/subscription-usage");
            }
            catch
            {
                return null;
            }
        }

        public async Task<ModuleEntitlementResultModel?> GetModuleEntitlementAsync(string moduleKey)
        {
            if (string.IsNullOrWhiteSpace(moduleKey))
            {
                return null;
            }

            try
            {
                var encoded = Uri.EscapeDataString(moduleKey.Trim());
                return await GetFromJsonWithAuthAsync<ModuleEntitlementResultModel>($"/api/entitlements/modules/{encoded}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<CheckoutSessionResponseModel?> CreatePlanChangeCheckoutSessionAsync(CreatePlanChangeCheckoutRequest request)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PostAsJsonAsync("/api/subscription/change-plan-checkout", request));
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<CheckoutSessionResponseModel>(_jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> ConfirmPlanChangeCheckoutAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return false;
            }

            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PostAsJsonAsync(
                    "/api/subscription/confirm-plan-change",
                    new { sessionId }));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<PlanModuleEntitlementModel>?> GetPlanModuleEntitlementsAsync(int planId)
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<PlanModuleEntitlementModel>>($"/api/superadmin/governance/subscription/plans/{planId}/modules");
            }
            catch
            {
                return [];
            }
        }

        public async Task<List<PlanModuleEntitlementModel>?> UpdatePlanModuleEntitlementsAsync(int planId, UpdatePlanModuleEntitlementsRequest request)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PutAsJsonAsync(
                    $"/api/superadmin/governance/subscription/plans/{planId}/modules",
                    request));
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<List<PlanModuleEntitlementModel>>(_jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public async Task<SuperAdminGovernanceAnalyticsModel?> GetGovernanceAnalyticsOverviewAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<SuperAdminGovernanceAnalyticsModel>("/api/superadmin/governance/analytics/overview");
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<BranchModel>?> GetBranchesAsync()
        {
            try
            {
                return await GetFromJsonWithAuthAsync<List<BranchModel>>("/api/superadmin/governance/branches");
            }
            catch
            {
                return [];
            }
        }

        public async Task<BranchModel?> CreateBranchAsync(UpsertBranchRequest request)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PostAsJsonAsync("/api/superadmin/governance/branches", request));
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<BranchModel>(_jsonOptions);
                }
            }
            catch
            {
            }
            return null;
        }

        public async Task<BranchModel?> UpdateBranchAsync(int branchId, UpsertBranchRequest request)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PutAsJsonAsync($"/api/superadmin/governance/branches/{branchId}", request));
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<BranchModel>(_jsonOptions);
                }
            }
            catch
            {
            }
            return null;
        }

        public async Task<bool> DeactivateBranchAsync(int branchId)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.DeleteAsync($"/api/superadmin/governance/branches/{branchId}"));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AssignUserBranchAsync(int userId, int? branchId)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PutAsJsonAsync(
                    "/api/superadmin/governance/users/assign-branch",
                    new { UserId = userId, BranchId = branchId }));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CreateUserAsync(CreateUserRequest request)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.PostAsJsonAsync("/api/SuperAdmin/users", request));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteSuperAdminUserAsync(int userId)
        {
            try
            {
                var response = await SendWithAuthAsync(() => _httpClient.DeleteAsync($"/api/SuperAdmin/users/{userId}"));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            // HttpClient is managed by DI - do not dispose
            GC.SuppressFinalize(this);
        }
    }
}
