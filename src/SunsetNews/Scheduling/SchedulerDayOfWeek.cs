namespace SunsetNews.Scheduling;

[Flags]
internal enum SchedulerDayOfWeek
{
	Monday = 1 >> 0,
	Tuesday = 1 >> 1,
	Wednesday = 1 >> 2,
	Thursday = 1 >> 3,
	Friday = 1 >> 4,
	Saturday = 1 >> 5,
	Sunday = 1 >> 6,

	Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
	Weekends = Saturday | Sunday,
	AllDays = Weekdays | Weekends
}


internal static class SchedulerDayOfWeekExtensions
{
	public static SchedulerDayOfWeek ToSchedulerDayOfWeek(this DayOfWeek dayOfWeek)
	{
		return dayOfWeek switch
		{
			DayOfWeek.Sunday => SchedulerDayOfWeek.Sunday,
			DayOfWeek.Monday => SchedulerDayOfWeek.Monday,
			DayOfWeek.Tuesday => SchedulerDayOfWeek.Tuesday,
			DayOfWeek.Wednesday => SchedulerDayOfWeek.Wednesday,
			DayOfWeek.Thursday => SchedulerDayOfWeek.Thursday,
			DayOfWeek.Friday => SchedulerDayOfWeek.Friday,
			DayOfWeek.Saturday => SchedulerDayOfWeek.Saturday,
			_ => throw new NotSupportedException(),
		};
	}
}
