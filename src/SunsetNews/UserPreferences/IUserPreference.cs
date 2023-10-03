namespace SunsetNews.UserPreferences;

internal interface IUserPreference<TPreferenceModel> where TPreferenceModel : class, new()
{
	public TPreferenceModel Get(UserZoneId user);

	public IReadOnlyDictionary<UserZoneId, TPreferenceModel> GetAll();

	public void Modify(UserZoneId user, Func<TPreferenceModel, TPreferenceModel> modificationAction);
}
