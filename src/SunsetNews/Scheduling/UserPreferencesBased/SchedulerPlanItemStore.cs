using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace SunsetNews.Scheduling.UserPreferencesBased;

internal sealed class SchedulerPlanItemStore : IImmutableDictionary<SchedulerTaskId, SchedulerPlanItem>
{
	public static SchedulerPlanItemStore Empty { get; } = new SchedulerPlanItemStore();


	private readonly ImmutableDictionary<SchedulerTaskId, SchedulerPlanItem> _innerCollection;


	public SchedulerPlanItemStore()
	{
		_innerCollection = ImmutableDictionary<SchedulerTaskId, SchedulerPlanItem>.Empty;
	}

	private SchedulerPlanItemStore(ImmutableDictionary<SchedulerTaskId, SchedulerPlanItem> collection)
	{
		_innerCollection = collection;
	}


	public SchedulerPlanItem this[SchedulerTaskId key] => ((IReadOnlyDictionary<SchedulerTaskId, SchedulerPlanItem>)_innerCollection)[key];


	public IEnumerable<SchedulerTaskId> Keys => _innerCollection.Keys;

	public IEnumerable<SchedulerPlanItem> Values => _innerCollection.Values;

	public int Count => _innerCollection.Count;


	public SchedulerPlanItemStore Add(SchedulerTaskId key, SchedulerPlanItem value) => new(_innerCollection.Add(key, value));
	IImmutableDictionary<SchedulerTaskId, SchedulerPlanItem> IImmutableDictionary<SchedulerTaskId, SchedulerPlanItem>.Add(SchedulerTaskId key, SchedulerPlanItem value) => Add(key, value);

	public SchedulerPlanItemStore AddRange(IEnumerable<KeyValuePair<SchedulerTaskId, SchedulerPlanItem>> pairs) => new(_innerCollection.AddRange(pairs));
	IImmutableDictionary<SchedulerTaskId, SchedulerPlanItem> IImmutableDictionary<SchedulerTaskId, SchedulerPlanItem>.AddRange(IEnumerable<KeyValuePair<SchedulerTaskId, SchedulerPlanItem>> pairs) => AddRange(pairs);

	public SchedulerPlanItemStore Clear() => Empty;
	IImmutableDictionary<SchedulerTaskId, SchedulerPlanItem> IImmutableDictionary<SchedulerTaskId, SchedulerPlanItem>.Clear() => Clear();

	public bool Contains(KeyValuePair<SchedulerTaskId, SchedulerPlanItem> pair) => _innerCollection.Contains(pair);

	public bool ContainsKey(SchedulerTaskId key) => _innerCollection.ContainsKey(key);

	public IEnumerator<KeyValuePair<SchedulerTaskId, SchedulerPlanItem>> GetEnumerator() => _innerCollection.GetEnumerator();

	public SchedulerPlanItemStore Remove(SchedulerTaskId key) => new(_innerCollection.Remove(key));
	IImmutableDictionary<SchedulerTaskId, SchedulerPlanItem> IImmutableDictionary<SchedulerTaskId, SchedulerPlanItem>.Remove(SchedulerTaskId key) => Remove(key);

	public SchedulerPlanItemStore RemoveRange(IEnumerable<SchedulerTaskId> keys) => new(_innerCollection.RemoveRange(keys));
	IImmutableDictionary<SchedulerTaskId, SchedulerPlanItem> IImmutableDictionary<SchedulerTaskId, SchedulerPlanItem>.RemoveRange(IEnumerable<SchedulerTaskId> keys) => RemoveRange(keys);

	public SchedulerPlanItemStore SetItem(SchedulerTaskId key, SchedulerPlanItem value) => new(_innerCollection.SetItem(key, value));
	IImmutableDictionary<SchedulerTaskId, SchedulerPlanItem> IImmutableDictionary<SchedulerTaskId, SchedulerPlanItem>.SetItem(SchedulerTaskId key, SchedulerPlanItem value) => SetItem(key, value);

	public SchedulerPlanItemStore SetItems(IEnumerable<KeyValuePair<SchedulerTaskId, SchedulerPlanItem>> items) => new(_innerCollection.SetItems(items));
	IImmutableDictionary<SchedulerTaskId, SchedulerPlanItem> IImmutableDictionary<SchedulerTaskId, SchedulerPlanItem>.SetItems(IEnumerable<KeyValuePair<SchedulerTaskId, SchedulerPlanItem>> items) => SetItems(items);

	public bool TryGetKey(SchedulerTaskId equalKey, out SchedulerTaskId actualKey) => _innerCollection.TryGetKey(equalKey, out actualKey);

	public bool TryGetValue(SchedulerTaskId key, [MaybeNullWhen(false)] out SchedulerPlanItem value) => _innerCollection.TryGetValue(key, out value);
	
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_innerCollection).GetEnumerator();
}
