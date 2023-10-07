namespace SunsetNews.Scheduling;

internal readonly record struct SchedulerTaskId(Guid Id)
{
	public static implicit operator SchedulerTaskId(Guid id)
	{
		return new SchedulerTaskId(id);
	}

	public static implicit operator Guid(SchedulerTaskId id)
	{
		return id.Id;
	}

	public override string ToString()
	{
		return $"{{SchedulerTaskId {Id}}}";
	}
}
