using Microsoft.Extensions.Localization;
using SunsetNews.Localization;
using SunsetNews.Telegram;
using SunsetNews.UserPreferences;
using SunsetNews.UserSequences.ReflectionRepository;
using SunsetNews.UserSequences.UserWaitConditions;
using SunsetNews.Weather;
using System.Globalization;

namespace SunsetNews;

internal sealed class WeatherSequenceModule : ISequenceModule
{
	private readonly IWeatherDataSource _source;
	private readonly IStringLocalizer<WeatherSequenceModule> _localizer;
	private readonly IUserPreferenceRepository _userPreferences;


	public WeatherSequenceModule(IWeatherDataSource source, IStringLocalizer<WeatherSequenceModule> localizer, IUserPreferenceRepository userPreferences)
	{
		_source = source;
		_localizer = localizer;
		_userPreferences = userPreferences;
	}


	[UserSequence("weather")]
	public async IAsyncEnumerator<UserWaitCondition> RequestWeather(IMessage awakeMessage)
	{
		await awakeMessage.Chat.SendMessageAsync(new MessageSendModel(_localizer["CityPrompt"]));

		var cityMessage = new TextMessageWaitCondition();
		yield return cityMessage;

		var city = cityMessage.CapturedMessage.Content;

		var todayForecast = await _source.FetchAsync(city, dayOffset: 0);
		var tomorrowForecast = await _source.FetchAsync(city, dayOffset: 1);

		await PrintForecast(awakeMessage.Chat, todayForecast, _localizer["Today"]);
		await PrintForecast(awakeMessage.Chat, tomorrowForecast, _localizer["Tomorrow"]);
	}

	[UserSequence("lang")]
	public async IAsyncEnumerator<UserWaitCondition> ChangeLanguage(IMessage awakeMessage)
	{
		await awakeMessage.Chat.SendMessageAsync(new MessageSendModel("Enter culture code"));

		var cultureMessage = new TextMessageWaitCondition();
		yield return cultureMessage;

		var code = cultureMessage.CapturedMessage.Content;

		var preference = _userPreferences.LoadPreference<CulturePreferences>();
		preference.Modify(awakeMessage.Chat, value => value with { Culture = new(code) });
	}

	private async Task PrintForecast(IUserChat chat, WeatherData forecast, string title)
	{
		var content = $"""
		{_localizer["ForecastTitle"]} ({title})
		{_localizer["Temperature", forecast.Temperature]} ({_localizer["RealFeelTemperature", forecast.RealFeelTemperature]})
		{_localizer["WindSpeed", forecast.Wind.Length()]}
		{_localizer["Cloudiness", _localizer["Cloudiness_" + forecast.Cloudiness]]}
		{_localizer["Thunderstorm", _localizer["Thunderstorm_" + forecast.Thunderstorm]]}
		{_localizer["Precipitation", _localizer["Precipitation_" + forecast.Precipitation], forecast.PrecipitationAmount]}
		{_localizer["SunPeriod", forecast.SunPeriod]}
		{_localizer["MoonPhase", _localizer["MoonPhase_" + forecast.MoonPhase]]}
		""";

		await chat.SendMessageAsync(new MessageSendModel(content));
	}
}
