namespace SunsetNews;

internal record class TimeZonePreferences(string SerializedTimeZoneInfo)
{
	private TimeZoneInfo? _timeZone;


	public TimeZonePreferences() : this(TimeZoneInfo.Utc.ToSerializedString())
	{ }


	public TimeZoneInfo GetTimeZone()
	{
		_timeZone ??= TimeZoneInfo.FromSerializedString(SerializedTimeZoneInfo);
		return _timeZone;
	}

	public static TimeZonePreferences FromTimeZone(TimeZoneInfo timeZone)
	{
		return new TimeZonePreferences(timeZone.ToSerializedString());
	}
}
