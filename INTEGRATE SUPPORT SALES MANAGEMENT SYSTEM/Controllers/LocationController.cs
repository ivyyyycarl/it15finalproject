using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using Microsoft.AspNetCore.Mvc;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _locationService;

        public LocationController(ILocationService locationService)
        {
            _locationService = locationService;
        }

        [HttpGet("countries")]
        public async Task<IActionResult> GetCountries()
        {
            var countries = await _locationService.GetCountriesAsync();
            return Ok(countries);
        }

        [HttpGet("regions/{countryCode}")]
        public async Task<IActionResult> GetRegions(string countryCode)
        {
            var regions = await _locationService.GetRegionsByCountryAsync(countryCode);
            return Ok(regions);
        }

        [HttpGet("provinces/{countryCode}/{regionCode}")]
        public async Task<IActionResult> GetProvinces(string countryCode, string regionCode)
        {
            var provinces = await _locationService.GetProvincesByRegionAsync(countryCode, regionCode);
            return Ok(provinces);
        }

        [HttpGet("cities-by-province/{countryCode}/{provinceCode}")]
        public async Task<IActionResult> GetCitiesByProvince(string countryCode, string provinceCode)
        {
            var cities = await _locationService.GetCitiesByProvinceAsync(countryCode, provinceCode);
            return Ok(cities);
        }

        [HttpGet("barangays/{countryCode}/{cityCode}")]
        public async Task<IActionResult> GetBarangays(string countryCode, string cityCode)
        {
            var barangays = await _locationService.GetBarangaysByCityAsync(countryCode, cityCode);
            return Ok(barangays);
        }

        [HttpGet("states/{countryCode}")]
        public async Task<IActionResult> GetStates(string countryCode)
        {
            var states = await _locationService.GetStatesByCountryAsync(countryCode);
            return Ok(states);
        }

        [HttpGet("cities/{countryCode}/{stateCode}")]
        public async Task<IActionResult> GetCities(string countryCode, string stateCode)
        {
            var cities = await _locationService.GetCitiesByStateAsync(countryCode, stateCode);
            return Ok(cities);
        }
    }
}
