using SunsetNews.Telegram;
using System.Globalization;

namespace SunsetNews.Localization;

internal interface ICultureSource
{
	public CultureInfo GetCultureInfoFor(IUserChat user);
}


internal static class CultureSourceExtensions
{
	public static void SetupCulture(this ICultureSource source, IUserChat user)
	{
		var culture = source.GetCultureInfoFor(user);

		Thread.CurrentThread.CurrentUICulture = culture;
		Thread.CurrentThread.CurrentCulture = culture;
	}
}
