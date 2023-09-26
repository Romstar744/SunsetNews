using SunsetNews.UserSequences;

namespace SunsetNews.Telegram;

internal interface ITelegramClient
{
    public void UseSequenceProcessor(IUserSequenceProcessor processor);

	public Task ConnectAsync();

    public Task MainLoop();
}
