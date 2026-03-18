using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public class LocationService : ILocationService
    {
        private static readonly List<CountryDto> Countries = new()
        {
            new CountryDto { Code = "PH", Name = "Philippines" },
            new CountryDto { Code = "US", Name = "United States" },
            new CountryDto { Code = "CA", Name = "Canada" },
            new CountryDto { Code = "UK", Name = "United Kingdom" },
            new CountryDto { Code = "AU", Name = "Australia" },
            new CountryDto { Code = "NG", Name = "Nigeria" }
        };

        // Philippines Hierarchy
        private static readonly Dictionary<string, List<RegionDto>> Regions = new()
        {
            ["PH"] = new()
            {
                new RegionDto { Code = "NCR", Name = "National Capital Region (NCR)" },
                new RegionDto { Code = "R11", Name = "Davao Region (Region XI)" },
                new RegionDto { Code = "R4A", Name = "CALABARZON (Region IV-A)" }
            }
        };

        private static readonly Dictionary<string, List<ProvinceDto>> Provinces = new()
        {
            ["PH_NCR"] = new()
            {
                new ProvinceDto { Code = "MM", Name = "Metro Manila" }
            },
            ["PH_R11"] = new()
            {
                new ProvinceDto { Code = "DVO_SUR", Name = "Davao del Sur" },
                new ProvinceDto { Code = "DVO_NOR", Name = "Davao del Norte" }
            }
        };

        private static readonly Dictionary<string, List<CityDto>> CitiesByProvince = new()
        {
            ["PH_MM"] = new()
            {
                new CityDto { Name = "Makati" },
                new CityDto { Name = "Quezon City" },
                new CityDto { Name = "Pasig" }
            },
            ["PH_DVO_SUR"] = new()
            {
                new CityDto { Name = "Davao City" },
                new CityDto { Name = "Digos City" }
            }
        };

        private static readonly Dictionary<string, List<BarangayDto>> Barangays = new()
        {
            ["PH_Makati"] = new()
            {
                new BarangayDto { Name = "Bel-Air", PostalCode = "1209" },
                new BarangayDto { Name = "Poblacion", PostalCode = "1210" },
                new BarangayDto { Name = "San Lorenzo", PostalCode = "1223" }
            },
            ["PH_Davao City"] = new()
            {
                new BarangayDto { Name = "Buhangin", PostalCode = "8000" },
                new BarangayDto { Name = "Talomo", PostalCode = "8000" },
                new BarangayDto { Name = "Agdao", PostalCode = "8000" }
            }
        };

        // Legacy/Generic Support (e.g., for US)
        private static readonly Dictionary<string, List<StateDto>> StatesByCountry = new()
        {
            ["US"] = new()
            {
                new StateDto { Code = "NY", Name = "New York" },
                new StateDto { Code = "CA", Name = "California" }
            }
        };

        private static readonly Dictionary<string, List<CityDto>> CitiesByState = new()
        {
            ["US_NY"] = new()
            {
                new CityDto { Name = "New York City", PostalCode = "10001" }
            },
            ["US_CA"] = new()
            {
                new CityDto { Name = "Los Angeles", PostalCode = "90001" }
            }
        };

        public Task<IEnumerable<CountryDto>> GetCountriesAsync() => Task.FromResult(Countries.AsEnumerable());

        public Task<IEnumerable<RegionDto>> GetRegionsByCountryAsync(string countryCode)
        {
            return Task.FromResult(Regions.TryGetValue(countryCode, out var list) ? list.AsEnumerable() : Enumerable.Empty<RegionDto>());
        }

        public Task<IEnumerable<ProvinceDto>> GetProvincesByRegionAsync(string countryCode, string regionCode)
        {
            var key = $"{countryCode}_{regionCode}";
            return Task.FromResult(Provinces.TryGetValue(key, out var list) ? list.AsEnumerable() : Enumerable.Empty<ProvinceDto>());
        }

        public Task<IEnumerable<CityDto>> GetCitiesByProvinceAsync(string countryCode, string provinceCode)
        {
            var key = $"{countryCode}_{provinceCode}";
            return Task.FromResult(CitiesByProvince.TryGetValue(key, out var list) ? list.AsEnumerable() : Enumerable.Empty<CityDto>());
        }

        public Task<IEnumerable<BarangayDto>> GetBarangaysByCityAsync(string countryCode, string cityCode)
        {
            var key = $"{countryCode}_{cityCode}";
            return Task.FromResult(Barangays.TryGetValue(key, out var list) ? list.AsEnumerable() : Enumerable.Empty<BarangayDto>());
        }

        public Task<IEnumerable<StateDto>> GetStatesByCountryAsync(string countryCode)
        {
            return Task.FromResult(StatesByCountry.TryGetValue(countryCode, out var list) ? list.AsEnumerable() : Enumerable.Empty<StateDto>());
        }

        public Task<IEnumerable<CityDto>> GetCitiesByStateAsync(string countryCode, string stateCode)
        {
            var key = $"{countryCode}_{stateCode}";
            return Task.FromResult(CitiesByState.TryGetValue(key, out var list) ? list.AsEnumerable() : Enumerable.Empty<CityDto>());
        }
    }
}
