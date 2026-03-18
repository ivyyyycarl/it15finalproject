using Microsoft.AspNetCore.Components;
using SupportSalesManagement.Frontend.Models;

namespace SupportSalesManagement.Frontend.Services
{
    public class AuthenticationService(ApiClient apiClient, NavigationManager navigationManager)
    {
        private readonly ApiClient _apiClient = apiClient;
        private readonly NavigationManager _navigationManager = navigationManager;
        public User? CurrentUser { get; private set; }
        public bool IsAuthenticated => CurrentUser != null;
        public string? LastLoginError { get; private set; }

        public event Action? OnAuthenticationChanged;

        public async Task<bool> LoginAsync(LoginRequest loginRequest)
        {
            LastLoginError = null;
            var response = await _apiClient.LoginAsync(loginRequest);
            if (response != null)
            {
                CurrentUser = response.User;
                OnAuthenticationChanged?.Invoke();
                return true;
            }

            LastLoginError = _apiClient.LastLoginError ?? "Invalid email or password. Please try again.";
            return false;
        }

        public async Task<(bool Success, string Message)> RegisterAsync(RegisterRequest registerRequest)
        {
            return await _apiClient.RegisterAsync(registerRequest);
        }

        public async Task LogoutAsync()
        {
            await _apiClient.LogoutAsync();
            CurrentUser = null;
            OnAuthenticationChanged?.Invoke();
            _navigationManager.NavigateTo("/login");
        }

        public async Task InitializeAsync()
        {
            try
            {
                var initTask = _apiClient.GetCurrentUserAsync();
                var timeoutTask = Task.Delay(5000);

                if (await Task.WhenAny(initTask, timeoutTask) == initTask)
                {
                    CurrentUser = await initTask;
                }

                OnAuthenticationChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AuthService Initialization FAILED: {ex.Message}");
            }
        }
    }
}
