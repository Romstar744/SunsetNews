using Microsoft.Extensions.Logging;
using SunsetNews.Telegram;
using SunsetNews.UserSequences.UserWaitConditions;
using System.Reflection;

namespace SunsetNews.UserSequences.ReflectionRepository;

internal sealed class ReflectionUserSequenceRepository : IUserSequenceRepository
{
	private delegate IAsyncEnumerator<UserWaitCondition> SequenceSource(IMessage message);


	public static readonly EventId SequenceLoadedLOG = new(11, "SequenceLoaded");
	public static readonly EventId SequenceInitiatedLOG = new(12, "SequenceInitiated");


	private readonly Dictionary<string, RepositoryItem> _items = new();
	private readonly ILogger<ReflectionUserSequenceRepository> _logger;


	public ReflectionUserSequenceRepository(IEnumerable<ISequenceModule> modules, ILogger<ReflectionUserSequenceRepository> logger)
	{
		_logger = logger;
		LoadSequences(modules);
	}


	private void LoadSequences(IEnumerable<ISequenceModule> modules)
	{
		foreach (var module in modules)
		{
			var moduleItems = module.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.Select(s => new { Method = s, Attribute = s.GetCustomAttribute<UserSequenceAttribute>() })
				.Where(s => s.Attribute is not null)
				.Select(s =>
				{
					var awakeCommand = s.Attribute?.AwakeCommand!;
					var method = s.Method;

					_logger.Log(LogLevel.Debug, SequenceLoadedLOG, "User sequence loaded to repository from {Module}.{Method} with awake command /{Command}", method.DeclaringType!.FullName, method.Name, awakeCommand);
					return new RepositoryItem(source, awakeCommand);


					IAsyncEnumerator<UserWaitCondition> source(IMessage message) =>
						(IAsyncEnumerator<UserWaitCondition>)(method.Invoke(module, new[] { message }) ?? throw new NullReferenceException());
				});

			foreach (var item in moduleItems)
				_items.Add(item.AwakeCommand, item);
		}
	}

	public bool HasSequence(string awakeCommand)
	{
		return _items.ContainsKey(awakeCommand);
	}

	public IAsyncEnumerator<UserWaitCondition> InitiateSequence(IMessage message, string awakeCommand)
	{
		var item = _items[awakeCommand];

		var sequence = item.Source.Invoke(message);

		_logger.Log(LogLevel.Debug, SequenceInitiatedLOG, "Sequence initiated using /{AwakeCommand} command", awakeCommand);

		return sequence;
	}


	private record class RepositoryItem(SequenceSource Source, string AwakeCommand);
}
