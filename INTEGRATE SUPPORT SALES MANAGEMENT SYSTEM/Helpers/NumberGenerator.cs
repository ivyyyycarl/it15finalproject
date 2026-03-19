using System;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Helpers
{
    public static class NumberGenerator
    {
        private static readonly Random _random = new Random();

        public static string GenerateTicketNumber()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = _random.Next(1000, 9999);
            return $"TK-{timestamp}-{random}";
        }

        public static string GenerateOrderNumber()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = _random.Next(1000, 9999);
            return $"ORD-{timestamp}-{random}";
        }

        public static string GenerateSKU(string prefix)
        {
            var timestamp = DateTime.UtcNow.ToString("yyMM");
            var random = _random.Next(1000, 9999);
            return $"{prefix}-{timestamp}-{random}";
        }
    }
}
