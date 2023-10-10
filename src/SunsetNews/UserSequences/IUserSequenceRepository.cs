using SunsetNews.Telegram;
using SunsetNews.UserSequences.UserWaitConditions;

namespace SunsetNews.UserSequences;

internal interface IUserSequenceRepository
{
	public bool HasSequence(string awakeCommand);

	public IAsyncEnumerator<UserWaitCondition> InitiateSequence(IMessage message, string awakeCommand);
}
