namespace SupportSalesManagement.Frontend.Models
{
    public class Country
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class State
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class Region
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class Province
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class City
    {
        public string Name { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
    }

    public class Barangay
    {
        public string Name { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
    }
}
