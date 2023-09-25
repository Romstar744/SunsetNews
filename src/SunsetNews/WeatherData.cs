namespace SunsetNews;

internal sealed class WeatherData
{
	public WeatherData(string city)
	{
		City = city;
	}


	public string City { get; }
}
