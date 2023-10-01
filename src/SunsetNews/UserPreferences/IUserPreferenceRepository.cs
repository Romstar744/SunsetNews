namespace SunsetNews.UserPreferences;

internal interface IUserPreferenceRepository : IDisposable
{
	public Task PreloadAllAsync();

	public IUserPreference<TPreferenceModel> LoadPreference<TPreferenceModel>() where TPreferenceModel : class, new();
}
