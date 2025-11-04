using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace NoFences.Util
{
    /// <summary>
    /// Weather data from OpenWeatherMap API
    /// </summary>
    public class WeatherData
    {
        public double Temperature { get; set; } // In Celsius
        public double FeelsLike { get; set; } // Feels like temperature in Celsius
        public string Description { get; set; } // "Clear", "Clouds", "Rain", etc.
        public string IconCode { get; set; } // OpenWeatherMap icon code
        public double WinsdSpeed { get; set; } // In m/s
        public int WindDegree { get; set; } // In degrees
        public string WindDirection
        {
            get
            {
                if (WindDegree >= 337 || WindDegree < 22) return "N";
                if (WindDegree >= 22 && WindDegree < 67) return "NE";
                if (WindDegree >= 67 && WindDegree < 112) return "E";
                if (WindDegree >= 112 && WindDegree < 157) return "SE";
                if (WindDegree >= 157 && WindDegree < 202) return "S";
                if (WindDegree >= 202 && WindDegree < 247) return "SW";
                if (WindDegree >= 247 && WindDegree < 292) return "W";
                if (WindDegree >= 292 && WindDegree < 337) return "NW";
                return "";
            }
        }
        public DateTime Sunrise { get; set; } // Sunrise time (local)
        public DateTime Sunset { get; set; } // Sunset time (local)
        public int Clouds { get; set; } // Cloud coverage percentage (0-100)
        public int Humidity { get; set; } // Humidity percentage (0-100)
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Service for fetching weather data from OpenWeatherMap API.
    /// Free tier: 1000 calls/day, so we cache results for 15 minutes.
    ///
    /// API Key: Users can get their own free key at https://openweathermap.org/api
    /// </summary>
    public class WeatherService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(WeatherService));

        private static readonly HttpClient httpClient = new HttpClient();
        private static WeatherData cachedWeather = null;
        private static string cachedLocation = null;
        private static DateTime lastFetchTime = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

        // Cache for weather icons
        private static readonly Dictionary<string, BitmapImage> iconCache = new Dictionary<string, BitmapImage>();

        // Default API key for testing (limited, users should get their own)
        // This is a public demo key - users should replace it with their own
        private const string DefaultApiKey = "YOUR_API_KEY_HERE";

        /// <summary>
        /// Gets weather data for the specified location.
        /// Results are cached for 15 minutes to avoid excessive API calls.
        /// </summary>
        /// <param name="location">City name (e.g., "London", "New York", "Tokyo")</param>
        /// <param name="apiKey">OpenWeatherMap API key (optional, uses default if not provided)</param>
        /// <returns>Weather data or null if fetch fails</returns>
        public static async Task<WeatherData> GetWeatherAsync(string location, string apiKey = null)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                log.Info("Location is empty");
                return null;
            }

            // Check cache
            if (cachedWeather != null &&
                cachedLocation == location &&
                DateTime.Now - lastFetchTime < CacheDuration)
            {
                log.Info($"Returning cached weather for {location}");
                return cachedWeather;
            }

            try
            {
                string apiKeyToUse = string.IsNullOrWhiteSpace(apiKey) ? DefaultApiKey : apiKey;

                if (apiKeyToUse == "YOUR_API_KEY_HERE")
                {
                    log.Info("No API key configured. Get one at https://openweathermap.org/api");
                    return null;
                }

                // OpenWeatherMap API call
                string url = $"https://api.openweathermap.org/data/2.5/weather?q={Uri.EscapeDataString(location)}&appid={apiKeyToUse}&units=metric";

                log.Debug($"Fetching weather for {location}");

                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var data = ParseWeatherResponse(json);

                if (data != null)
                {
                    // Cache the result
                    cachedWeather = data;
                    cachedLocation = location;
                    lastFetchTime = DateTime.Now;

                    log.Debug($"Successfully fetched weather for {location}: {data.Temperature}°C, {data.Description}");
                }

                return data;
            }
            catch (HttpRequestException ex)
            {
                log.Error($"HTTP error fetching weather: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                log.Error($"Error fetching weather: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parses OpenWeatherMap JSON response
        /// </summary>
        private static WeatherData ParseWeatherResponse(string json)
        {
            try
            {
                dynamic data = JsonConvert.DeserializeObject(json);

                if (data == null)
                    return null;

                // Extract temperature (Kelvin to Celsius is handled by units=metric in API call)
                double temp = data.main.temp;
                double feelsLike = data.main.feels_like;

                // Extract weather description and icon
                string description = data.weather[0].main; // "Clear", "Clouds", "Rain", etc.
                string iconCode = data.weather[0].icon; // "01d", "02n", etc.

                // Extract sunrise/sunset (Unix timestamp to DateTime)
                long sunriseUnix = data.sys.sunrise;
                long sunsetUnix = data.sys.sunset;
                DateTime sunrise = DateTimeOffset.FromUnixTimeSeconds(sunriseUnix).LocalDateTime;
                DateTime sunset = DateTimeOffset.FromUnixTimeSeconds(sunsetUnix).LocalDateTime;

                // Extract clouds and humidity
                int clouds = data.clouds.all; // Cloud coverage percentage
                int humidity = data.main.humidity; // Humidity percentage

                return new WeatherData
                {
                    Temperature = Math.Round(temp, 1),
                    FeelsLike = Math.Round(feelsLike, 1),
                    Description = description,
                    IconCode = iconCode,
                    LastUpdated = DateTime.Now,
                    WindDegree = data.wind.deg,
                    WinsdSpeed = data.wind.speed,
                    Sunrise = sunrise,
                    Sunset = sunset,
                    Clouds = clouds,
                    Humidity = humidity
                };
            }
            catch (Exception ex)
            {
                log.Error($"Error parsing weather JSON: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets a Unicode weather emoji based on the weather description
        /// </summary>
        public static string GetWeatherEmoji(string description)
        {
            if (string.IsNullOrEmpty(description))
                return "🌡️";

            switch (description.ToLower())
            {
                case "clear":
                    return "☀️";
                case "clouds":
                    return "☁️";
                case "rain":
                case "drizzle":
                    return "🌧️";
                case "thunderstorm":
                    return "⛈️";
                case "snow":
                    return "❄️";
                case "mist":
                case "fog":
                case "haze":
                    return "🌫️";
                default:
                    return "🌡️";
            }
        }

        /// <summary>
        /// Clears the weather cache (useful for testing or manual refresh)
        /// </summary>
        public static void ClearCache()
        {
            cachedWeather = null;
            cachedLocation = null;
            lastFetchTime = DateTime.MinValue;
            log.Info("Cache cleared");
        }

        internal static string GetWindDirectionArrow(string windDirection)
        {
            if (string.IsNullOrEmpty(windDirection))
                return "🧭";
            switch (windDirection)
            {
                case "N":
                    return "⬆️";
                case "NE":
                    return "↗️";
                case "E":
                    return "➡️";
                case "SE":
                    return "↘️";
                case "S":
                    return "⬇️";
                case "SW":
                    return "↙️";
                case "W":
                    return "⬅️";
                case "NW":
                    return "↖️";
                default:
                    return "🧭";
            }
        }

        internal static string GetWindSpeedInKmH(double winsdSpeed)
        {
            return $"{String.Format("{0:0.0#}", winsdSpeed * 3.6)} km/h";
        }

        /// <summary>
        /// Gets a sunrise/sunset emoji based on the time of day
        /// </summary>
        public static string GetSunIcon(bool isSunrise)
        {
            return isSunrise ? "🌅" : "🌇";
        }

        /// <summary>
        /// Gets a cloud emoji
        /// </summary>
        public static string GetCloudIcon()
        {
            return "☁️";
        }

        /// <summary>
        /// Gets a humidity/water droplet emoji
        /// </summary>
        public static string GetHumidityIcon()
        {
            return "💧";
        }

        /// <summary>
        /// Formats a time as HH:mm
        /// </summary>
        public static string FormatTime(DateTime time)
        {
            return time.ToString("HH:mm");
        }

        /// <summary>
        /// Gets temperature icon
        /// </summary>
        public static string GetTemperatureIcon()
        {
            return "🌡️";
        }

        /// <summary>
        /// Downloads and caches a weather icon from OpenWeatherMap.
        /// Icons are PNG images at 2x resolution (100x100px).
        /// </summary>
        /// <param name="iconCode">Icon code from API (e.g., "01d", "10n")</param>
        /// <returns>BitmapImage or null if download fails</returns>
        public static async Task<BitmapImage> GetWeatherIconAsync(string iconCode)
        {
            if (string.IsNullOrEmpty(iconCode))
                return null;

            // Check cache first
            if (iconCache.ContainsKey(iconCode))
            {
                log.Debug($"Returning cached weather icon for {iconCode}");
                return iconCache[iconCode];
            }

            try
            {
                // Download icon from OpenWeatherMap
                string iconUrl = $"https://openweathermap.org/img/wn/{iconCode}@2x.png";
                log.Debug($"Downloading weather icon from {iconUrl}");

                var response = await httpClient.GetAsync(iconUrl);
                response.EnsureSuccessStatusCode();

                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                // Create BitmapImage from bytes
                var bitmap = new BitmapImage();
                using (var stream = new MemoryStream(imageBytes))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze(); // Make it thread-safe
                }

                // Cache the icon
                iconCache[iconCode] = bitmap;
                log.Debug($"Weather icon {iconCode} downloaded and cached successfully");

                return bitmap;
            }
            catch (Exception ex)
            {
                log.Error($"Error downloading weather icon {iconCode}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Clears the icon cache
        /// </summary>
        public static void ClearIconCache()
        {
            iconCache.Clear();
            log.Info("Weather icon cache cleared");
        }
    }
}
