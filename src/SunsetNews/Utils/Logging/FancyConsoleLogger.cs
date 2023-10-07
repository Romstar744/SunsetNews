using Colorify;
using Microsoft.Extensions.Logging;

namespace SunsetNews.Utils.Logging;

internal sealed class FancyConsoleLogger : ILogger
{
	private readonly string _categoryName;
	private readonly Format _console;
	private readonly Stack<ScopeHandler> _scopeStack = new();
	private readonly DateOnly _startTime;
	private static readonly object _syncRoot = new();

	public FancyConsoleLogger(string categoryName, Format format, DateOnly startTime)
	{
		lock (_syncRoot)
		{
			_categoryName = categoryName;
			_console = format;
		}

		_startTime = startTime;
	}


	public IDisposable BeginScope<TState>(TState state) where TState : notnull
	{
		var scope = new ScopeHandler(this, state);
		_scopeStack.Push(scope);
		return scope;
	}

	public bool IsEnabled(LogLevel logLevel)
	{
		return true;
	}

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		var msg = formatter(state, exception);

		var scope = string.Join('/', _scopeStack.Select(s => s.State));

		lock (_syncRoot)
		{
			_console.Write("[Day:");
			_console.Write($"{(DateTime.Now - _startTime.ToDateTime(new TimeOnly(0, 0, 0, 0))).Days} {DateTime.Now:HH:mm:ss}", Colors.txtWarning);
			_console.Write("] [");
			_console.Write(_categoryName, Colors.txtSuccess);
			if (_scopeStack.Count != 0)
			{
				_console.Write("|", Colors.txtDefault);
				_console.Write(scope.ToString(), Colors.txtSuccess);
			}
			_console.Write("|", Colors.txtDefault);
			_console.Write($"{eventId.Name}({eventId.Id})", Colors.txtSuccess);
			_console.Write($"] [");
			_console.Write(logLevel.ToString(), logLevel switch
			{
				LogLevel.Trace => Colors.bgMuted,
				LogLevel.Debug => Colors.bgPrimary,
				LogLevel.Information => Colors.bgInfo,
				LogLevel.Warning => Colors.bgWarning,
				LogLevel.Error => Colors.bgDanger,
				LogLevel.Critical => Colors.bgDanger,
				_ or LogLevel.None => Colors.txtDefault,
			});
			_console.Write($"] : {msg}");

			Console.WriteLine();

			if (exception is not null)
			{
				_console.WriteLine(exception.ToString(), Colors.txtDanger);
			}
		}
	}


	private sealed class ScopeHandler : IDisposable
	{
		private readonly FancyConsoleLogger owner;


		public ScopeHandler(FancyConsoleLogger owner, object? state)
		{
			this.owner = owner;
			State = state;
		}


		public bool Disposed { get; private set; } = false;

		public object? State { get; }


		public void Dispose()
		{
			Disposed = true;
			owner._scopeStack.Pop();
		}
	}
}
