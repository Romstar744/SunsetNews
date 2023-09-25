namespace SunsetNews;

internal interface IWeatherDataSource
{
	public Task<WeatherData> FetchAsync(string city);
}
