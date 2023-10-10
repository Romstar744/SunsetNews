using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SunsetNews.Utils.Logging;

internal static class LoggingExtensions
{
	public static ILoggingBuilder AddFancyConsoleLogging(this ILoggingBuilder builder, DateTime start)
	{
		builder.Services.AddTransient(s => new Colorify.Format(Colorify.UI.Theme.Dark));
		builder.Services.AddTransient<ILoggerProvider, FancyConsoleLoggerProvider>((services) =>
			new FancyConsoleLoggerProvider(services.GetRequiredService<Colorify.Format>(), start));
		return builder;
	}
}
