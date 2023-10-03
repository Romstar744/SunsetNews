using Newtonsoft.Json;
using SunsetNews.Telegram;
using SunsetNews.UserPreferences;
using System.Linq;

namespace SunsetNews.Scheduling.UserPreferencesBased;

internal sealed class UserPreferencesBasedScheduler : IScheduler, IDisposable
{
	private readonly int WorkThreadTimeoutMS = 10000;


	private readonly Dictionary<string, ISchedulerModule> _modules = new();
	private readonly IUserPreference<SchedulerPlanItemStore> _userPreference;
	private readonly CancellationTokenSource _threadCTS = new();
	private readonly Thread _workThread;


	public UserPreferencesBasedScheduler(IUserPreference<SchedulerPlanItemStore> userPreference)
	{
		_userPreference = userPreference;

		_workThread = new Thread(ThreadWorker);
	}


	public void Initialize(IEnumerable<ISchedulerModule> modules)
	{
		foreach (var item in modules)
			_modules.Add(item.ModuleId, item);

		_workThread.Start();
	}

	public void Cancel(UserZoneId user, SchedulerTaskId id)
	{
		_userPreference.Modify(user, store => store.Remove(id));
	}

	public SchedulerTaskId Plan(UserZoneId user, SchedulerTask task, TimeOnly utcTime, SchedulerDayOfWeek days)
	{
		var id = new SchedulerTaskId(Guid.NewGuid());

		var newItem = new SchedulerPlanItem(task.ModuleId, task.FunctionName,
			JsonConvert.SerializeObject(task.Parameter), task.Parameter?.GetType()?.AssemblyQualifiedName ?? "#null",
			(int)days, utcTime, DateTimeOffset.UtcNow);

		_userPreference.Modify(user, store => store.Add(id, newItem));

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

				foreach (var planItemPair in userPlans.Value.Where(needExecuteNow))
				{
					var modificationStore = new Dictionary<SchedulerTaskId, SchedulerPlanItem>();

					var planItem = planItemPair.Value;
					if (_modules.TryGetValue(planItem.SchedulerModuleId, out var module))
					{
						modificationStore.Add(planItemPair.Key, planItem with { LastExecution = DateTimeOffset.UtcNow });

						try
						{
							var parameter = JsonConvert.DeserializeObject(planItem.ParameterJson, Type.GetType(planItem.ParameterTypeAsmQName, throwOnError: true)!);
							module.ExecuteFunction(planItem.FunctionName, parameter);
						}
						catch (Exception ex)
						{
							Console.WriteLine(ex); //TODO: log error
						}
					}

					_userPreference.Modify(userPlans.Key, store => store.SetItems(modificationStore));
				}
			}
		}



		static bool needExecuteNow(KeyValuePair<SchedulerTaskId, SchedulerPlanItem> planItemPair)
		{
			var planItem = planItemPair.Value;
			var today = DateTimeOffset.UtcNow;

			var todayWeekDay = today.DayOfWeek.ToSchedulerDayOfWeek();
			var todayTime = TimeOnly.FromDateTime(today.DateTime);

			return ((SchedulerDayOfWeek)planItem.Days).HasFlag(todayWeekDay) && planItem.UtcTime >= todayTime;
		}
	}
}
