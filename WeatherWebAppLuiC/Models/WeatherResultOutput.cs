namespace WeatherWebAppLuiC.Models
// WeatherResultOutput.cs is used to gather fields
// Firstly, two classes are made to handle the output of the Weather Results and Hourly Weather calculations.
// WeatherResultOutput class is declared (Microsoft, 2025b), which allows for fields (Microsoft, 2023) and properties. (Microsoft, 2025e)

// Weather information is stored in certain data types (e.g. strings for city names, double for the numeric data types such as weather, etc.), to allow data to be displayed in the
// WeatherResultsOutput page. The results are gathered from the OpenWeather API calls. (OpenWeather, 2026a)

{
    public class WeatherResultOutput
    {
        public string? City { get; set; }
        public string? WeatherDescription { get; set; }
        public string? CurrentIcon { get; set; }

        // Temperature stored in Celsius (current)
        public double Temps { get; set; }

        // Minimum, maximum and Feels Like temperatures for further temperature information.
        public double? MinTempsCelsius { get; set; }
        public double? MaxTempsCelsius { get; set; }
        public double? TempsFeelsLikeCelsius { get; set; }

        public string? WeatherNow { get; set; }

        // Creation of a List is made here, and can be dynamically resized unlike arrays.
        // (Rovenskii, 2022)
        public List<HourlyWeather> HourlyForecast { get; set; } = new();

        // Convert Celsius to Fahrenheit
        // Display string like as an example: 9°C / 48.2 °F
        // The formula is: 	°F = °C * 9/5 + 32
        // (CalculatorSoup LLC, 2025)
        public static double CelsiusToFahrenheit(double c) => c * 9.0 / 5.0 + 32.0;

        // Fahrenheit rounded to 1 decimal place for display (current)
        public double TempsF => Math.Round(CelsiusToFahrenheit(Temps), 1);
        public string TempsDisplay => $"{Math.Round(Temps, 0)}°C / {TempsF} °F";

        // Computed Fahrenheit and display helpers for nullable values.
        // Ternary operators are used to display values if the value is null, otherwise a value is shown as a percentage.
        // (Microsoft, 2026a)
        public double? MinTempsFahrenheit => MinTempsCelsius.HasValue ? Math.Round(CelsiusToFahrenheit(MinTempsCelsius.Value), 1) : (double?)null;
        public double? MaxTempsFahrenheit => MaxTempsCelsius.HasValue ? Math.Round(CelsiusToFahrenheit(MaxTempsCelsius.Value), 1) : (double?)null;
        public double? TempsFeelsLikeFahrenheit => TempsFeelsLikeCelsius.HasValue ? Math.Round(CelsiusToFahrenheit(TempsFeelsLikeCelsius.Value), 1) : (double?)null;

       // For each instance that $"{}"; appears, the values are being interpolated to allow it to be dispayed on the Weather Results page. (Mellor, 2025)
       // This has been done for the temperature values, precipitation probability, etc. 
        public string MinTempsDisplay => MinTempsCelsius.HasValue && MinTempsFahrenheit.HasValue
            ? $"{Math.Round(MinTempsCelsius.Value, 0)}°C / {MinTempsFahrenheit.Value} °F"
            : "N/A"; // Shows as N/A if unavailable from OpenWeather API data (OpenWeather, 2026a), which has also been done for the Maximum and Feels Like Temperatures.

        public string MaxTempsDisplay => MaxTempsCelsius.HasValue && MaxTempsFahrenheit.HasValue
            ? $"{Math.Round(MaxTempsCelsius.Value, 0)}°C / {MaxTempsFahrenheit.Value} °F"
            : "N/A"; 

        public string TempsFeelsLikeDisplay => TempsFeelsLikeCelsius.HasValue && TempsFeelsLikeFahrenheit.HasValue
            ? $"{Math.Round(TempsFeelsLikeCelsius.Value, 0)}°C / {TempsFeelsLikeFahrenheit.Value} °F"
            : "N/A";
    }

    public class HourlyWeather
    {
        // DateTime struct is placed here to enable the functionality in displaying the hourly weather. (Microsoft, 2026c)
        public DateTime Time { get; set; }

        // Temperature in Celsius
        public double Temperature { get; set; }
        public string? IconName { get; set; }

        public string? WeatherDescription { get; set; }

        public double TemperatureF => Math.Round(WeatherResultOutput.CelsiusToFahrenheit(Temperature), 1);

        public string TemperatureDisplay => $"{Math.Round(Temperature, 0)}°C / {TemperatureF} °F";

        // Probability of precipitation as an integer percenage (0-100)
        public int? PrecipitationProbabilityPercent { get; set; }

        // Display for Precipitaion Probability. If there is a value above 1%, it is displayed. Otherwise, a ternary operator is used to display a value
        // of "0%". (Microsoft, 2026a)
        public string PrecipitationProbabilityDisplay => PrecipitationProbabilityPercent.HasValue ? $"{PrecipitationProbabilityPercent.Value}%" : "0%";
    }

}