namespace SunsetNews;

internal interface IUserSequenceProcessor
{
	public object GetStateForUser(long chatId);

	public Task PerformCommandAsync(object stateObject, string command);

	public Task PerformMessageAsync(object stateObject, string message);

	public Task PerformButtonAsync(object stateObject, string message);
}
