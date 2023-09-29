namespace SunsetNews.Utils;

internal readonly record struct FloatRange(float Min, float Max)
{
	public bool InRange(float value)
	{
		return value >= Min && value <= Max;
	}

	public override string ToString()
	{
		return $"{Min} - {Max}";
	}
}
