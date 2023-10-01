﻿using SunsetNews.Telegram;

namespace SunsetNews.UserPreferences;

internal sealed class DIUserPreference<TPreferenceModel> : IUserPreference<TPreferenceModel> where TPreferenceModel : class, new()
{
	private readonly IUserPreference<TPreferenceModel> _preference;


	public DIUserPreference(IUserPreferenceRepository preferenceRepository)
	{
		_preference = preferenceRepository.LoadPreference<TPreferenceModel>();
	}


	public TPreferenceModel Get(IUserChat user)
	{
		return _preference.Get(user);
	}

	public void Modify(IUserChat user, Func<TPreferenceModel, TPreferenceModel> modificationAction)
	{
		_preference.Modify(user, modificationAction);
	}
}
