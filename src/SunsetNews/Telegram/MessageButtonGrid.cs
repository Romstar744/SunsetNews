namespace SunsetNews.Telegram;

internal sealed class MessageButtonGrid
{
	private readonly IEnumerable<IEnumerable<MessageButton>> _buttonRows;


	public MessageButtonGrid(IEnumerable<IEnumerable<MessageButton>> buttonRows)
	{
		_buttonRows = buttonRows;
	}

	public MessageButtonGrid(IEnumerable<MessageButton> buttonRow)
	{
		_buttonRows = new[] { buttonRow };
	}

	public MessageButtonGrid(params MessageButton[] buttonRow)
	{
		_buttonRows = new[] { (IEnumerable<MessageButton>)buttonRow };
	}


	public IEnumerable<IEnumerable<MessageButton>> Enumerate() => _buttonRows;
}
