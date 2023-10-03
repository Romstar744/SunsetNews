using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using SunsetNews.Localization;
using SunsetNews.Scheduling;
using SunsetNews.Telegram;
using SunsetNews.UserPreferences;
using SunsetNews.UserSequences.ReflectionRepository;
using SunsetNews.UserSequences.UserWaitConditions;
using SunsetNews.Weather;
using System.Globalization;

namespace SunsetNews;

internal sealed class WeatherSequenceModule : ISequenceModule, ISchedulerModule
{
	private readonly IScheduler _scheduler;
	private readonly IWeatherDataSource _source;
	private readonly IStringLocalizer<WeatherSequenceModule> _localizer;
	private readonly IUserPreferenceRepository _userPreferences;


	public WeatherSequenceModule(IScheduler scheduler, IWeatherDataSource source, IStringLocalizer<WeatherSequenceModule> localizer, IUserPreferenceRepository userPreferences)
	{
		_scheduler = scheduler;
		_source = source;
		_localizer = localizer;
		_userPreferences = userPreferences;
	}


	public string ModuleId => nameof(WeatherSequenceModule);


	[UserSequence("schedule")]
	public async IAsyncEnumerator<UserWaitCondition> ScheduleTask(IMessage awakeMessage)
	{
		_scheduler.Plan(awakeMessage.Chat.Id, new SchedulerTask(this, "demo", 55), new TimeOnly(19, 25, 30, 0, 0), SchedulerDayOfWeek.AllDays);
		yield break;
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
		preference.Modify(awakeMessage.Chat.Id, value => value with { Culture = new(code) });
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

	public void ExecuteFunction(string functionName, object? parameter)
	{
		switch (functionName)
		{
			case "demo":
				Console.WriteLine(JsonConvert.SerializeObject(parameter));
				break;

			default:
				throw new ArgumentException("No function with given name", nameof(functionName));
		}
	}
}
