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
using Telegram.Bot.Types;

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
	private readonly IUserPreference<CulturePreferences> _culturePreferences;


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
		_culturePreferences = userPreferences.LoadPreference<CulturePreferences>();
	}


	public string ModuleId => nameof(WeatherSequenceModule);


	[UserSequence("settings")]
	public async IAsyncEnumerator<UserWaitCondition> SettingBot(IMessage awakeMessage)
	{
	loop:
		var menuMessage = await awakeMessage.Chat.SendMessageAsync(new MessageSendModel(_localizer["SettingsMenuContent"], new(
			new MessageButton("lang", _localizer["LanguageSettingsButton"]),
			new MessageButton("time", _localizer["TimeZoneSettingsButton"]),
			new MessageButton("exit", _localizer["ExitSettingsButton"])
		)));

		var clickedButton = new ButtonWaitCondition(menuMessage);
		yield return clickedButton;

		await menuMessage.DeleteAsync();

		switch (clickedButton.CapturedButtonId)
		{
			case "lang":
				await awakeMessage.Chat.SendMessageAsync(new MessageSendModel(_localizer["LangAskSettingsMessage"]));
				var langAnswer = new TextMessageWaitCondition();
				yield return langAnswer;

				try
				{
					var cultureInfo = CultureInfo.GetCultureInfo(langAnswer.CapturedMessage.Content);
					_culturePreferences.Modify(awakeMessage.Chat.Id, value => value with { Culture = cultureInfo });

				}
				catch (CultureNotFoundException)
				{
					await awakeMessage.Chat.SendMessageAsync(new MessageSendModel(_localizer["InvalidCultureCodeMessage"]));
				}

				goto loop;

			case "time":
				var availableTimeZones = TimeZoneInfo.GetSystemTimeZones();

				await awakeMessage.Chat.SendMessageAsync(new MessageSendModel(_localizer["TimeZoneAskSettingsMessage"]));
				var contentStrings = availableTimeZones.Select((timeZone, i) => $"[{i + 1}]: (UTC{(timeZone.BaseUtcOffset.Ticks < 0 ? "-" : "+")}{timeZone.BaseUtcOffset:hh\\:ss}) {timeZone.Id}");
				while (contentStrings.Any())
				{
					var toPrint = contentStrings.Take(50);
					await awakeMessage.Chat.SendMessageAsync(new MessageSendModel(string.Join("\n", toPrint)));
					contentStrings = contentStrings.Skip(50);
				}

				var timeZoneAnswer = new TextMessageWaitCondition(s => int.TryParse(s.Content, out var number) && number > 0 && number <= availableTimeZones.Count);
				yield return timeZoneAnswer;

				var selectedTimeZone = availableTimeZones[int.Parse(timeZoneAnswer.CapturedMessage.Content) - 1];

				_timeZonePreferences.Modify(awakeMessage.Chat.Id, value => TimeZonePreferences.FromTimeZone(selectedTimeZone));

				goto loop;

			default:
				break;
		}
	}

	[UserSequence("notifications")]
	public async IAsyncEnumerator<UserWaitCondition> SetupNotifications(IMessage awakeMessage)
	{
	loop:
		var currentConfig = _notificationPreferences.Get(awakeMessage.Chat.Id);

		var menuMessage = await awakeMessage.Chat.SendMessageAsync(new MessageSendModel(_localizer["NotificationsMenuContent"], new(
			new MessageButton("toggle", currentConfig.IsActive() ? _localizer["DisableNotificationsButton"] : _localizer["EnableNotificationsButton"]),
			new MessageButton("city", _localizer["CityNotificationsButton"]),
			new MessageButton("day", _localizer["DaysOfWeekNotificationsButton"]),
			new MessageButton("time", _localizer["TimeNotificationsButton"]),
			new MessageButton("exit", _localizer["ExitNotificationsButton"])
		)));

		var clickedButton = new ButtonWaitCondition(menuMessage);
		yield return clickedButton;

		await menuMessage.DeleteAsync();

		switch (clickedButton.CapturedButtonId)
		{
			case "toggle":
				if (currentConfig.IsActive())
				{
					_scheduler.Cancel(awakeMessage.Chat.Id, currentConfig.SchedulerTaskId);
					_notificationPreferences.Modify(awakeMessage.Chat.Id, s => s with { SchedulerTaskId = Guid.Empty });
				}
				else
				{
					var timeZone = _timeZonePreferences.Get(awakeMessage.Chat.Id).GetTimeZone();
					var taskId = _scheduler.Plan(awakeMessage.Chat.Id, new SchedulerTask(this, "printNotification", null), currentConfig.LocalTime, currentConfig.WeekOfDays, timeZone);
					_notificationPreferences.Modify(awakeMessage.Chat.Id, s => s with { SchedulerTaskId = taskId, TimeZoneSerializedForm = timeZone.ToSerializedString() });
				}

				goto loop;

			case "city":
				await awakeMessage.Chat.SendMessageAsync(new MessageSendModel(_localizer["CityAskNotificationsMessage"]));
				var cityAnswer = new TextMessageWaitCondition();
				yield return cityAnswer;

				_notificationPreferences.Modify(awakeMessage.Chat.Id, s => s with { TargetCity = cityAnswer.CapturedMessage.Content });

				recreateTask();
				goto loop;

			case "time":
				await awakeMessage.Chat.SendMessageAsync(new MessageSendModel(_localizer["TimeAskNotificationsMessage"]));
				var timeAnswer = new TextMessageWaitCondition(s => TimeOnly.TryParse(s.Content, out _));
				yield return timeAnswer;

				_notificationPreferences.Modify(awakeMessage.Chat.Id, s => s with { LocalTime = TimeOnly.Parse(timeAnswer.CapturedMessage.Content) });

				recreateTask();
				goto loop;

			case "day":
				await awakeMessage.Chat.SendMessageAsync(new MessageSendModel(_localizer["DaysOfWeekAskNotificationsMessage"]));
				var daysAnswer = new TextMessageWaitCondition(s => s.Content.Length == 7);
				yield return daysAnswer;

				SchedulerDayOfWeek result = 0;
				for (int dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++)
					if (daysAnswer.CapturedMessage.Content[dayOfWeek] == '+')
						result |= (SchedulerDayOfWeek)(1 << dayOfWeek);

				_notificationPreferences.Modify(awakeMessage.Chat.Id, s => s with { WeekOfDays = result });

				recreateTask();
				goto loop;

			default:
				break;
		}


		void recreateTask()
		{
			_scheduler.Cancel(awakeMessage.Chat.Id, currentConfig.SchedulerTaskId);
			var timeZone = _timeZonePreferences.Get(awakeMessage.Chat.Id).GetTimeZone();
			var taskId = _scheduler.Plan(awakeMessage.Chat.Id, new SchedulerTask(this, "printNotification", null), currentConfig.LocalTime, currentConfig.WeekOfDays, timeZone);
			_notificationPreferences.Modify(awakeMessage.Chat.Id, s => s with { SchedulerTaskId = taskId, TimeZoneSerializedForm = timeZone.ToSerializedString() });
		}
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
