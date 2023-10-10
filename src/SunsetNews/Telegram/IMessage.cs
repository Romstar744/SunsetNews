namespace SunsetNews.Telegram;

internal interface IMessage
{
	public IUserChat Chat { get; }

	public int Id { get; }

	public string Content { get; }


	public Task DeleteAsync();
}
