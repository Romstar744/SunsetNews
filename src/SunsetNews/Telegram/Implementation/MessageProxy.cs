using Telegram.Bot;
using Telegram.Bot.Types;

namespace SunsetNews.Telegram.Implementation;

internal sealed class MessageProxy : IMessage
{
	private readonly TelegramBotClient _bot;
	private readonly Message _message;


	public MessageProxy(TelegramBotClient bot, UserChatProxy chat, Message message)
	{
		_bot = bot;
		Chat = chat;
		_message = message;
	}


	public int Id => _message.MessageId;

	public string Content => _message.Text ?? throw new NullReferenceException();

	public IUserChat Chat { get; }


	public Task DeleteAsync()
	{
		return _bot.DeleteMessageAsync(Chat.Id, Id);
	}

	public override bool Equals(object? obj) => obj is MessageProxy messageProxy && messageProxy.Id == Id;

	public override int GetHashCode() => Id;
}
