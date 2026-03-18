using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context;

        public UsersController(IUserService userService, ApplicationDbContext context)
        {
            _userService = userService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive)
                .AsQueryable();

            if (!User.IsInRole("SuperAdmin"))
            {
                if (User.IsInRole("Customer"))
                {
                    var currentUserId = GetCurrentUserId();
                    if (!currentUserId.HasValue)
                    {
                        return Forbid();
                    }

                    query = query.Where(u => u.Id == currentUserId.Value);
                }
                else
                {
                    var currentBranchId = await GetCurrentUserBranchIdAsync();
                    if (!currentBranchId.HasValue)
                    {
                        return Ok(new List<UserDto>());
                    }

                    query = query.Where(u => u.BranchId == currentBranchId.Value);
                }
            }

            var users = await query
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

            return Ok(users);
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetUsersPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string? search = null,
            [FromQuery] UserRole? role = null,
            [FromQuery] bool? isActive = true,
            [FromQuery] string? sortBy = "name",
            [FromQuery] string? sortDir = "asc")
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var query = _context.Users.AsNoTracking().AsQueryable();

            if (!User.IsInRole("SuperAdmin"))
            {
                if (User.IsInRole("Customer"))
                {
                    var currentUserId = GetCurrentUserId();
                    if (!currentUserId.HasValue)
                    {
                        return Forbid();
                    }

                    query = query.Where(u => u.Id == currentUserId.Value);
                }
                else
                {
                    var currentBranchId = await GetCurrentUserBranchIdAsync();
                    if (!currentBranchId.HasValue)
                    {
                        return Ok(PagedResultDto<UserDto>.Create(new List<UserDto>(), page, pageSize, 0));
                    }

                    query = query.Where(u => u.BranchId == currentBranchId.Value);
                }
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(term) ||
                    u.LastName.ToLower().Contains(term) ||
                    u.Email.ToLower().Contains(term));
            }

            var isDesc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            query = (sortBy ?? "name").ToLowerInvariant() switch
            {
                "email" => isDesc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                "role" => isDesc ? query.OrderByDescending(u => u.Role) : query.OrderBy(u => u.Role),
                "createdat" => isDesc ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
                "lastloginat" => isDesc ? query.OrderByDescending(u => u.LastLoginAt) : query.OrderBy(u => u.LastLoginAt),
                "lastname" => isDesc ? query.OrderByDescending(u => u.LastName).ThenByDescending(u => u.FirstName) : query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName),
                _ => isDesc ? query.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName) : query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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

            return Ok(PagedResultDto<UserDto>.Create(items, page, pageSize, totalCount));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            if (!await CanAccessUserAsync(id))
            {
                return Forbid();
            }

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            // Enforce Creation Hierarchy:
            // SuperAdmin -> Admin
            // Admin -> Supervisor or Agent
            // Supervisor -> Agent
            // Customer -> self-registers via /register
            bool isAuthorized = false;

            if (User.IsInRole("SuperAdmin"))
            {
                isAuthorized = createUserDto.Role == UserRole.Admin;
            }
            else if (User.IsInRole("Admin"))
            {
                isAuthorized = createUserDto.Role == UserRole.Supervisor || createUserDto.Role == UserRole.Agent;
            }
            else if (User.IsInRole("Supervisor"))
            {
                isAuthorized = createUserDto.Role == UserRole.Agent;
            }

            if (!isAuthorized)
            {
                return Forbid();
            }

            if (createUserDto.Role is UserRole.Admin or UserRole.Supervisor or UserRole.Agent)
            {
                var creatorBranchId = await GetCurrentUserBranchIdAsync();
                if (User.IsInRole("Admin") || User.IsInRole("Supervisor"))
                {
                    // Enforce same-branch creation for non-superadmin hierarchy.
                    createUserDto.BranchId = creatorBranchId;
                }
                else if (!createUserDto.BranchId.HasValue)
                {
                    // SuperAdmin or other elevated contexts can auto-fallback to their own branch.
                    createUserDto.BranchId = creatorBranchId;
                }

                if (!createUserDto.BranchId.HasValue || createUserDto.BranchId <= 0)
                {
                    return BadRequest(new
                    {
                        message = "Branch assignment is required for Admin, Supervisor, and Agent accounts."
                    });
                }
            }

            if (createUserDto.Role != UserRole.Customer)
            {
                var userLimitMessage = await GetUserLimitValidationMessageAsync();
                if (!string.IsNullOrWhiteSpace(userLimitMessage))
                {
                    return BadRequest(new { message = userLimitMessage });
                }
            }

            var existingUser = await _userService.GetUserByEmailAsync(createUserDto.Email);
            if (existingUser != null)
            {
                return Conflict(new { message = "User with this email already exists" });
            }

            var currentUserName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System Administrator";
            var user = await _userService.CreateUserAsync(createUserDto, currentUserName);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            if (!await CanAccessUserAsync(id))
            {
                return Forbid();
            }

            if (updateUserDto.IsActive == true)
            {
                var existing = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                if (existing != null && !existing.IsActive && existing.Role != UserRole.Customer)
                {
                    var userLimitMessage = await GetUserLimitValidationMessageAsync();
                    if (!string.IsNullOrWhiteSpace(userLimitMessage))
                    {
                        return BadRequest(new { message = userLimitMessage });
                    }
                }
            }

            var user = await _userService.UpdateUserAsync(id, updateUserDto);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!await CanAccessUserAsync(id))
            {
                return Forbid();
            }

            var result = await _userService.DeleteUserAsync(id);
            if (!result)
            {
                return NotFound(new { message = "User not found" });
            }

            return NoContent();
        }

        private async Task<string?> GetUserLimitValidationMessageAsync()
        {
            var currentSubscription = await _context.TenantSubscriptions
                .Include(s => s.SubscriptionPlan)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            var userLimit = currentSubscription?.SubscriptionPlan?.MaxUsers;
            if (!userLimit.HasValue || userLimit.Value <= 0)
            {
                return null;
            }

            var activeBillableUsers = await _context.Users.CountAsync(u =>
                u.IsActive &&
                u.Role != UserRole.Customer &&
                u.Role != UserRole.SuperAdmin);

            if (activeBillableUsers >= userLimit.Value)
            {
                return $"User limit reached for current subscription plan. Allowed: {userLimit.Value}.";
            }

            return null;
        }

        private async Task<int?> GetCurrentUserBranchIdAsync()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.BranchId)
                .FirstOrDefaultAsync();
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private async Task<bool> CanAccessUserAsync(int targetUserId)
        {
            if (User.IsInRole("SuperAdmin"))
            {
                return true;
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return false;
            }

            if (User.IsInRole("Customer"))
            {
                return currentUserId.Value == targetUserId;
            }

            var currentBranchId = await GetCurrentUserBranchIdAsync();
            if (!currentBranchId.HasValue)
            {
                return false;
            }

            return await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == targetUserId && u.BranchId == currentBranchId.Value);
        }
    }
}
