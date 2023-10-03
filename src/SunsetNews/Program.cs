using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SunsetNews;
using SunsetNews.Localization;
using SunsetNews.Scheduling;
using SunsetNews.Scheduling.UserPreferencesBased;
using SunsetNews.Telegram;
using SunsetNews.Telegram.Implementation;
using SunsetNews.UserPreferences;
using SunsetNews.UserPreferences.FileBased;
using SunsetNews.UserSequences;
using SunsetNews.UserSequences.ReflectionRepository;
using SunsetNews.Weather;

var config = new ConfigurationBuilder().AddJsonFile("config.json").Build();

var services = new ServiceCollection()
	.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace))
	.AddLocalization(options => options.ResourcesPath = "Translations")

	.Configure<DefaultTelegramClient.Options>(config.GetSection("Telegram"))
	.AddSingleton<ITelegramClient, DefaultTelegramClient>()

	.AddSingleton<IUserSequenceProcessor, YieldUserSequenceProcessor>()

	.AddSingleton<IUserSequenceRepository, ReflectionUserSequenceRepository>()

	.Configure<AccuWeatherDataSource.Options>(config.GetSection("Weather:AccuWeather"))
	.AddSingleton<IWeatherDataSource, AccuWeatherDataSource>()

	.Configure<FileBasedUserPreferenceRepository.Options>(config.GetSection("UserPreferences:FileBased"))
	.AddSingleton<IUserPreferenceRepository, FileBasedUserPreferenceRepository>()
	.AddTransient(typeof(IUserPreference<>), typeof(DIUserPreference<>))

	.AddTransient<ICultureSource, PreferencesBasedCultureSource>()

	.AddSingleton<IScheduler, UserPreferencesBasedScheduler>()

	.AddTransient<ISequenceModule, WeatherSequenceModule>()
	.AddTransient<ISchedulerModule, WeatherSequenceModule>()

	.BuildServiceProvider();

var userPreferenceRepository = services.GetRequiredService<IUserPreferenceRepository>();
await userPreferenceRepository.PreloadAllAsync();

var scheduler = services.GetRequiredService<IScheduler>();
scheduler.Initialize(services.GetServices<ISchedulerModule>());

var client = services.GetRequiredService<ITelegramClient>();

client.UseSequenceProcessor(services.GetRequiredService<IUserSequenceProcessor>());

await client.ConnectAsync();

await client.MainLoop();
