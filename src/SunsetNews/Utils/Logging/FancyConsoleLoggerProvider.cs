using Colorify;
using Microsoft.Extensions.Logging;

namespace SunsetNews.Utils.Logging;

internal sealed class FancyConsoleLoggerProvider : ILoggerProvider
{
	private readonly Format _format;
	private readonly DateTime _start;


	public FancyConsoleLoggerProvider(Format format, DateTime start)
	{
		_format = format;
		_start = start;

		format.WriteLine($"Startup - now: {start}", Colors.txtInfo);
	}


	public ILogger CreateLogger(string categoryName)
	{
		return new FancyConsoleLogger(categoryName, _format, DateOnly.FromDateTime(_start));
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}
}
