using System;

namespace Echo.Core.Common.Diagnostics;

public interface ILogger
{
	public void Write(string text);
	public void WriteWarning(string text);
	public void WriteError(string text);
}

public class ConsoleLogger : ILogger
{
	public void Write(string text) => Console.WriteLine(text);

	public void WriteWarning(string text)
	{
		var foreground = Console.ForegroundColor;
		var background = Console.BackgroundColor;

		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.BackgroundColor = ConsoleColor.Black;

		Console.WriteLine(text);

		Console.ForegroundColor = foreground;
		Console.BackgroundColor = background;
	}

	public void WriteError(string text)
	{
		var foreground = Console.ForegroundColor;
		var background = Console.BackgroundColor;

		Console.ForegroundColor = ConsoleColor.Red;
		Console.BackgroundColor = ConsoleColor.Black;

		Console.WriteLine(text);

		Console.ForegroundColor = foreground;
		Console.BackgroundColor = background;
	}
}