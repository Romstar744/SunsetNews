using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SunsetNews.UserSequences;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SunsetNews.Telegram.Implementation;

internal sealed class DefaultTelegramClient : ITelegramClient, IUpdateHandler
{
	public static readonly EventId ClientConnectedLOG = new(11, "ClientConnected");
	public static readonly EventId PollingErrorLOG = new(21, "PollingError");


	private readonly Options _options;
	private readonly TelegramBotClient _bot;
	private readonly ILogger<DefaultTelegramClient> _logger;
	private IUserSequenceProcessor? _processor;


	public DefaultTelegramClient(IOptions<Options> options, ILogger<DefaultTelegramClient> logger)
	{
		_options = options.Value;
		_bot = new TelegramBotClient(_options.Token);
		_logger = logger;
	}


	public Task ConnectAsync()
	{
		_bot.StartReceiving(this);
		_logger.Log(LogLevel.Information, ClientConnectedLOG, "Telegram client connected with id {BotId}", _bot.BotId);
		return Task.CompletedTask;
	}

	public Task MainLoop()
	{
		return Task.Delay(-1);
	}

	public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
	{
		_logger.Log(LogLevel.Error, PollingErrorLOG, exception, "Exception while polling");
		return Task.CompletedTask;
	}

	public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
	{
		if (_processor is null)
			return;

		if (update.Type == UpdateType.Message && update is { Message.Text: not null })
		{
			var tgMessage = update.Message;
			var chat = new UserChatProxy(_bot, tgMessage.Chat);
			var message = new MessageProxy(_bot, chat, tgMessage);


			var processorState = _processor.GetStateForChat(chat);

			if (tgMessage.Text.StartsWith(_options.CommandPrefix) == false)
			{
				await _processor.PerformMessageAsync(processorState, message);
			}
			else
			{
				var command = tgMessage.Text[_options.CommandPrefix.Length..];

				if (command == _options.StartCommand)
					return;

				await _processor.PerformCommandAsync(processorState, message, command);
			}
		}

		if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery is not null)
		{
			var data = update.CallbackQuery.Data;
			var tgMessage = update.CallbackQuery.Message;

			if (data is null || tgMessage is null)
				return;

			var chat = new UserChatProxy(_bot, tgMessage.Chat);
			var message = new MessageProxy(_bot, chat, tgMessage);

			var processorState = _processor.GetStateForChat(chat);

			await _processor.PerformButtonAsync(processorState, message, data);
		}
	}

	public void UseSequenceProcessor(IUserSequenceProcessor processor)
	{
		if (_processor is not null)
			throw new InvalidOperationException("Sequence processor has already used");

		_processor = processor;
	}

	public async Task<IUserChat> GetUserChatAsync(long id)
	{
		return new UserChatProxy(_bot, await _bot.GetChatAsync(id));
	}


	public class Options
	{
		public required string Token { get; init; }

		public required string StartCommand { get; init; }

		public string CommandPrefix { get; init; } = "/";
	}
}
