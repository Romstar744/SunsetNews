using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SunsetNews.Telegram.Implementation
{
	internal sealed class UserChatProxy : IUserChat
	{
		private readonly TelegramBotClient _bot;
		private readonly Chat _chat;


		public UserChatProxy(TelegramBotClient bot, Chat chat)
		{
			_bot = bot;
			_chat = chat;
		}


		public long Id => _chat.Id;

		public string UserNickname => _chat.FirstName ?? string.Empty;


		public async Task<IMessage> SendMessageAsync(MessageSendModel model)
		{
			InlineKeyboardMarkup? markup = null;

			if (model.Buttons is not null)
			{
				var grid = model.Buttons.Enumerate().Select(s => s.Select(btn => new InlineKeyboardButton(btn.Label) { CallbackData = btn.Id }));
				markup = new InlineKeyboardMarkup(grid);
			}

			var message = await _bot.SendTextMessageAsync(_chat, model.Content, replyMarkup: markup);

			return new MessageProxy(_bot, this, message);
		}

		public override bool Equals(object? obj) => obj is UserChatProxy userChatProxy && userChatProxy.Id == Id;

		public override int GetHashCode() => Id.GetHashCode();

		public override string ToString()
		{
			return $"{{TelegramUser {Id}}}";
		}
	}
}
