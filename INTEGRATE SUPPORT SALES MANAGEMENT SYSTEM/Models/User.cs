using System.ComponentModel.DataAnnotations;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; } = UserRole.Agent;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int? BranchId { get; set; }

        public virtual Branch? Branch { get; set; }

        // Navigation properties
        public virtual ICollection<Call> Calls { get; set; } = new List<Call>();
        public virtual ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<PerformanceReport> PerformanceReports { get; set; } = new List<PerformanceReport>();
    }

    public enum UserRole
    {
        Customer = 0,
        Agent = 1,
        Supervisor = 2,
        Admin = 3,
        SuperAdmin = 4
    }
}
