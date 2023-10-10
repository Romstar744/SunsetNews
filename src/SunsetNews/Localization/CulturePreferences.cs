using System.Globalization;

namespace SunsetNews.Localization;

internal record class CulturePreferences(CultureInfo Culture)
{
	public CulturePreferences() : this(new CultureInfo("en"))
	{

	}
}
