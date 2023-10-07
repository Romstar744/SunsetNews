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
	private readonly ITelegramClient _bot;
	private readonly IWeatherDataSource _source;
	private readonly IStringLocalizer<WeatherSequenceModule> _localizer;
	private readonly IUserPreferenceRepository _userPreferences;
	private readonly IUserPreference<TimeZonePreferences> _timeZonePreferences;
	private readonly IUserPreference<NotificationPreferences> _notificationPreferences;


	public WeatherSequenceModule(IScheduler scheduler,
		ITelegramClient bot,
		IWeatherDataSource source,
		IStringLocalizer<WeatherSequenceModule> localizer,
		IUserPreferenceRepository userPreferences)
	{
		_scheduler = scheduler;
		_bot = bot;
		_source = source;
		_localizer = localizer;

		_userPreferences = userPreferences;
		_timeZonePreferences = userPreferences.LoadPreference<TimeZonePreferences>();
		_notificationPreferences = userPreferences.LoadPreference<NotificationPreferences>();
	}


	public string ModuleId => nameof(WeatherSequenceModule);


	[UserSequence("notifications")]
	public async IAsyncEnumerator<UserWaitCondition> SetupNotifications(IMessage awakeMessage)
	{
		var taskId = _scheduler.Plan(awakeMessage.Chat.Id, new SchedulerTask(this, "printNotification", null), new TimeOnly(23, 06), SchedulerDayOfWeek.AllDays);
		_notificationPreferences.Modify(awakeMessage.Chat.Id, _ => new NotificationPreferences(taskId, "Moscow"));
		await awakeMessage.Chat.SendMessageAsync(new MessageSendModel("OK!"));
		yield break;
	}

	[UserSequence("weather")]
	public async IAsyncEnumerator<UserWaitCondition> RequestWeather(IMessage awakeMessage)
	{
		var timeZone = _timeZonePreferences.Get(awakeMessage.Chat.Id);
		var utcNow = DateTime.UtcNow;

		await awakeMessage.Chat.SendMessageAsync(new MessageSendModel(_localizer["CityPrompt"]));

		var cityMessage = new TextMessageWaitCondition();
		yield return cityMessage;

		var city = cityMessage.CapturedMessage.Content;

		var dayPrompt = await awakeMessage.Chat.SendMessageAsync(new MessageSendModel(_localizer["DayPrompt"], new(
			new MessageButton("today", _localizer["Today+0", getDay(0)]),
			new MessageButton("yesterday", _localizer["Today+1", getDay(1)]),
			new MessageButton("5days", _localizer["FiveDays"])
		
		)));

		var dayPromptResponse = new ButtonWaitCondition(dayPrompt);
		yield return dayPromptResponse;

		try
		{
			switch (dayPromptResponse.CapturedButtonId)
			{
				case "today":
					var todayForecast = await _source.FetchAsync(city, 0);
					await PrintForecastAsync(awakeMessage.Chat, todayForecast, _localizer["Today+0", getDay(0)]);
					break;

				case "yesterday":
					var yesterdayForecast = await _source.FetchAsync(city, 1);
					await PrintForecastAsync(awakeMessage.Chat, yesterdayForecast, _localizer["Today+1", getDay(1)]);
					break;

				case "5days":
					var forecasts = await Task.WhenAll(Enumerable.Range(0, 5).Select(offset => _source.FetchAsync(city, offset)));
					var offset = 0;
					foreach (var forecast in forecasts)
					{
						await PrintForecastAsync(awakeMessage.Chat, forecast, _localizer["Today+" + offset, getDay(offset)]);
						offset++;
					}
					break;
			}
		}
		catch (Exception)
		{
			await awakeMessage.Chat.SendMessageAsync(new MessageSendModel(_localizer["ErrorDuringProcess"]));
			throw;
		}



		string getDay(int offset)
		{
			var thatDay = utcNow.AddDays(offset);
			var transformedThatDay = thatDay + timeZone.GetTimeZone().GetUtcOffset(thatDay);
			var dayOfWeek = transformedThatDay.DayOfWeek;
			var dayOfWeekString = _localizer["DayOfWeek_" + dayOfWeek];

			return $"{dayOfWeekString} {transformedThatDay.ToShortDateString()}";
		}
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

	private async Task PrintForecastAsync(IUserChat chat, WeatherData forecast, string title)
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

	public async Task ExecuteFunctionAsync(string functionName, object? parameter, UserZoneId user)
	{
		switch (functionName)
		{
			case "printNotification":
				var tgUser = await _bot.GetUserChatAsync(user.Id);
				await PrintNotificationAsync(tgUser);
				break;

			default:
				throw new ArgumentException("No function with given name", nameof(functionName));
		}
	}

	private async Task PrintNotificationAsync(IUserChat user)
	{
		var preference = _notificationPreferences.Get(user.Id);
		if (preference.IsActive() == false)
			return;

		var forecast = await _source.FetchAsync(preference.TargetCity);
		await PrintForecastAsync(user, forecast, _localizer["NotificationTitle"]);
	}
}
