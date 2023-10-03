namespace SunsetNews.Scheduling;

internal record class SchedulerTask(ISchedulerModule Module, string FunctionName, object? Parameter)
{
	public string ModuleId => Module.ModuleId;
}
