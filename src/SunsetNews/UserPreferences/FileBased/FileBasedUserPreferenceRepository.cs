using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SunsetNews.Telegram;
using System.Collections.Concurrent;
using System.Reflection;

namespace SunsetNews.UserPreferences.FileBased;

internal sealed class FileBasedUserPreferenceRepository : IUserPreferenceRepository
{
	private readonly Options _options;
	private readonly Dictionary<Type, UserPreference> _cache = new();
	private readonly ConcurrentQueue<ValueWriteTask> _tasks = new();
	private readonly AutoResetEvent _onNewWriteTask = new(false);
	private readonly Thread _dataWriteThread;
	private bool _disposed = false;


	public FileBasedUserPreferenceRepository(IOptions<Options> options)
	{
		_options = options.Value;
		_dataWriteThread = new(DataWriteThreadWorker);
	}

	public void Dispose()
	{
		_disposed = true;
		_onNewWriteTask.Set();
		_dataWriteThread.Join();
	}

	public Task PreloadAllAsync()
	{
		_dataWriteThread.Start();

		if (Directory.Exists(_options.BaseDirectory) == false)
			Directory.CreateDirectory(_options.BaseDirectory);

		var directories = Directory.EnumerateDirectories(_options.BaseDirectory);
		var tasks = directories.Select(loadDirectory).ToArray();

		return Task.WhenAll(tasks);

		

		async Task loadDirectory(string directory)
		{
			try
			{
				var modelType = Type.GetType(await File.ReadAllTextAsync(Path.Combine(directory, "model.ini")), throwOnError: true, ignoreCase: false) ?? throw new NullReferenceException();
				await (Task)GetType().GetMethod(nameof(PreloadPreferenceAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
					.MakeGenericMethod(modelType)
					.Invoke(this, new object[] { directory })!;
			}
			catch (Exception)
			{
				//TODO: failed to load case
			}
		}
	}

	public IUserPreference<TPreferenceModel> LoadPreference<TPreferenceModel>() where TPreferenceModel : class, new()
	{
		var type = typeof(TPreferenceModel);

		if (_cache.TryGetValue(type, out var value))
		{
			return (UserPreference<TPreferenceModel>)value;
		}
		else
		{
			var directory = Path.Combine(_options.BaseDirectory, type.FullName!);
			Directory.CreateDirectory(directory);
			File.WriteAllText(Path.Combine(directory, "model.ini"), type.AssemblyQualifiedName);

			var preference = new UserPreference<TPreferenceModel>(this, new());
			_cache.Add(type, preference);
			return preference;
		}
	}

	private async Task PreloadPreferenceAsync<TPreferenceModel>(string directory) where TPreferenceModel : class, new()
	{
		var files = Directory.EnumerateFiles(directory, "*.json");

		var data = new Dictionary<UserZoneId, TPreferenceModel>();

		foreach (var file in files)
		{
			try
			{
				var userId = long.Parse(Path.GetFileNameWithoutExtension(file));
				var fileContent = await File.ReadAllTextAsync(file);
				var preferenceModel = JsonConvert.DeserializeObject<TPreferenceModel>(fileContent);

				if (preferenceModel is not null)
					data.Add(userId, preferenceModel);
			}
			catch (Exception)
			{
				//TODO: failed to load case
			}
		}

		_cache.Add(typeof(TPreferenceModel), new UserPreference<TPreferenceModel>(this, data));
	}

	private void DataWriteThreadWorker()
	{
		while(_disposed == false)
		{
			_onNewWriteTask.WaitOne();
			if (_disposed)
				break;

			while(_tasks.TryDequeue(out var task))
			{
				executeTask(task);
			}
		}



		void executeTask(ValueWriteTask task)
		{
			File.WriteAllText(task.DestinationFile, JsonConvert.SerializeObject(task.Value));
		}
	}

	private void DispatchModification<TPreferenceModel>(UserZoneId user, TPreferenceModel model) where TPreferenceModel : class, new()
	{
		var task = new ValueWriteTask(Path.Combine(_options.BaseDirectory, typeof(TPreferenceModel).FullName!, $"{user.Id}.json"), model);
		_tasks.Enqueue(task);
		_onNewWriteTask.Set();
	}


	public class Options
	{
		public required string BaseDirectory { get; init; }
	}

	private readonly record struct ValueWriteTask(string DestinationFile, object Value);

	private abstract class UserPreference
	{

	}

	private sealed class UserPreference<TPreferenceModel> : UserPreference, IUserPreference<TPreferenceModel> where TPreferenceModel : class, new()
	{
		private readonly FileBasedUserPreferenceRepository _owner;
		private readonly Dictionary<UserZoneId, TPreferenceModel> _cachedValues;
		private readonly Mutex _mutex = new();


		public UserPreference(FileBasedUserPreferenceRepository owner, Dictionary<UserZoneId, TPreferenceModel> initialValues)
		{
			_owner = owner;
			_cachedValues = initialValues;
		}


		public TPreferenceModel Get(UserZoneId user)
		{
			_mutex.WaitOne();
			try
			{
				if (_cachedValues.TryGetValue(user.Id, out var value) == false)
					value = new();
				return value;
			}
			finally
			{
				_mutex.ReleaseMutex();
			}
		}

		public void Modify(UserZoneId user, Func<TPreferenceModel, TPreferenceModel> modificationAction)
		{
			_mutex.WaitOne();
			try
			{
				TPreferenceModel modifiedValue;

				if (_cachedValues.TryGetValue(user.Id, out var value))
				{
					modifiedValue = modificationAction(value);
					_cachedValues[user.Id] = modifiedValue;
				}
				else
				{
					modifiedValue = modificationAction(new());
					_cachedValues.Add(user.Id, modifiedValue);
				}

				_owner.DispatchModification(user, modifiedValue);
			}
			finally
			{
				_mutex.ReleaseMutex();
			}
		}

		public IReadOnlyDictionary<UserZoneId, TPreferenceModel> GetAll() => _cachedValues;
	}
}
