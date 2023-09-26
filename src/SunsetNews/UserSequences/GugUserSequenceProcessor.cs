using Microsoft.Extensions.Logging;
using SunsetNews.Telegram;
using System.Collections.Concurrent;

namespace SunsetNews.UserSequences;

internal sealed class GugUserSequenceProcessor : IUserSequenceProcessor
{
    private readonly ConcurrentDictionary<long, UserState> _chatsState = new();
    private readonly ILogger<GugUserSequenceProcessor> _logger;


    public GugUserSequenceProcessor(ILogger<GugUserSequenceProcessor> logger)
    {
        _logger = logger;
    }


    public object GetStateForChat(IUserChat chat)
    {
        return _chatsState.GetOrAdd(chat.Id, (id) => new UserState(id));
    }

    public Task PerformButtonAsync(object stateObject, IMessage message, string id)
    {
        var state = (UserState)stateObject;
        _logger.Log(LogLevel.Information, "PerformButtonAsync {User}: {MessageId} -> {ButtonId}", state.Id, message.Id, id);
        return Task.CompletedTask;
    }

    public Task PerformCommandAsync(object stateObject, IMessage message, string command)
    {
        var state = (UserState)stateObject;
        _logger.Log(LogLevel.Information, "PerformCommandAsync {User}: {Message}", state.Id, command);
        return Task.CompletedTask;
    }

    public Task PerformMessageAsync(object stateObject, IMessage message)
    {
        var state = (UserState)stateObject;
        _logger.Log(LogLevel.Information, "PerformMessageAsync {User}: {Message}", state.Id, message.Content);
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
