using SunsetNews.Telegram;
using SunsetNews.UserSequences.ReflectionRepository;
using SunsetNews.UserSequences.UserWaitConditions;
using SunsetNews.Weather;

namespace SunsetNews;

internal sealed class WeatherSequenceModule : ISequenceModule
{
	private readonly IWeatherDataSource _source;


	public WeatherSequenceModule(IWeatherDataSource source)
	{
		_source = source;
	}


	[UserSequence("weather")]
	public async IAsyncEnumerator<UserWaitCondition> RequestWeather(IMessage awakeMessage)
	{
		await awakeMessage.Chat.SendMessageAsync(new MessageSendModel("Enter your city"));

		var cityMessage = new TextMessageWaitCondition();
		yield return cityMessage;

		var city = cityMessage.CapturedMessage.Content;

		var todayForecast = await _source.FetchAsync(city, dayOffset: 0);
		var tomorrowForecast = await _source.FetchAsync(city, dayOffset: 1);

		await PrintForecast(awakeMessage.Chat, todayForecast, "Today");
		await PrintForecast(awakeMessage.Chat, tomorrowForecast, "Tomorrow");
	}

	private static async Task PrintForecast(IUserChat chat, WeatherData forecast, string title)
	{
		var content = $"""
		Weather forecast ({title})
		Temperature: {forecast.Temperature}℃ (real feel {forecast.RealFeelTemperature}℃)
		Wind speed: {forecast.Wind.Length()} km/h
		Cloudiness: {forecast.Cloudiness}
		Thunderstorm: {forecast.Thunderstorm}
		Precipitation: {forecast.Precipitation} (Amount: {forecast.PrecipitationAmount}mm)
		Sun period: {forecast.SunPeriod}
		Moon phase: {forecast.MoonPhase}
		""";

		await chat.SendMessageAsync(new MessageSendModel(content));
	}
}
