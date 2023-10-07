using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using SunsetNews.Utils;
using System.Collections.Concurrent;
using System.Net;
using System.Numerics;

namespace SunsetNews.Weather;

internal sealed class AccuWeatherDataSource : IWeatherDataSource
{
	public static readonly EventId WeatherRequestedLOG = new(11, "WeatherRequested");
	public static readonly EventId CityCacheMissLOG = new(12, "CityCacheMiss");
	public static readonly EventId ForecastCacheMissLOG = new(13, "ForecastCacheMiss");
	public static readonly EventId CityLocalizationErrorLOG = new(21, "CityLocalizationError");
	public static readonly EventId ForecastRequestErrorLOG = new(22, "ForecastRequestError");
	public static readonly EventId ForecastParseErrorLOG = new(23, "ForecastParseError");
	public static readonly EventId UnspecifiedErrorLOG = new(50, "UnspecifiedError");


	private readonly Options _options;
	private readonly ConcurrentDictionary<string, int> _cityCache = new();
	private readonly ConcurrentDictionary<CacheKey, WeatherData> _forecastCache = new();
	private readonly ILogger<AccuWeatherDataSource> _logger;


	public AccuWeatherDataSource(IOptions<Options> options, ILogger<AccuWeatherDataSource> logger)
	{
		_options = options.Value;
		_logger = logger;
	}


	public async Task<WeatherData> FetchAsync(string city, int dayOffset = 0)
	{
		if (dayOffset > 4)
			throw new ArgumentOutOfRangeException(nameof(dayOffset), dayOffset, "Day offset max value is 4");

		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var thatDay = today.AddDays(dayOffset);
		var cacheKey = new CacheKey(thatDay, city);
		try
		{
			_logger.Log(LogLevel.Debug, WeatherRequestedLOG, "Weather requested for {City} on date {ThatDay} ({Offset} days in future)", city, thatDay, dayOffset);

			if (_forecastCache.TryGetValue(cacheKey, out var value))
			{
				return value;
			}
			else
			{
				_logger.Log(LogLevel.Trace, ForecastCacheMissLOG, "No forecast for {City} on date {ThatDay} ({Offset} days in future) in cache. Calling server API", city, thatDay, dayOffset);
				var api = new Accuweather.AccuweatherApi(_options.ApiKey);

#nullable disable
				if (_cityCache.TryGetValue(city, out var locationKey) == false)
				{
					_logger.Log(LogLevel.Trace, CityCacheMissLOG, "No locationKey for {City} in cache. Calling server API", city);
					try
					{
						var searchResponse = JObject.Parse(await api.Locations.CitySearch(city));

						var data = searchResponse["Data"].ToObject<string>();

						locationKey = JArray.Parse(data)[0]["Key"].ToObject<int>();

						_cityCache.TryAdd(city, locationKey);
					}
					catch (Exception ex)
					{
						_logger.Log(LogLevel.Error, CityLocalizationErrorLOG, ex, "Enable to get locationKey for {City}", city);
						throw;
					}
				}

				JObject forecastResponse;
				try
				{
					forecastResponse = JObject.Parse(await api.Forecast.FiveDaysOfDailyForecasts(locationKey, details: true, metric: true));
					if (forecastResponse["StatusCode"].ToObject<HttpStatusCode>() != HttpStatusCode.OK)
						throw new WebException($"Non OK status code from server while requesting forecast, Server response: {forecastResponse}");
				}
				catch (Exception ex)
				{
					_logger.Log(LogLevel.Error, ForecastRequestErrorLOG, ex, "Enable to get forecast for {City} on date {ThatDay} ({Offset} days in future) in cache", city, thatDay, dayOffset);
					throw;
				}

				for (int i = 0; i < 5; i++)
				{
					if (i == dayOffset)
						continue;

					try
					{
						ParseForecast(city, i, today, forecastResponse);
					}
					catch (Exception)
					{
						//Ignore exception
					}
				}


				return ParseForecast(city, dayOffset, today, forecastResponse);
			}
#nullable restore
		}
		catch (Exception ex)
		{
			_logger.Log(LogLevel.Error, UnspecifiedErrorLOG, ex, "City: {City}, RequestedDay: {RequestedDay}, Offset: {Offset}", city, thatDay, dayOffset);
			throw;
		}
	}


