using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading;
using CodeHelpers.Packed;
using Echo.Terminal.Areas;

namespace Echo.Terminal;

public sealed class Terminal : IDisposable
{
	public Terminal()
	{
		//Configure console
		Console.CursorVisible = false;
		Console.Title = nameof(Echo);
		Console.OutputEncoding = Encoding.UTF8;

		//Launch thread
		thread = new Thread(Main)
		{
			Priority = ThreadPriority.AboveNormal,
			IsBackground = true, Name = "Terminal"
		};

		thread.Start();
	}

	readonly Thread thread;

	int disposed;

	public TimeSpan UpdateDelay { get; set; } = TimeSpan.FromSeconds(1f / 24f);

	void Main()
	{
		var stopwatch = Stopwatch.StartNew();

		var root = new RootUI { };

		while (Volatile.Read(ref disposed) == 0)
		{
			//Update and draw
			Int2 size = new Int2(Console.WindowWidth, Console.WindowHeight);

			root.Size = size;
			root.Update();

			if (size > Int2.Zero)
			{
				Console.SetCursorPosition(0, 0);
				root.Domain.DrawToConsole();
				Console.SetCursorPosition(0, 0);
			}

			//Sleep for update delay
			TimeSpan remain = UpdateDelay - stopwatch.Elapsed;
			if (remain > TimeSpan.Zero) Thread.Sleep(remain);

			stopwatch.Restart();
		}
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref disposed, 1) == 1) return;

		thread.Join();
	}
}