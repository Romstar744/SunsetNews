namespace SunsetNews.Telegram;

internal interface IUserChat
{
	public long Id { get; }

	public string UserNickname { get; }


	public Task<IMessage> SendMessageAsync(MessageSendModel model);
}
