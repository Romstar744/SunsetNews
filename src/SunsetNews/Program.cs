using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SunsetNews;
using SunsetNews.Telegram;
using SunsetNews.Telegram.Implementation;
using SunsetNews.UserSequences;
using SunsetNews.UserSequences.ReflectionRepository;
using SunsetNews.Weather;

var config = new ConfigurationBuilder().AddJsonFile("config.json").Build();

var services = new ServiceCollection()
	.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace))

	.Configure<DefaultTelegramClient.Options>(config.GetSection("Telegram"))
	.AddSingleton<ITelegramClient, DefaultTelegramClient>()

	.AddSingleton<IUserSequenceProcessor, YieldUserSequenceProcessor>()
	.AddSingleton<IUserSequenceRepository, ReflectionUserSequenceRepository>()

	.AddTransient<ISequenceModule, WeatherSequenceModule>()

	.Configure<AccuWeatherDataSource.Options>(config.GetSection("Weather:AccuWeather"))
	.AddTransient<IWeatherDataSource, AccuWeatherDataSource>()
	.BuildServiceProvider();


var client = services.GetRequiredService<ITelegramClient>();

client.UseSequenceProcessor(services.GetRequiredService<IUserSequenceProcessor>());

await client.ConnectAsync();

await client.MainLoop();
