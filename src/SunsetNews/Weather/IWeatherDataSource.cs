namespace SunsetNews.Weather;

internal interface IWeatherDataSource
{
    public Task<WeatherData> FetchAsync(string city, int dayOffset = 0);
}
