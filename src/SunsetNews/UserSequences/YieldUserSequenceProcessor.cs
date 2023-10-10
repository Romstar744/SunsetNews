using Microsoft.Extensions.Logging;
using SunsetNews.Localization;
using SunsetNews.Telegram;
using SunsetNews.UserSequences.UserWaitConditions;
using System.Collections.Concurrent;

namespace SunsetNews.UserSequences
{
    internal sealed class YieldUserSequenceProcessor : IUserSequenceProcessor
	{
		public static readonly EventId MessagePerformedLOG = new(11, "MessagePerformed");
		public static readonly EventId ButtonPerformedLOG = new(12, "ButtonPerformed");
		public static readonly EventId CommandMatchLOG = new(13, "CommandMatch");
		public static readonly EventId UnknownCommandLOG = new(14, "UnknownCommand");
		public static readonly EventId SequenceProgressedLOG = new(15, "SequenceProgressed");
		public static readonly EventId SequenceFinishedLOG = new(16, "SequenceFinished");
		public static readonly EventId SequenceAbortedLOG = new(17, "SequenceAborted");
		public static readonly EventId SequenceInternalFailLOG = new(21, "SequenceInternalFail");


		private readonly ConcurrentDictionary<long, ChatState> _chatsState = new();
		private readonly ILogger<YieldUserSequenceProcessor> _logger;
		private readonly IUserSequenceRepository _repository;
		private readonly ICultureSource _cultureSource;


		public YieldUserSequenceProcessor(ILogger<YieldUserSequenceProcessor> logger, IUserSequenceRepository repository, ICultureSource cultureSource)
		{
			_logger = logger;
			_repository = repository;
			_cultureSource = cultureSource;
		}


		public object GetStateForChat(IUserChat chat)
		{
			return _chatsState.GetOrAdd(chat.Id, (s) => new ChatState(chat));
		}

		public async Task PerformCommandAsync(object stateObject, IMessage message, string command)
		{
			var state = (ChatState)stateObject;

			if (state.CurrentSequence is not null)
			{
				_logger.Log(LogLevel.Error, SequenceAbortedLOG, "Sequence for {User} aborted by new command from this user", state.Chat);
				await state.CurrentSequence.DisposeAsync();
				state.CurrentSequence = null;
			}

			if (_repository.HasSequence(command))
			{
				_logger.Log(LogLevel.Debug, CommandMatchLOG, "Command /{Command} from {User} has been found and is starting execution", command, message.Chat);

				var sequence = _repository.InitiateSequence(message, command);
				state.CurrentSequence = sequence;
				await ProgressSequence(state);
			}
			else
			{
				_logger.Log(LogLevel.Debug, UnknownCommandLOG, "Unknown command /{Command} from {User}", command, message.Chat);
			}
		}

		public async Task PerformButtonAsync(object stateObject, IMessage message, string id)
		{
			var state = (ChatState)stateObject;

			_logger.Log(LogLevel.Debug, ButtonPerformedLOG, "Button performed. Button with id {ButtonId} attached to {MessageId} and clicked by {User}", id, message.Id, message.Chat);

			if (state.CurrentSequence is not null)
				if (state.CurrentSequence.Current.PromoteButton(message, id))
					await ProgressSequence(state);
		}

		public async Task PerformMessageAsync(object stateObject, IMessage message)
		{
			var state = (ChatState)stateObject;

			_logger.Log(LogLevel.Debug, MessagePerformedLOG, "Message performed. Message from {User} with id {Id} and content: \"{Content}\"", message.Chat, message.Id, message.Content);

			if (state.CurrentSequence is not null)
				if (state.CurrentSequence.Current.PromoteMessage(message))
					await ProgressSequence(state);
		}

		private async Task ProgressSequence(ChatState state)
		{
			var seq = state.CurrentSequence ?? throw new InvalidOperationException("No sequence to progress");

			_cultureSource.SetupCulture(state.Chat);

			try
			{
				_logger.Log(LogLevel.Trace, SequenceProgressedLOG, "Sequence for {User} is progressing", state.Chat);
				var isFinished = await seq.MoveNextAsync() == false;

				if (isFinished)
				{
					_logger.Log(LogLevel.Trace, SequenceFinishedLOG, "Sequence for {User} has been finished", state.Chat);
					await seq.DisposeAsync();
					state.CurrentSequence = null;
				}
			}
			catch (Exception ex)
			{
				_logger.Log(LogLevel.Error, SequenceInternalFailLOG, ex, "Sequence for {User} fails with critical error", state.Chat);
				await seq.DisposeAsync();
				state.CurrentSequence = null;
			}
		}


		private class ChatState
		{
			public ChatState(IUserChat chat)
			{
				Chat = chat;
			}


			public IUserChat Chat { get; }

			public IAsyncEnumerator<UserWaitCondition>? CurrentSequence { get; set; }
		}
	}
}
