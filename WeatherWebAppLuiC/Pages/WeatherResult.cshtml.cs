using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WeatherWebAppLuiC.Models;
using System.Net.Http;
using System.Linq;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

// Similar to Index.cshtml.cs, IHttpClientFactory is added here (Microsoft, 2025f) as well as IConfiguration (Microsoft, 2026b), so that it enables for HTTP requests.
// WeatherResultOutput is being received here so that the weather information is processed.


namespace WeatherWebAppLuiC.Pages
{
    public class WeatherResultModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        public WeatherResultOutput? Weather { get; set; }
        public GeoLocation[]? Candidates { get; set; }
        public string? QueryCity { get; set; }

        public WeatherResultModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task OnGetAsync(string city, double? lat, double? lon)
        {
            QueryCity = city;

            if (string.IsNullOrWhiteSpace(city))
            {
                Weather = null;
                return;
            }

            var apiKey = _configuration["OpenWeather:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey)) return;

            var client = _httpClientFactory.CreateClient();

            // Longtitude/Latitude can be provided to the app to find the weather information for a particular location.
            if (lat.HasValue && lon.HasValue)
            {
                var data = await FetchOneCallDataAsync(client, apiKey, lat.Value, lon.Value);
                if (data == null) return;
                Weather = BuildWeatherResult(city, data);
                return;
            }

            // Cities are provided here as "candidates", which is resolved using the city information from the client and the API key from OpenWeather.
            var candidates = await ResolveCityCandidatesAsync(client, apiKey, city);
            if (candidates == null || candidates.Length == 0) return;

            // Use the first candidate returned by the geocoding API to fetch weather.
            // This avoids leaving the page with no results when multiple matches exist
            // (e.g., many places named "New York").
            var chosen = candidates[0];
            var oneCallData = await FetchOneCallDataAsync(client, apiKey, chosen.Lat, chosen.Lon);
            if (oneCallData == null) return;
            Weather = BuildWeatherResult(city, oneCallData);
            return;
        }

        // In these private async classes (GeoLocation list, OneCallResponse List, WeatherResultOutput), the city information is resolved here to build.
        // Using the OpenWeather API calls for current weather (OpenWeather, 2026a) and Geolocation data API (OpenWeather, 2026b), it is placed here to build the results in
        // the WeatherResult.cshtml page.

        private async Task<GeoLocation[]?> ResolveCityCandidatesAsync(HttpClient client, string apiKey, string city)
        {
            var geoUrl = $"https://api.openweathermap.org/geo/1.0/direct?q={Uri.EscapeDataString(city)}&limit=5&appid={apiKey}";
            var geoResp = await client.GetAsync(geoUrl);
            if (!geoResp.IsSuccessStatusCode) return null;

            var geoJson = await geoResp.Content.ReadAsStringAsync();
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var geoArray = JsonSerializer.Deserialize<GeoLocation[]>(geoJson, options);
                return geoArray;
            }
            catch
            {
                return null;
            }
        }

