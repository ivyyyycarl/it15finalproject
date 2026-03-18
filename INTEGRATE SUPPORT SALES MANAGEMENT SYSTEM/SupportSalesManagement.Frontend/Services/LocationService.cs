using System.Net.Http.Json;
using SupportSalesManagement.Frontend.Models;

namespace SupportSalesManagement.Frontend.Services
{
    public class LocationService
    {
        private readonly HttpClient _httpClient;

        public LocationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Country>> GetCountriesAsync()
        {
            try
            {
                var countries = await _httpClient.GetFromJsonAsync<List<Country>>("api/location/countries");
                return countries ?? new List<Country>();
            }
            catch
            {
                return new List<Country>();
            }
        }

        public async Task<List<Region>> GetRegionsAsync(string countryCode)
        {
            if (string.IsNullOrEmpty(countryCode)) return new List<Region>();
            try
            {
                var regions = await _httpClient.GetFromJsonAsync<List<Region>>($"api/location/regions/{countryCode}");
                return regions ?? new List<Region>();
            }
            catch
            {
                return new List<Region>();
            }
        }

        public async Task<List<Province>> GetProvincesAsync(string countryCode, string regionCode)
        {
            if (string.IsNullOrEmpty(countryCode) || string.IsNullOrEmpty(regionCode)) return new List<Province>();
            try
            {
                var provinces = await _httpClient.GetFromJsonAsync<List<Province>>($"api/location/provinces/{countryCode}/{regionCode}");
                return provinces ?? new List<Province>();
            }
            catch
            {
                return new List<Province>();
            }
        }

        public async Task<List<City>> GetCitiesByProvinceAsync(string countryCode, string provinceCode)
        {
            if (string.IsNullOrEmpty(countryCode) || string.IsNullOrEmpty(provinceCode)) return new List<City>();
            try
            {
                var cities = await _httpClient.GetFromJsonAsync<List<City>>($"api/location/cities-by-province/{countryCode}/{provinceCode}");
                return cities ?? new List<City>();
            }
            catch
            {
                return new List<City>();
            }
        }

        public async Task<List<Barangay>> GetBarangaysAsync(string countryCode, string cityCode)
        {
            if (string.IsNullOrEmpty(countryCode) || string.IsNullOrEmpty(cityCode)) return new List<Barangay>();
            try
            {
                var barangays = await _httpClient.GetFromJsonAsync<List<Barangay>>($"api/location/barangays/{countryCode}/{cityCode}");
                return barangays ?? new List<Barangay>();
            }
            catch
            {
                return new List<Barangay>();
            }
        }

        // Legacy/Generic support
        public async Task<List<State>> GetStatesAsync(string countryCode)
        {
            if (string.IsNullOrEmpty(countryCode)) return new List<State>();
            try
            {
                var states = await _httpClient.GetFromJsonAsync<List<State>>($"api/location/states/{countryCode}");
                return states ?? new List<State>();
            }
            catch
            {
                return new List<State>();
            }
        }

        public async Task<List<City>> GetCitiesAsync(string countryCode, string stateCode)
        {
            if (string.IsNullOrEmpty(countryCode) || string.IsNullOrEmpty(stateCode)) return new List<City>();
            try
            {
                var cities = await _httpClient.GetFromJsonAsync<List<City>>($"api/location/cities/{countryCode}/{stateCode}");
                return cities ?? new List<City>();
            }
            catch
            {
                return new List<City>();
            }
        }
    }
}
