namespace SunsetNews;

internal interface ITelegramClient
{
	public Task ConnectAsync();

	public Task MainLoop();
}
