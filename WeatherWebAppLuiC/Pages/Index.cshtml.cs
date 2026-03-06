using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WeatherWebAppLuiC.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;


// Index page for the weather web application
// It refers to the same internal C# file (WeatherResultOutput) to gather the city name, longtitude and latitude to find the weather information.
// Throughout the page, some of the data types and attributes have null values (Kanjilal, 2023), which is used to allow the application to function and handle exceptions where there is no data.

namespace WeatherWebAppLuiC.Pages;
/* To enable gathering data from the OpenWeather API (2026a), it is necessary to place to
IHttpClientFactory in the IndexModel class. (Lock, 2020)
IConfiguration is initialised to allow the API key to be read and processed, which is necessary for weather information.
(Microsoft, 2026b)
WeatherResultOutput is referenced, which contains the weather attributes, such as location, city name, etc.
*/
public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    public WeatherResultOutput? Weather { get; set; }

    // A list is also made here to have a list of preloaded cities with their weather conditions.
    // Allows for Razor pages to process data from HTTP requests (Microsoft, 2025d), once again necessary for the OpenWeather API.
    // It also receives the weather attributes to gather the weather for the location.
    public List<WeatherResultOutput> PreloadedWeathers { get; set; } = new();
    [BindProperty]
    public string? City { get; set; }
        [BindProperty]
        public double? Lat { get; set; }
        [BindProperty]
        public double? Lon { get; set; }
        

    public IndexModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }


    // This class is used to initialize the page needed for the Index.

    // The task is done asynchronously to allow loading the necessary information.
    /*
    These include:

    1. Preloading a small set of world cities for the index table
    2. Gathering the weather icon for the small list of cities found at the index page. (OpenWeather, 2026a)
    3. Finds the API key from the configuration file to allow access to OpenWeather data.

    */

    // An asynchronous public task is done to allow for the preloaded list of cities to load the API key and gather weather location data.
    // (Microsoft, 2025a)
    public async Task OnGetAsync()
    {
        // Preloaded cities to prepare the gathering of weather information
        // The API key is also gathered here.
        var cities = new[] { "New York", "London", "Tokyo", "Belfast", "Dublin" };

        var apiKey = _configuration["OpenWeather:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return;

        var client = _httpClientFactory.CreateClient();

        // A foreach loop is used to run through the array of strings and parse the city information. (Refsnes Data, 2026)
        // Using the API key from OpenWeather, its value such as weather condition, the icon for the weather, etc. are gathered.
        // In future iterations, further details may be added to show the information (weather description, temperatures, etc.) can be displayed.

        foreach (var c in cities)
        {
            try
            {
                var url = $"https://api.openweathermap.org/data/3.0/weather?q={Uri.EscapeDataString(c)}&appid={apiKey}&units=metric";
                var resp = await client.GetAsync(url);
                if (!resp.IsSuccessStatusCode) continue;
                var txt = await resp.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(txt);
                var weather = obj["weather"]?.First;
                var iconCode = weather?[("icon")]?.ToString();
                var desc = weather?[("description")]?.ToString();
                double temp = (double?)obj["main"]?["temp"] ?? 0.0;

                // Ternary operator to display the weather icon, using the OpenWeather API. (2026a)

                var iconUrl = !string.IsNullOrWhiteSpace(iconCode)
                    ? $"https://openweathermap.org/img/wn/{iconCode}@2x.png"
                    : "https://openweathermap.org/img/wn/03d@2x.png";

                
// Based on the WeatherResultOutput file, the HourlyWeather data is gathered here so that it can be processed in the Weather Overview/Results page.
                var wr = new WeatherResultOutput
                {
                    City = c,
                    WeatherDescription = desc,
                    Temps = temp,
                    HourlyForecast = new List<HourlyWeather>
                    {
                        new HourlyWeather
                        {
                            Time = DateTime.UtcNow,
                            Temperature = temp,
                            IconName = iconUrl,
                            WeatherDescription = desc
                        }
                    }
                };

                PreloadedWeathers.Add(wr);
            }
            catch
            {
                // For exception handling, if there are no results.
            }
        }
    }


// The public async class is made here to handle the OnPost asynchronous task so that when the user writes a city in the search bar,
// it can handle the form submissions (Anderson, Brock and Larkin, 2025) and for the app to return weather information to the user.

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(City)) return Page();
        var apiKey = _configuration["OpenWeather:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            ModelState.AddModelError(string.Empty, "No OpenWeather API key or invalid API key. For debugging: Set `OpenWeather:ApiKey` in appsettings.json.");
            return Page();
        }
        var client = _httpClientFactory.CreateClient();

        // If lat/lon were provided by the autocomplete selection, skip server geocoding
        if (Lat.HasValue && Lon.HasValue)
        {
            return RedirectToPage("WeatherResult", new { city = City, lat = Lat, lon = Lon });
        }

        // Resolve city to coordinates using geocoding
        var geoUrl = $"https://api.openweathermap.org/geo/1.0/direct?q={Uri.EscapeDataString(City)}&limit=1&appid={apiKey}";
        var geoResp = await client.GetAsync(geoUrl);
        if (!geoResp.IsSuccessStatusCode) return Page();

        var geoJson = await geoResp.Content.ReadAsStringAsync();

        // JArray is placed here, which parses the API information as an array. (Newtonsoft , n.d.)
        JArray? geoArray;
        try
        {
            geoArray = JArray.Parse(geoJson);
        }
        catch
        {
            return Page();
        }

        if (geoArray == null || geoArray.Count == 0) return Page();

        double lat = (double?)geoArray[0]["lat"] ?? 0.0;
        double lon = (double?)geoArray[0]["lon"] ?? 0.0;

        // Using the One Call API (v3) to get current + hourly forecast (OpenWeather, 2026a)
        var url = $"https://api.openweathermap.org/data/3.0/onecall?lat={lat}&lon={lon}&exclude=minutely,daily,alerts&appid={apiKey}&units=metric";
        var response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            JObject dataObj;
            try
            {
                dataObj = JObject.Parse(json);
            }
            catch
            {
                return Page();
            }

            var current = dataObj["current"];
            if (current == null) return Page();

            var weatherDesc = current["weather"]?.First?["description"]?.ToString();
            double temp = (double?)current["temp"] ?? 0.0;

            Weather = new WeatherResultOutput
            {
                City = City,
                WeatherDescription = weatherDesc,
                Temps = temp
            };

            return RedirectToPage("WeatherResult", new { city = City, lat = lat, lon = lon });
        }

        return Page();
    }

