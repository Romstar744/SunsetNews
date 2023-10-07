using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace SunsetNews.Scheduling.UserPreferencesBased;

internal sealed class SchedulerPlanItemStore : IImmutableDictionary<Guid, SchedulerPlanItem>
{
	public static SchedulerPlanItemStore Empty { get; } = new SchedulerPlanItemStore();


	private readonly ImmutableDictionary<Guid, SchedulerPlanItem> _innerCollection;


	public SchedulerPlanItemStore()
	{
		_innerCollection = ImmutableDictionary<Guid, SchedulerPlanItem>.Empty;
	}

	private SchedulerPlanItemStore(ImmutableDictionary<Guid, SchedulerPlanItem> collection)
	{
		_innerCollection = collection;
	}

	public SchedulerPlanItemStore(IEnumerable<KeyValuePair<Guid, SchedulerPlanItem>> collection)
	{
		_innerCollection = ImmutableDictionary<Guid, SchedulerPlanItem>.Empty.AddRange(collection);
	}


	public SchedulerPlanItem this[Guid key] => ((IReadOnlyDictionary<Guid, SchedulerPlanItem>)_innerCollection)[key];


	public IEnumerable<Guid> Keys => _innerCollection.Keys;

	public IEnumerable<SchedulerPlanItem> Values => _innerCollection.Values;

	public int Count => _innerCollection.Count;


	public SchedulerPlanItemStore Add(Guid key, SchedulerPlanItem value) => new(_innerCollection.Add(key, value));
	IImmutableDictionary<Guid, SchedulerPlanItem> IImmutableDictionary<Guid, SchedulerPlanItem>.Add(Guid key, SchedulerPlanItem value) => Add(key, value);

	public SchedulerPlanItemStore AddRange(IEnumerable<KeyValuePair<Guid, SchedulerPlanItem>> pairs) => new(_innerCollection.AddRange(pairs));
	IImmutableDictionary<Guid, SchedulerPlanItem> IImmutableDictionary<Guid, SchedulerPlanItem>.AddRange(IEnumerable<KeyValuePair<Guid, SchedulerPlanItem>> pairs) => AddRange(pairs);

	public SchedulerPlanItemStore Clear() => Empty;
	IImmutableDictionary<Guid, SchedulerPlanItem> IImmutableDictionary<Guid, SchedulerPlanItem>.Clear() => Clear();

	public bool Contains(KeyValuePair<Guid, SchedulerPlanItem> pair) => _innerCollection.Contains(pair);

	public bool ContainsKey(Guid key) => _innerCollection.ContainsKey(key);

	public IEnumerator<KeyValuePair<Guid, SchedulerPlanItem>> GetEnumerator() => _innerCollection.GetEnumerator();

	public SchedulerPlanItemStore Remove(Guid key) => new(_innerCollection.Remove(key));
	IImmutableDictionary<Guid, SchedulerPlanItem> IImmutableDictionary<Guid, SchedulerPlanItem>.Remove(Guid key) => Remove(key);

	public SchedulerPlanItemStore RemoveRange(IEnumerable<Guid> keys) => new(_innerCollection.RemoveRange(keys));
	IImmutableDictionary<Guid, SchedulerPlanItem> IImmutableDictionary<Guid, SchedulerPlanItem>.RemoveRange(IEnumerable<Guid> keys) => RemoveRange(keys);

	public SchedulerPlanItemStore SetItem(Guid key, SchedulerPlanItem value) => new(_innerCollection.SetItem(key, value));
	IImmutableDictionary<Guid, SchedulerPlanItem> IImmutableDictionary<Guid, SchedulerPlanItem>.SetItem(Guid key, SchedulerPlanItem value) => SetItem(key, value);

	public SchedulerPlanItemStore SetItems(IEnumerable<KeyValuePair<Guid, SchedulerPlanItem>> items) => new(_innerCollection.SetItems(items));
	IImmutableDictionary<Guid, SchedulerPlanItem> IImmutableDictionary<Guid, SchedulerPlanItem>.SetItems(IEnumerable<KeyValuePair<Guid, SchedulerPlanItem>> items) => SetItems(items);

	public bool TryGetKey(Guid equalKey, out Guid actualKey) => _innerCollection.TryGetKey(equalKey, out actualKey);

	public bool TryGetValue(Guid key, [MaybeNullWhen(false)] out SchedulerPlanItem value) => _innerCollection.TryGetValue(key, out value);
	
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_innerCollection).GetEnumerator();
}