        private async Task<OneCallResponse?> FetchOneCallDataAsync(HttpClient client, string apiKey, double lat, double lon)
        {
            var url = $"https://api.openweathermap.org/data/3.0/onecall?lat={lat.ToString(CultureInfo.InvariantCulture)}&lon={lon.ToString(CultureInfo.InvariantCulture)}&exclude=minutely,daily,alerts&appid={apiKey}&units=metric";
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<OneCallResponse>(json, options);
            }
            catch
            {
                return null;
            }
        }

        private WeatherResultOutput? BuildWeatherResult(string city, OneCallResponse data)
        {
            var current = data.Current;
            if (current == null) return null;

            var weatherItem = current.Weather?.FirstOrDefault();
            var weatherMain = weatherItem?.Main;
            var weatherDesc = weatherItem?.Description;
            double temp = current.Temp;

            var result = new WeatherResultOutput
            {
                City = city,
                WeatherDescription = weatherDesc,
                CurrentIcon = MapIcon(weatherItem),
                Temps = temp,
                WeatherNow = weatherMain,
                HourlyForecast = new List<HourlyWeather>()
            };

            int timezoneOffsetSeconds = data.TimezoneOffset;
            var hourly = data.Hourly;
            if (hourly == null) return result;

            // For loop utilised to show hourly weather information 12 hours from the current time.
            // DateTime struct is also utilised (Microsoft, 2026c), and the local time zone is also gathered, instead of the current time.

            for (int i = 0; i < 12 && i < hourly.Count; i++)
            {
                var hourData = hourly[i];
                var hourWeather = hourData.Weather?.FirstOrDefault();

                DateTime time;
                try
                {
                    var dto = DateTimeOffset.FromUnixTimeSeconds(hourData.Dt);
                    time = dto.ToOffset(TimeSpan.FromSeconds(timezoneOffsetSeconds)).DateTime;
                }
                catch
                {
                    time = DateTime.UtcNow;
                }

                double hourTemp = hourData.Temp;

                result.HourlyForecast.Add(new HourlyWeather
                {
                    Time = time,
                    Temperature = hourTemp,
                    IconName = MapIcon(hourWeather),
                    WeatherDescription = hourWeather?.Description
                    ,
                    PrecipitationProbabilityPercent = hourData.Pop.HasValue ? (int?)Math.Round(hourData.Pop.Value * 100) : (int?)null
                });
            }

            // Set feels-like temperature (from current data if available from OpenWeather API (OpenWeather, 2026a))
            try
            {
                result.TempsFeelsLikeCelsius = current.FeelsLike;
            }
            catch { result.TempsFeelsLikeCelsius = null; }

            // Derive min/max from hourly forecast when available, which is also gathered from OpenWeather API (2026a)
            if (result.HourlyForecast != null && result.HourlyForecast.Count > 0)
            {
                var min = result.HourlyForecast.Min(h => h.Temperature);
                var max = result.HourlyForecast.Max(h => h.Temperature);
                result.MinTempsCelsius = Math.Round(min, 1);
                result.MaxTempsCelsius = Math.Round(max, 1);
            }

            return result;
        }


        // Weather info and icons are returned to string, allowing for the app to process and gather OpenWeather API icons (OpenWeather, 2026c)
        private string MapIcon(WeatherInfo? weatherItem)
        {
            // Icons are mapped, so that the correct weather icon is displayed, as gathered from the OpenWeather API. (OpenWeather, 2026c)
            try
            {
                
                var apiIcon = weatherItem?.Icon;
                var suffix = 'd';
                if (!string.IsNullOrWhiteSpace(apiIcon))
                {
                    var last = apiIcon[^1];
                    if (last == 'n' || last == 'd') suffix = last;
                }

                // Weather text descriptions from the API (OpenWeather, 2026c) and the numerical icon to represent the weather. (OpenWeather, 2026c)
                var desc = weatherItem?.Description?.ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(desc))
                {
                    string? baseCode = desc switch
                    {
                        "clear sky" => "01",
                        "few clouds" => "02",
                        "scattered clouds" => "03",
                        "broken clouds" => "04",
                        "overcast clouds" => "04",
                        "shower rain" => "09",
                        "light intensity shower rain" => "09",
                        "rain" => "10",
                        "light rain" => "10",
                        "moderate rain" => "10",
                        "thunderstorm" => "11",
                        "snow" => "13",
                        "light snow" => "13",
                        "mist" => "50",
                        "smoke" => "50",
                        "haze" => "50",
                        "fog" => "50",
                        _ => null,
                    };

                    if (!string.IsNullOrWhiteSpace(baseCode))
                    {
                        return $"https://openweathermap.org/img/wn/{baseCode}{suffix}@2x.png";
                    }
                }
            }
            catch { }

        
            try
            {
                var iconCode = weatherItem?.Icon;
                if (!string.IsNullOrWhiteSpace(iconCode))
                {
                    return $"https://openweathermap.org/img/wn/{iconCode}@2x.png";
                }
            }
            catch { }

            // Used for handling exceptions
            try
            {
                var main = weatherItem?.Main?.ToLowerInvariant() ?? string.Empty;
                var baseCode = main switch
                {
                    "clouds" => "03",
                    "clear" => "01",
                    "drizzle" => "09",
                    "rain" => "10",
                    "thunderstorm" => "11",
                    "snow" => "13",
                    "mist" => "50",
                    _ => "03",
                };

                return $"https://openweathermap.org/img/wn/{baseCode}d@2x.png";
            }
            catch
            {
                return "https://openweathermap.org/img/wn/03d@2x.png";
            }
        }
    }
}