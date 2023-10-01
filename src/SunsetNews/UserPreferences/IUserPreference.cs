using SunsetNews.Telegram;

namespace SunsetNews.UserPreferences;

internal interface IUserPreference<TPreferenceModel> where TPreferenceModel : class, new()
{
	public TPreferenceModel Get(IUserChat user);

	public void Modify(IUserChat user, Func<TPreferenceModel, TPreferenceModel> modificationAction);
}
