using System.ComponentModel.DataAnnotations;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs
{
    // Customer DTOs
    public class CustomerDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? CreatedByUserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Company { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string Type { get; set; } = "Individual";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int TotalCalls { get; set; }
        public int OpenTickets { get; set; }
        public int TotalOrders { get; set; }
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public decimal TotalSpent { get; set; }
    }
    
    public class CreateCustomerDto
    {
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
        
        [StringLength(11)]
        public string Phone { get; set; } = string.Empty;
        
        public int? UserId { get; set; }
        public int? CreatedByUserId { get; set; }
        
        [StringLength(255)]
        public string? Company { get; set; }
        
        [StringLength(500)]
        public string? Address { get; set; }
        
        [StringLength(100)]
        public string? City { get; set; }
        
        [StringLength(100)]
        public string? State { get; set; }
        
        [StringLength(20)]
        public string? PostalCode { get; set; }
        
        [StringLength(100)]
        public string? Country { get; set; }
        
        public int? BranchId { get; set; }
        public CustomerType Type { get; set; } = CustomerType.Individual;
    }
    
    public class UpdateCustomerDto
    {
        [StringLength(100)]
        public string? FirstName { get; set; }
        
        [StringLength(100)]
        public string? LastName { get; set; }
        
        [EmailAddress]
        [StringLength(255)]
        public string? Email { get; set; }
        
        [StringLength(11)]
        public string? Phone { get; set; }
        
        [StringLength(255)]
        public string? Company { get; set; }
        
        [StringLength(500)]
        public string? Address { get; set; }
        
        [StringLength(100)]
        public string? City { get; set; }
        
        [StringLength(100)]
        public string? State { get; set; }
        
        [StringLength(20)]
        public string? PostalCode { get; set; }
        
        [StringLength(100)]
        public string? Country { get; set; }
        
        public CustomerType? Type { get; set; }
    }
    
    public class CustomerInteractionDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public List<CallDto> RecentCalls { get; set; } = new();
        public List<TicketDto> RecentTickets { get; set; } = new();
        public List<OrderDto> RecentOrders { get; set; } = new();
    }
}
