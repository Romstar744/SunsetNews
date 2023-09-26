namespace SunsetNews.UserSequences.ReflectionRepository;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal sealed class UserSequenceAttribute : Attribute
{
	public UserSequenceAttribute(string awakeCommand)
	{
		AwakeCommand = awakeCommand;
	}


	public string AwakeCommand { get; }
}
