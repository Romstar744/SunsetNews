using SunsetNews.Utils;
using System.Numerics;

namespace SunsetNews.Weather;

internal sealed class WeatherData
{
	public WeatherData(string city, DateOnly day)
	{
		City = city;
		Day = day;
	}


	public string City { get; }

	public DateOnly Day { get; }

	public string? GovernmentalMessage { get; init; }

	public required MoonPhaseType MoonPhase { get; init; }

	public required TimeRange SunPeriod { get; init; }

	public required FloatRange Temperature { get; init; }

	public required FloatRange RealFeelTemperature { get; init; }

	public required Vector2 Wind { get; init; }

	public required CloudinessType Cloudiness { get; init; }

	public required ThunderstormStatus Thunderstorm { get; init; }

	public required PrecipitationType Precipitation { get; init; }

	public required float PrecipitationAmount { get; init; }


	public enum CloudinessType
	{
		Clear = 0,
		PartlyCloudy = 1,
		ModerateCloudy = 2,
		MainlyCloudy = 3,
		VariableCloudy = 4
	}

	public enum MoonPhaseType
	{
		New = 0,
		WaxingCrescent = 1,
		FirstQuarter = 2,
		WaxingGibbous = 3,
		Full = 4,
		WaningGibbous = 5,
		LastQuarter = 6,
		WaningCrescent = 7
	}

	public enum PrecipitationType
	{
		None,
		Rain,
		Snow,
		Mixed,
		Other
	}

	public enum ThunderstormStatus
	{
		None = 0,
		Maybe = 1,
		Probably = 2,
		Guaranteed = 3
	}
}
