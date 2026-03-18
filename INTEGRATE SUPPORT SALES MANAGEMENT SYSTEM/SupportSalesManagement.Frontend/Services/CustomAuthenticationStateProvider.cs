using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using SupportSalesManagement.Frontend.Models;

namespace SupportSalesManagement.Frontend.Services
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly AuthenticationService _authService;

        public CustomAuthenticationStateProvider(AuthenticationService authService)
        {
            _authService = authService;
            _authService.OnAuthenticationChanged += AuthStateChanged;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (_authService.CurrentUser == null)
            {
                // Verify session persistence if needed, for now start as anonymous
                // Assuming InitializeAsync handled in App.razor or main layout
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, _authService.CurrentUser.Username ?? _authService.CurrentUser.Email ?? "User"),
                new Claim(ClaimTypes.Email, _authService.CurrentUser.Email ?? ""),
                new Claim(ClaimTypes.NameIdentifier, _authService.CurrentUser.Id.ToString())
            };

            // Map enum role to string role name
            var roleName = _authService.CurrentUser.Role.ToString();
            claims.Add(new Claim(ClaimTypes.Role, roleName));

            // Allow SuperAdmin to access Admin pages
            if (_authService.CurrentUser.Role == UserRole.SuperAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var identity = new ClaimsIdentity(claims, "CustomAuth");
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }

        private void AuthStateChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public void Dispose()
        {
            _authService.OnAuthenticationChanged -= AuthStateChanged;
        }
    }
}
