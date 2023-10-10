namespace SunsetNews.Telegram;

internal record class MessageSendModel(string Content, MessageButtonGrid? Buttons = null);
