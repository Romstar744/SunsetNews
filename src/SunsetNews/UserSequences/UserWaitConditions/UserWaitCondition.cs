using SunsetNews.Telegram;

namespace SunsetNews.UserSequences.UserWaitConditions;

internal abstract class UserWaitCondition
{
    public abstract bool PromoteMessage(IMessage message);

    public abstract bool PromoteButton(IMessage message, string id);
}