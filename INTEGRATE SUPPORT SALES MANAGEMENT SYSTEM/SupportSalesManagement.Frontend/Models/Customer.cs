using System.Text.Json.Serialization;
using System.Text.Json;

namespace SupportSalesManagement.Frontend.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? CreatedByUserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        [JsonPropertyName("type")]
        [JsonConverter(typeof(CustomerTypeStringJsonConverter))]
        public string Type { get; set; } = "Individual";
        public string CustomerType
        {
            get => string.IsNullOrWhiteSpace(Type) ? "Individual" : Type;
            set => Type = string.IsNullOrWhiteSpace(value) ? "Individual" : value;
        }
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public sealed class CustomerTypeStringJsonConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();
                return string.IsNullOrWhiteSpace(value) ? "Individual" : value;
            }

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var numberValue))
            {
                return numberValue switch
                {
                    1 => "Individual",
                    _ => numberValue.ToString()
                };
            }

            return "Individual";
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(string.IsNullOrWhiteSpace(value) ? "Individual" : value);
        }
    }

    public class CreateCustomerRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        [JsonPropertyName("type")]
        public string CustomerType { get; set; } = "Individual";
        public int? UserId { get; set; }
        public int? BranchId { get; set; }
    }

    public class UpdateCustomerRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        [JsonPropertyName("type")]
        public string CustomerType { get; set; } = "Individual";
        public bool IsActive { get; set; }
    }
}
