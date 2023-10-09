using SunsetNews.Scheduling;

namespace SunsetNews;

internal record class NotificationPreferences(Guid SchedulerTaskId, string TargetCity, string TimeZoneSerializedForm, TimeOnly LocalTime, SchedulerDayOfWeek WeekOfDays)
{
	public bool IsActive() => SchedulerTaskId != Guid.Empty;


	public NotificationPreferences() : this(Guid.Empty, string.Empty, TimeZoneInfo.Utc.ToSerializedString(), new TimeOnly(), 0)
	{ }
}
