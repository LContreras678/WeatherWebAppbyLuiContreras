// Used to gather the local time zones when gathering location and weather data

using System.Text.Json.Serialization;

namespace WeatherWebAppLuiC.Models
{
    public class GeoLocation
    // Geolocation API data is called using JSON format. OpenWeather API supports geolocation for placing the weather information. 
    // (OpenWeather, 2026b)
    // The OpenWeather API supports JSON, XML and HTML to process weather data around the world. (OpenWeather, 2026a)
    {
        // For gathering the city name, longtitude, latitude and country of the location.
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("lat")] public double Lat { get; set; }
        [JsonPropertyName("lon")] public double Lon { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
    }

    // Class for gathering the current weather, hourly weather and when searching for the city, it is based on the local time zone of the location selected.
    public class OneCallResponse
    {
        [JsonPropertyName("current")] public Current? Current { get; set; }
        [JsonPropertyName("hourly")] public List<Hourly>? Hourly { get; set; }
        [JsonPropertyName("timezone_offset")] public int TimezoneOffset { get; set; }
    }

    // Similar to WeatherResultOutput.cs, this is required to gather API data for the:
    /*
    1. Date and time
    2. Temperature
    3. "Feels Like" temperature
    4. Current weather condition

    */

    public class Current
    {
        [JsonPropertyName("dt")] public long Dt { get; set; }
        [JsonPropertyName("temp")] public double Temp { get; set; }
        [JsonPropertyName("feels_like")] public double FeelsLike { get; set; }
        [JsonPropertyName("weather")] public List<WeatherInfo>? Weather { get; set; }
    }

    /*
    Similar to the Current class above:
    1. Gathers the Date/Time information
    2. Current Temperature
    3. Hourly Precipitation Information (which appears as Precipitation Probability)
    4. Current Weather Information
    
    */

    public class Hourly
    {
        [JsonPropertyName("dt")] public long Dt { get; set; }
        [JsonPropertyName("temp")] public double Temp { get; set; }
        [JsonPropertyName("pop")] public double? Pop { get; set; }
        [JsonPropertyName("weather")] public List<WeatherInfo>? Weather { get; set; }
    }

    // Also similar to the above classes, but gather further information from the OpenWeather API (OpenWeather, 2026a)
    // These include: description of weather in string format, weather icons, etc.

    public class WeatherInfo
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("main")] public string? Main { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("icon")] public string? Icon { get; set; }
    }
}
