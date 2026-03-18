using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public interface ILocationService
    {
        Task<IEnumerable<CountryDto>> GetCountriesAsync();
        Task<IEnumerable<RegionDto>> GetRegionsByCountryAsync(string countryCode);
        Task<IEnumerable<ProvinceDto>> GetProvincesByRegionAsync(string countryCode, string regionCode);
        Task<IEnumerable<CityDto>> GetCitiesByProvinceAsync(string countryCode, string provinceCode);
        Task<IEnumerable<BarangayDto>> GetBarangaysByCityAsync(string countryCode, string cityCode);

        // Legacy/Generic support
        Task<IEnumerable<StateDto>> GetStatesByCountryAsync(string countryCode);
        Task<IEnumerable<CityDto>> GetCitiesByStateAsync(string countryCode, string stateCode);
    }
}
