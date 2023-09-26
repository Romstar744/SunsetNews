using SunsetNews.Telegram;

namespace SunsetNews.UserSequences.UserWaitConditions
{
	internal sealed class ButtonWaitCondition : UserWaitCondition
	{
		private readonly IMessage _targetMessage;
		private readonly string[]? _buttons;
		private string? _capturedButtonId = null;


		public ButtonWaitCondition(IMessage targetMessage, params string[] buttons)
		{
			_targetMessage = targetMessage;
			_buttons = buttons;
		}

		public ButtonWaitCondition(IMessage targetMessage)
		{
			_targetMessage = targetMessage;
			_buttons = null;
		}


		public string CapturedButtonId => _capturedButtonId ?? throw new InvalidOperationException("No button has been captured yet");


		public override bool PromoteButton(IMessage message, string id)
		{
			if (Equals(message, _targetMessage) == false)
				return false;

			if (_buttons is not null && _buttons.Contains(id) == false)
				return false;

			_capturedButtonId = id;

			return true;
		}

		public override bool PromoteMessage(IMessage message)
		{
			return false;
		}
	}
}
