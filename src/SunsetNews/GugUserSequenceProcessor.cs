using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Telegram.Bot.Types;

namespace SunsetNews;

internal sealed class GugUserSequenceProcessor : IUserSequenceProcessor
{
	private readonly ConcurrentDictionary<long, UserState> _userState = new();
	private readonly ILogger<GugUserSequenceProcessor> _logger;


	public GugUserSequenceProcessor(ILogger<GugUserSequenceProcessor> logger)
	{
		_logger = logger;
	}


	public object GetStateForUser(long chatId)
	{
		return _userState.GetOrAdd(chatId, (id) => new UserState(id));
	}

	public Task PerformButtonAsync(object stateObject, string message)
	{
		throw new NotImplementedException();
	}

	public Task PerformCommandAsync(object stateObject, string command)
	{
		var state = (UserState)stateObject;
		_logger.Log(LogLevel.Information, "PerformCommandAsync {User}: {Message}", state.Id, command);
		return Task.CompletedTask;
	}

	public Task PerformMessageAsync(object stateObject, string message)
	{
		var state = (UserState)stateObject;
		_logger.Log(LogLevel.Information, "PerformMessageAsync {User}: {Message}", state.Id, message);
		return Task.CompletedTask;
	}


	private class UserState
	{
		public UserState(long id)
		{
			Id = id;
		}


		public long Id { get; }
	}
}
