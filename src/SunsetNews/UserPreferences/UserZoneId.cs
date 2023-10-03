namespace SunsetNews.UserPreferences;

internal record struct UserZoneId(long Id)
{
	public static implicit operator UserZoneId(long id)
	{
		return new UserZoneId(id);
	}

	public static implicit operator long(UserZoneId id)
	{
		return id.Id;
	}
}
