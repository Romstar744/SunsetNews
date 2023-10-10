using SunsetNews.UserPreferences;

namespace SunsetNews.Scheduling;

internal interface ISchedulerModule
{
	public string ModuleId { get; }


	public Task ExecuteFunctionAsync(string functionName, object? parameter, UserZoneId user);
}
