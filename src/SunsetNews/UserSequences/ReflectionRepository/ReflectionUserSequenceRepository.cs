using SunsetNews.Telegram;
using SunsetNews.UserSequences.UserWaitConditions;
using System.Reflection;

namespace SunsetNews.UserSequences.ReflectionRepository;

internal sealed class ReflectionUserSequenceRepository : IUserSequenceRepository
{
	private delegate IAsyncEnumerator<UserWaitCondition> SequenceSource(IMessage message);


	private readonly Dictionary<string, RepositoryItem> _items = new();


	public ReflectionUserSequenceRepository(IEnumerable<ISequenceModule> modules)
	{
		LoadSequences(modules);
	}



	public void LoadSequences(IEnumerable<ISequenceModule> modules)
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

		return sequence;
	}


	private record class RepositoryItem(SequenceSource Source, string AwakeCommand);
}
