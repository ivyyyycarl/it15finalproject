using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserService> _logger;
        private readonly IEmailService _emailService;

        public UserService(ApplicationDbContext context, IConfiguration configuration, ILogger<UserService> logger, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    BranchId = u.BranchId,
                    BranchName = u.Branch != null ? u.Branch.Name : null,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    Phone = u.Phone,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                })
                .ToListAsync();
        }

        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == id && u.IsActive)
                .Select(user => new UserDto
                {
                    Id = user.Id,
                    BranchId = user.BranchId,
                    BranchName = user.Branch != null ? user.Branch.Name : null,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            var normalizedEmail = email.ToLower();
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Email.ToLower() == normalizedEmail)
                .Select(user => new UserDto
                {
                    Id = user.Id,
                    BranchId = user.BranchId,
                    BranchName = user.Branch != null ? user.Branch.Name : null,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            return await CreateUserAsync(createUserDto, null);
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto, string? createdByName)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == createUserDto.Email.ToLower());

            if (existingUser != null)
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            var plainPassword = createUserDto.Password;
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
            var resolvedBranchId = createUserDto.BranchId;

            if (createUserDto.Role == UserRole.Customer && !resolvedBranchId.HasValue)
            {
                resolvedBranchId = await _context.Branches
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.Id)
                    .Select(b => (int?)b.Id)
                    .FirstOrDefaultAsync();
            }

            var user = new User
            {
                FirstName = createUserDto.FirstName,
                LastName = createUserDto.LastName,
                Email = createUserDto.Email,
                PasswordHash = passwordHash,
                Phone = createUserDto.Phone ?? string.Empty,
                Role = createUserDto.Role,
                BranchId = resolvedBranchId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (user.Role == UserRole.Customer)
            {
                var resolvedCompany = string.IsNullOrWhiteSpace(createUserDto.Company)
                    ? await _context.TenantSubscriptions
                        .Where(t => !string.IsNullOrWhiteSpace(t.TenantName))
                        .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
                        .Select(t => t.TenantName)
                        .FirstOrDefaultAsync()
                    : createUserDto.Company.Trim();

                var customer = new Customer
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    UserId = user.Id,
                    Company = resolvedCompany,
                    Address = createUserDto.Address,
                    City = createUserDto.City,
                    State = createUserDto.State,
                    PostalCode = createUserDto.PostalCode,
                    Country = createUserDto.Country,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            // Send email notification
            try
            {
                if (!string.IsNullOrEmpty(createdByName))
                {
                    await _emailService.SendAccountCreatedByAdminEmailAsync(
                        user.Email, user.FirstName, user.LastName,
                        user.Role.ToString(), plainPassword, createdByName);
                }
                else
                {
                    await _emailService.SendWelcomeEmailAsync(
                        user.Email, user.FirstName, user.LastName,
                        user.Role.ToString(), plainPassword);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send welcome email to {Email}, but account was created successfully", user.Email);
            }

            return new UserDto
            {
                Id = user.Id,
                BranchId = user.BranchId,
                BranchName = null,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }

        public async Task<UserDto?> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return null;

            // Update fields if provided
            if (!string.IsNullOrEmpty(updateUserDto.FirstName))
                user.FirstName = updateUserDto.FirstName;

            if (!string.IsNullOrEmpty(updateUserDto.LastName))
                user.LastName = updateUserDto.LastName;

            if (!string.IsNullOrEmpty(updateUserDto.Email))
            {
                // Check if email is already taken by another user
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == updateUserDto.Email.ToLower() && u.Id != id);

                if (existingUser != null)
                {
                    throw new InvalidOperationException("Email is already taken by another user");
                }

                user.Email = updateUserDto.Email;
            }

            if (!string.IsNullOrEmpty(updateUserDto.Phone))
                user.Phone = updateUserDto.Phone;

            if (updateUserDto.Role.HasValue)
                user.Role = updateUserDto.Role.Value;

            if (updateUserDto.IsActive.HasValue)
                user.IsActive = updateUserDto.IsActive.Value;

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                BranchId = user.BranchId,
                BranchName = null,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return false;

            // Soft delete
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var email = loginDto.Email.Trim().ToLower();
            var password = loginDto.Password;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive);

            if (user == null)
            {
                return null;
            }

            bool bcryptResult = false;
            try
            {
                bcryptResult = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            catch (Exception)
            {
            }

            if (!bcryptResult)
            {
                return null;
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return new LoginResponseDto
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("JwtSettings:ExpiryMinutes")),
                User = new UserDto
                {
                    Id = user.Id,
                    BranchId = user.BranchId,
                    BranchName = user.Branch != null ? user.Branch.Name : null,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                }
            };
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ResetPasswordResult> ResetPasswordAsync(string email)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);

            if (user == null)
            {
                return new ResetPasswordResult(false, "No active user found with that email.");
            }

            var tempPassword = GenerateTemporaryPassword();
            var originalPasswordHash = user.PasswordHash;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            try
            {
                var emailSent = await _emailService.SendPasswordResetEmailAsync(email, user.FirstName, tempPassword);
                if (!emailSent)
                {
                    _logger.LogWarning("Failed to send password reset email to {Email}.", email);
                    user.PasswordHash = originalPasswordHash;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return new ResetPasswordResult(
                        false,
                        "Unable to send reset email via Gmail SMTP. Please verify EmailSettings (SenderEmail, App Password, and EnableEmailNotifications).");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Email sending failed for password reset to {Email}.", email);
                user.PasswordHash = originalPasswordHash;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return new ResetPasswordResult(false, "Password reset email failed to send. Please try again.");
            }

            return new ResetPasswordResult(true, "A temporary password has been sent to your Gmail inbox.");
        }

        public async Task<SystemStatsDto> GetSystemStatsAsync()
        {
            return new SystemStatsDto
            {
                TotalUsers = await _context.Users.CountAsync(u => u.IsActive),
                ActiveAdmins = await _context.Users.CountAsync(u => u.IsActive && (u.Role == UserRole.Admin || u.Role == UserRole.SuperAdmin)),
                TotalTickets = await _context.Tickets.CountAsync(),
                ResolvedTickets = await _context.Tickets.CountAsync(t => t.Status == TicketStatus.Resolved),
                TotalSales = await _context.Orders.SumAsync(o => o.FinalAmount),
                TotalCalls = await _context.Calls.CountAsync(),
                AverageResponseTimeHours = 0,
                SystemUptime = 0
            };
        }

        public async Task LogAuditActionAsync(string action, string description, int? userId = null, string? details = null)
        {
            var auditLog = new AuditLog
            {
                Action = action,
                Description = description,
                UserId = userId,
                UserEmail = userId.HasValue ? (await _context.Users.FindAsync(userId))?.Email ?? "System" : "System",
                Timestamp = DateTime.UtcNow,
                Details = details
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = ResolveJwtSigningKey();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("UserId", user.Id.ToString()),
                new Claim("UserRole", user.Role.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("JwtSettings:ExpiryMinutes")),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string ResolveJwtSigningKey()
        {
            var key = _configuration["JwtSettings:Key"];
            if (string.IsNullOrWhiteSpace(key))
            {
                key = _configuration["JwtSettings__Key"];
            }
            if (string.IsNullOrWhiteSpace(key))
            {
                key = Environment.GetEnvironmentVariable("JWT__KEY")
                      ?? Environment.GetEnvironmentVariable("JwtSettings__Key");
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
                {
                    return "classicfit-dev-jwt-key-change-before-production-2026";
                }

                throw new InvalidOperationException(
                    "JWT signing key is missing. Configure JwtSettings:Key (or JWT__KEY).");
            }

            return key;
        }

        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var password = new char[12];
            var randomBytes = new byte[12];
            System.Security.Cryptography.RandomNumberGenerator.Fill(randomBytes);
            for (int i = 0; i < password.Length; i++)
            {
                password[i] = chars[randomBytes[i] % chars.Length];
            }
            return new string(password);
        }

        public async Task<IEnumerable<UserDto>> GetRecentlyActiveUsersAsync(int count = 10)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive && u.LastLoginAt != null)
                .OrderByDescending(u => u.LastLoginAt)
                .Take(count)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    BranchId = u.BranchId,
                    BranchName = u.Branch != null ? u.Branch.Name : null,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    Phone = u.Phone,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                })
                .ToListAsync();
        }
    }
}
