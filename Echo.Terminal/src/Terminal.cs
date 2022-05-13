using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using CodeHelpers.Packed;
using Echo.Terminal.Core.Interface;

namespace Echo.Terminal;

public sealed class Terminal<T> : IDisposable where T : RootTI, new()
{
	public Terminal()
	{
		//Configure console
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

		RootTI root = new T();

		while (Volatile.Read(ref disposed) == 0)
		{
			//Update and draw
			Int2 size = new Int2(Console.WindowWidth, Console.WindowHeight);
			root.SetTransform(Int2.Zero, size);

			root.Update();

			if (size > Int2.Zero)
			{
				Console.SetCursorPosition(0, 0);
				root.DrawToConsole();
				Console.CursorVisible = false;
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