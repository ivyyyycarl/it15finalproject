using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ICustomerService _customerService;
        private readonly IConfiguration _configuration;

        public AuthController(IUserService userService, ICustomerService customerService, IConfiguration configuration)
        {
            _userService = userService;
            _customerService = customerService;
            _configuration = configuration;
        }

        [EnableRateLimiting("auth")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _userService.LoginAsync(loginDto);

            if (result == null)
            {
                if (!string.IsNullOrWhiteSpace(loginDto.Email))
                {
                    var existingUser = await _userService.GetUserByEmailAsync(loginDto.Email.Trim());
                    if (existingUser != null && !existingUser.IsActive)
                    {
                        return Unauthorized(new
                        {
                            message = "Account is not active yet. Complete subscription activation (success page/webhook) before logging in."
                        });
                    }
                }

                return Unauthorized(new { message = "Invalid email or password" });
            }

            return Ok(result);
        }

        [EnableRateLimiting("auth")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateUserDto createUserDto)
        {
            if (!createUserDto.AcceptTerms)
            {
                return BadRequest(new { message = "You must accept the Terms and Conditions before creating an account." });
            }

            var existingUser = await _userService.GetUserByEmailAsync(createUserDto.Email);
            if (existingUser != null)
            {
                return Conflict(new { message = "User with this email already exists" });
            }

            createUserDto.Role = UserRole.Customer;

            var user = await _userService.CreateUserAsync(createUserDto);

            try
            {
                await _customerService.CreateCustomerAsync(new CreateCustomerDto
                {
                    FirstName = createUserDto.FirstName,
                    LastName = createUserDto.LastName,
                    Email = createUserDto.Email,
                    Phone = createUserDto.Phone ?? "",
                    UserId = user.Id,
                    Type = CustomerType.Individual
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not auto-create customer profile: {ex.Message}");
            }

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        [Authorize]
        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            var result = await _userService.ChangePasswordAsync(
                changePasswordDto.UserId,
                changePasswordDto.CurrentPassword,
                changePasswordDto.NewPassword);

            if (!result)
            {
                return Unauthorized(new { message = "Failed to change password. Please check your current password." });
            }

            return Ok(new { message = "Password changed successfully" });
        }

        [EnableRateLimiting("auth")]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            var result = await _userService.ResetPasswordAsync(resetPasswordDto.Email);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }
    }

    public class ChangePasswordDto
    {
        public int UserId { get; set; }
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;
        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
