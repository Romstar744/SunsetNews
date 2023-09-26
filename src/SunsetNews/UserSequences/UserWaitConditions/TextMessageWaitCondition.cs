using SunsetNews.Telegram;

namespace SunsetNews.UserSequences.UserWaitConditions
{
	internal sealed class TextMessageWaitCondition : UserWaitCondition
	{
		private readonly Predicate<IMessage>? _predicate;
		private IMessage? _capturedMessage = null;


		public TextMessageWaitCondition(Predicate<IMessage> predicate)
		{
			_predicate = predicate;
		}

		public TextMessageWaitCondition()
		{

		}


		public IMessage CapturedMessage => _capturedMessage ?? throw new InvalidOperationException("No message has been captured yet");


		public override bool PromoteButton(IMessage message, string id) => false;

		public override bool PromoteMessage(IMessage message)
		{
			if (_predicate is not null && _predicate(message) == false)
				return false;

			_capturedMessage = message;

			return true;
		}
	}
}
