using Microsoft.Extensions.Logging;
using SunsetNews.Localization;
using SunsetNews.Telegram;
using SunsetNews.UserSequences.UserWaitConditions;
using System.Collections.Concurrent;

namespace SunsetNews.UserSequences
{
    internal sealed class YieldUserSequenceProcessor : IUserSequenceProcessor
	{
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
				await state.CurrentSequence.DisposeAsync();
				state.CurrentSequence = null;
			}

			if (_repository.HasSequence(command))
			{
				var sequence = _repository.InitiateSequence(message, command);
				state.CurrentSequence = sequence;
				await ProgressSequence(state);
			}
		}

		public async Task PerformButtonAsync(object stateObject, IMessage message, string id)
		{
			var state = (ChatState)stateObject;

			if (state.CurrentSequence is not null)
				if (state.CurrentSequence.Current.PromoteButton(message, id))
					await ProgressSequence(state);
		}

		public async Task PerformMessageAsync(object stateObject, IMessage message)
		{
			var state = (ChatState)stateObject;

			if (state.CurrentSequence is not null)
				if (state.CurrentSequence.Current.PromoteMessage(message))
					await ProgressSequence(state);
		}

		private async Task ProgressSequence(ChatState state)
		{
			var seq = state.CurrentSequence ?? throw new InvalidOperationException("No sequence to progress");

			_cultureSource.SetupCulture(state.Chat);

			var isFinished = await seq.MoveNextAsync() == false;

			if (isFinished)
			{
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
