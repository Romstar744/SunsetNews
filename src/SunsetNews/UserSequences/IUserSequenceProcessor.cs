using SunsetNews.Telegram;

namespace SunsetNews.UserSequences;

internal interface IUserSequenceProcessor
{
    public object GetStateForChat(IUserChat chat);

	public Task PerformCommandAsync(object stateObject, IMessage message, string command);

	public Task PerformMessageAsync(object stateObject, IMessage message);

    public Task PerformButtonAsync(object stateObject, IMessage message, string id);
}
