namespace SunsetNews.Utils;

internal record struct TimeRange(TimeOnly Start, TimeOnly End)
{
	public bool InRange(TimeOnly value)
	{
		return value >= Start && value <= End;
	}

	public override string ToString()
	{
		return $"{Start:HH:mm} - {End:HH:mm}";
	}
}
