namespace SunsetNews;

internal record class NotificationPreferences(Guid SchedulerTaskId, string TargetCity)
{
	public bool IsActive() => SchedulerTaskId != Guid.Empty;


	public NotificationPreferences() : this(Guid.Empty, string.Empty)
	{ }
}
