using SunsetNews.Telegram;
using SunsetNews.UserPreferences;
using System.Globalization;

namespace SunsetNews.Localization;

internal sealed class PreferencesBasedCultureSource : ICultureSource
{
	private readonly IUserPreference<CulturePreferences> _preference;


	public PreferencesBasedCultureSource(IUserPreference<CulturePreferences> preference)
	{
		_preference = preference;
	}

	public CultureInfo GetCultureInfoFor(IUserChat user)
	{
		return _preference.Get(user.Id).Culture;
	}
}