	private WeatherData ParseForecast(string city, int dayOffset, DateOnly today, JObject forecastResponse)
	{
		var thatDay = today.AddDays(dayOffset);
		var cacheKey = new CacheKey(thatDay, city);

		try
		{
#nullable disable
			var forecast = (JObject)JObject.Parse(forecastResponse["Data"].ToObject<string>())["DailyForecasts"][dayOffset];


			var temperatureJson = forecast["Temperature"];
			var realFeelTemperatureJson = forecast["RealFeelTemperature"];

			var windSpeed = forecast["Day"]["Wind"]["Speed"]["Value"].ToObject<float>();
			var windDirection = forecast["Day"]["Wind"]["Direction"]["Degrees"].ToObject<float>() / 180f * MathF.PI;
			var wind = new Vector2(windSpeed * MathF.Cos(windDirection), windSpeed * MathF.Sin(windDirection));

			var cloudCover = forecast["Day"]["CloudCover"].ToObject<int>();
			var cloudiness = cloudCover == 0 ? WeatherData.CloudinessType.Clear : (WeatherData.CloudinessType)Ceil(cloudCover, 25);


			var rainProbability = forecast["Day"]["RainProbability"]?.ToObject<int?>() ?? 0;
			var snowProbability = forecast["Day"]["SnowProbability"]?.ToObject<int?>() ?? 0;

			var rainValue = forecast["Day"]["Rain"]["Value"]?.ToObject<int?>() ?? 0;
			var snowValue = (forecast["Day"]["Snow"]["Value"]?.ToObject<int?>() ?? 0) * 10; //from cm to mm

			WeatherData.PrecipitationType precipitationType;
			int precipitationAmount;
			if (rainProbability > 15 && snowProbability > 15)
				(precipitationType, precipitationAmount) = (WeatherData.PrecipitationType.Mixed, rainValue + snowValue);
			else if (rainProbability > 15)
				(precipitationType, precipitationAmount) = (WeatherData.PrecipitationType.Rain, rainValue);
			else if (snowProbability > 15)
				(precipitationType, precipitationAmount) = (WeatherData.PrecipitationType.Snow, snowValue);
			else
				(precipitationType, precipitationAmount) = (WeatherData.PrecipitationType.None, 0);


			var temperature = new FloatRange(temperatureJson["Minimum"]["Value"].ToObject<float>(), temperatureJson["Maximum"]["Value"].ToObject<float>());
			var realFeelTemperature = new FloatRange(realFeelTemperatureJson["Minimum"]["Value"].ToObject<float>(), realFeelTemperatureJson["Maximum"]["Value"].ToObject<float>());
			var moonPhase = Enum.Parse<WeatherData.MoonPhaseType>(forecast["Moon"]["Phase"].ToObject<string>());
			var sunPeriod = new TimeRange(TimeOnly.FromDateTime(forecast["Sun"]["Rise"].ToObject<DateTime>()), TimeOnly.FromDateTime(forecast["Sun"]["Set"].ToObject<DateTime>()));
			var thunderstorm = (WeatherData.ThunderstormStatus)((forecast["Day"]["ThunderstormProbability"]?.ToObject<int?>() ?? 0) / 26);

			var weatherData = new WeatherData(city, thatDay)
			{
				Temperature = temperature,
				RealFeelTemperature = realFeelTemperature,
				MoonPhase = moonPhase,
				SunPeriod = sunPeriod,
				Wind = wind,
				Cloudiness = cloudiness,
				Thunderstorm = thunderstorm,
				Precipitation = precipitationType,
				PrecipitationAmount = precipitationAmount
			};

			_forecastCache.TryAdd(cacheKey, weatherData);

			return weatherData;
#nullable restore
		}
		catch (Exception ex)
		{
			_logger.Log(LogLevel.Error, ForecastParseErrorLOG, ex, "Enable to parse forecast for {City} on date {ThatDay} ({Offset} days in future) in cache", city, thatDay, dayOffset);
			throw;
		}
	}

	private static int Ceil(int a, int div)
	{
		if (a % div == 0)
			return a / div;
		else
			return (a / div) + 1;
	}


	public class Options
	{
		public required string ApiKey { get; init; }
	}

	private record struct CacheKey(DateOnly Date, string City);
}
