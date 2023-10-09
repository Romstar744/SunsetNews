using SunsetNews.Telegram;
using SunsetNews.UserPreferences;

namespace SunsetNews.Scheduling;

internal interface IScheduler
{
	public void Initialize(IEnumerable<ISchedulerModule> modules);

	public SchedulerTaskId Plan(UserZoneId user, SchedulerTask task, TimeOnly localTime, SchedulerDayOfWeek days, TimeZoneInfo timeZone);

	public void Cancel(UserZoneId user, SchedulerTaskId id);
}
