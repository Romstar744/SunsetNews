namespace SunsetNews.Scheduling.UserPreferencesBased;

internal record class SchedulerPlanItem(string SchedulerModuleId,
		string FunctionName,
		string ParameterJson,
		string ParameterTypeAsmQName,
		int Days, TimeOnly UtcTime, DateTimeOffset LastExecution);
