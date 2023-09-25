using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SunsetNews;

var config = new ConfigurationBuilder().AddJsonFile("config.json").Build();

var services = new ServiceCollection()
	.AddLogging(builder => builder.AddConsole())
	.Configure<DefaultTelegramClient.Options>(config.GetSection("Telegram"))
	.AddSingleton<IUserSequenceProcessor, GugUserSequenceProcessor>()
	.AddSingleton<ITelegramClient, DefaultTelegramClient>()
	.BuildServiceProvider();

var client = services.GetRequiredService<ITelegramClient>();

await client.ConnectAsync();

await client.MainLoop();
