using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SunsetNews.UserPreferences;

namespace SunsetNews.Scheduling.UserPreferencesBased;

internal sealed class UserPreferencesBasedScheduler : IScheduler, IDisposable
{
	public static readonly EventId InitializedLOG = new(11, "Initialized");
	public static readonly EventId TaskCanceledLOG = new(12, "TaskCanceled");
	public static readonly EventId TaskPlannedLOG = new(13, "TaskPlanned");
	public static readonly EventId TaskExecutedLOG = new(14, "TaskExecuted");
	public static readonly EventId TaskExecutionFailLOG = new(21, "TaskExecutionFail");
	public static readonly EventId TaskModuleNotFoundLOG = new(22, "TaskModuleNotFound");


	public static readonly int WorkThreadTimeoutMS = 10000;


	private readonly Dictionary<string, ISchedulerModule> _modules = new();
	private readonly IUserPreference<SchedulerPlanItemStore> _userPreference;
	private readonly ILogger<UserPreferencesBasedScheduler> _logger;
	private readonly CancellationTokenSource _threadCTS = new();
	private readonly Thread _workThread;


	public UserPreferencesBasedScheduler(IUserPreference<SchedulerPlanItemStore> userPreference, ILogger<UserPreferencesBasedScheduler> logger)
	{
		_userPreference = userPreference;
		_logger = logger;
		_workThread = new Thread(ThreadWorker);
	}


	public void Initialize(IEnumerable<ISchedulerModule> modules)
	{
		foreach (var item in modules)
			_modules.Add(item.ModuleId, item);

		_workThread.Start();

		_logger.Log(LogLevel.Information, InitializedLOG, "Scheduler is initialized with this modules: [{Modules}]", string.Join(", ", modules.Select(s => s.ModuleId)));
	}

	public void Cancel(UserZoneId user, SchedulerTaskId id)
	{
		_userPreference.Modify(user, store => store.Remove(id));
		_logger.Log(LogLevel.Debug, TaskCanceledLOG, "Task {Task} has been canceled", id);
	}

	public SchedulerTaskId Plan(UserZoneId user, SchedulerTask task, TimeOnly utcTime, SchedulerDayOfWeek days)
	{
		var id = new SchedulerTaskId(Guid.NewGuid());

		var newItem = new SchedulerPlanItem(task.ModuleId, task.FunctionName,
			JsonConvert.SerializeObject(task.Parameter), task.Parameter?.GetType()?.AssemblyQualifiedName ?? "#null",
			(int)days, utcTime, DateTimeOffset.UtcNow);

		_userPreference.Modify(user, store => store.Add(id, newItem));

		_logger.Log(LogLevel.Debug, TaskPlannedLOG, "Task {Task} has been planned with parameters: {PlanItem}", id, newItem);

		return id;
	}

	public void Dispose()
	{
		_threadCTS.Cancel();
		_workThread.Join();
	}

	private void ThreadWorker()
	{
		var token = _threadCTS.Token;
		while (token.IsCancellationRequested == false)
		{
			Task.Delay(WorkThreadTimeoutMS, token).Wait();
			if (token.IsCancellationRequested)
				return;

			foreach (var userPlans in _userPreference.GetAll())
			{
				if (token.IsCancellationRequested)
					return;

				var modificationStore = new Dictionary<Guid, SchedulerPlanItem>();

				foreach (var planItemPair in userPlans.Value.Where(needExecuteNow))
				{
					var planItem = planItemPair.Value;
					if (_modules.TryGetValue(planItem.SchedulerModuleId, out var module))
					{
						try
						{
							var parameter = JsonConvert.DeserializeObject(planItem.ParameterJson, Type.GetType(planItem.ParameterTypeAsmQName, throwOnError: true)!);
							module.ExecuteFunction(planItem.FunctionName, parameter);
						}
						catch (Exception ex)
						{
							_logger.Log(LogLevel.Error, TaskExecutionFailLOG, ex, "Task {Task} finished with error (Module: {Module})", planItemPair.Key, planItem.SchedulerModuleId);
						}
					}
					else
					{
						_logger.Log(LogLevel.Warning, TaskModuleNotFoundLOG, "Scheduler has no module named {ModuleId} for Task {Task}. Execution skipped", planItem.SchedulerModuleId, planItemPair.Key);
					}

					modificationStore.Add(planItemPair.Key, planItem with { LastExecution = DateTimeOffset.UtcNow });
					_logger.Log(LogLevel.Debug, TaskExecutedLOG, "Task {Task} execution finished", planItemPair.Key);
				}
				
				if (modificationStore.Count > 0)
					_userPreference.Modify(userPlans.Key, store => store.SetItems(modificationStore));
			}
		}



		static bool needExecuteNow(KeyValuePair<Guid, SchedulerPlanItem> planItemPair)
		{
			var planItem = planItemPair.Value;
			var today = DateTimeOffset.UtcNow;

			if (DateOnly.FromDateTime(today.DateTime) == DateOnly.FromDateTime(planItem.LastExecution.DateTime))
				return false;

			var todayWeekDay = today.DayOfWeek.ToSchedulerDayOfWeek();
			var todayTime = TimeOnly.FromDateTime(today.DateTime);

			return ((SchedulerDayOfWeek)planItem.Days).HasFlag(todayWeekDay) && todayTime >= planItem.UtcTime;
		}
	}
}