// Similar to above with OnGet and OnPost, this is for the Geocoding classes and Weather Information gathering. (Anderson, Brock and Larkin, 2025)

    // Async server-side proxy task for the OpenWeather geocoding API and to keep the API key hidden
    // Stored in a variable so that the API key is acccessible, but hidden. (Anderson and Larkin, 2025)
    public async Task<IActionResult> OnGetGeocode(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return Content("[]", "application/json");

        var apiKey = _configuration["OpenWeather:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return Content("[]", "application/json");

        var url = $"https://api.openweathermap.org/geo/1.0/direct?q={Uri.EscapeDataString(query)}&limit=5&appid={apiKey}";
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(url);
        var json = await response.Content.ReadAsStringAsync();
        return Content(json, "application/json");
    }

    // Server-side proxy to fetch current weather for a given city and return minimal JSON (icon + description), since
    // the API uses JSON to parse the data. (OpenWeather, 2026a)
    public async Task<IActionResult> OnGetWeather(string city)
    {
        if (string.IsNullOrWhiteSpace(city)) return BadRequest(new { error = "city required" });

        var apiKey = _configuration["OpenWeather:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return StatusCode(500, new { error = "OpenWeather API key not configured" });

        var client = _httpClientFactory.CreateClient();
        var url = $"https://api.openweathermap.org/data/2.5/weather?q={Uri.EscapeDataString(city)}&appid={apiKey}&units=metric";

        try
        {
            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                var txt = await resp.Content.ReadAsStringAsync();
                return StatusCode((int)resp.StatusCode, new { error = "upstream", detail = txt });
            }

            var json = await resp.Content.ReadAsStringAsync();
            // Parse minimal fields to return only what the user needs. It will only parse weather, icon and description data only.
            // The information is then converted to string.
            var obj = JObject.Parse(json);
            var weather = obj["weather"]?.First;
            var icon = weather?["icon"]?.ToString();
            var description = weather?["description"]?.ToString();

            return new JsonResult(new { icon, description });
        }
        // For exception handling, if there is no information available.
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "exception", detail = ex.Message });
        }
    }

}
