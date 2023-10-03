namespace SunsetNews.Scheduling;

internal interface ISchedulerModule
{
	public string ModuleId { get; }


	public void ExecuteFunction(string functionName, object? parameter);
}
