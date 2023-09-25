using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace SunsetNews;

internal sealed class DefaultTelegramClient : ITelegramClient, IUpdateHandler
{
	private readonly Options _options;
	private readonly TelegramBotClient _bot;
	private readonly ILogger<DefaultTelegramClient> _logger;
	private readonly IUserSequenceProcessor _processor;


	public DefaultTelegramClient(IOptions<Options> options, ILogger<DefaultTelegramClient> logger, IUserSequenceProcessor processor)
	{
		_options = options.Value;
		_bot = new TelegramBotClient(_options.Token);
		_logger = logger;
		_processor = processor;
	}


	public Task ConnectAsync()
	{
		_bot.StartReceiving(this);
		return Task.CompletedTask;
	}

	public Task MainLoop()
	{
		return Task.Delay(-1);
	}

	public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
	{
		_logger.Log(LogLevel.Error, exception, "Exception while polling");
		return Task.CompletedTask;
	}

	public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
	{
		if (update is { Message.Text: not null })
		{
			var message = update.Message;

			var processorState = _processor.GetStateForUser(message.Chat.Id);

			if (message.Text.StartsWith(_options.CommandPrefix) == false)
			{
				await _processor.PerformMessageAsync(processorState, message.Text);
			}
			else
			{
				var command = message.Text[_options.CommandPrefix.Length..];

				if (command == _options.StartCommand)
					return;

				await _processor.PerformCommandAsync(processorState, command);
			}
		}
	}


	public class Options
	{
		public required string Token { get; init; }

		public required string StartCommand { get; init; }

		public string CommandPrefix { get; init; } = "/";
	}
}
