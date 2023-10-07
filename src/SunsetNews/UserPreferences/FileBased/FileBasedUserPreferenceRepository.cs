using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SunsetNews.Telegram;
using System.Collections.Concurrent;
using System.Reflection;

namespace SunsetNews.UserPreferences.FileBased;

internal sealed class FileBasedUserPreferenceRepository : IUserPreferenceRepository
{
	public static readonly EventId PreferencesPreLoadedLOG = new(11, "PreferencesPreLoaded");
	public static readonly EventId PreferenceRequestedLOG = new(12, "PreferencesRequested");
	public static readonly EventId PreferencesLoadingFailLOG = new(21, "PreferencesLoadingFail");
	public static readonly EventId PreferencesSaveFailLOG = new(22, "PreferencesSaveFail");
	public static readonly EventId PreferenceModifiedLOG = new(31, "PreferenceModified");


	private readonly Options _options;
	private readonly Dictionary<Type, UserPreference> _cache = new();
	private readonly ConcurrentQueue<ValueWriteTask> _tasks = new();
	private readonly AutoResetEvent _onNewWriteTask = new(false);
	private readonly Thread _dataWriteThread;
	private readonly ILogger<FileBasedUserPreferenceRepository> _logger;
	private bool _disposed = false;


	public FileBasedUserPreferenceRepository(IOptions<Options> options, ILogger<FileBasedUserPreferenceRepository> _logger)
	{
		_options = options.Value;
		_dataWriteThread = new(DataWriteThreadWorker);
		this._logger = _logger;
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

				_logger.Log(LogLevel.Information, PreferencesPreLoadedLOG, "Preferences of type {Model type} loaded for all users", modelType.FullName);
			}
			catch (Exception ex)
			{
				_logger.Log(LogLevel.Critical, PreferencesLoadingFailLOG, ex, "Enable to load data directory {DirectoryPath}", directory);
				throw;
			}
		}
	}

	// Interface method
	public IUserPreference<TPreferenceModel> LoadPreference<TPreferenceModel>() where TPreferenceModel : class, new()
	{
		var type = typeof(TPreferenceModel);

		try
		{
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
		finally
		{
			_logger.Log(LogLevel.Trace, PreferenceRequestedLOG, "Preference of type {ModelType} has been requested", type.FullName);
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
			catch (Exception ex)
			{
				throw new Exception($"Enable to load file with user preferences - {file}", ex);
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
				try
				{
					executeTask(task);
				}
				catch (Exception ex)
				{
					_logger.Log(LogLevel.Error, PreferencesSaveFailLOG, ex, "Enable to save preferences to file, destination: {DestinationFile}", task.DestinationFile);
				}
			}
		}



		static void executeTask(ValueWriteTask task)
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

				_owner._logger.Log(LogLevel.Debug, PreferenceModifiedLOG, "Preference of type {ModelType} for {User} modified to {NewValue}", typeof(TPreferenceModel).FullName, user, modifiedValue);
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
