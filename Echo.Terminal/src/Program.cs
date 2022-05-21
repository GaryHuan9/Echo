using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using CodeHelpers.Packed;
using Echo.Terminal;
using Echo.Terminal.Core;
using Echo.Terminal.Core.Interface;

using var program = new Program<EchoTI>();
program.Launch();

namespace Echo.Terminal
{
	public sealed class Program<T> : IDisposable where T : RootTI, new()
	{
		public Program()
		{
			//Configure console
			Console.Title = "Echo Terminal Interface";
			Console.OutputEncoding = Encoding.Unicode;
			Console.TreatControlCAsInput = true;

			//Build root
			root = new T();
		}

		readonly RootTI root;

		int disposed;

		public TimeSpan UpdateDelay { get; set; } = TimeSpan.FromSeconds(1f / 16f);

		public void Launch()
		{
			Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
			root.ProcessArguments(Environment.GetCommandLineArgs());

			var stopwatch = Stopwatch.StartNew();
			var moment = new Moment();

			while (Volatile.Read(ref disposed) == 0)
			{
				//Update
				moment = new Moment(moment, stopwatch);
				Int2 size = new Int2(Console.WindowWidth, Console.WindowHeight);

				root.SetTransform(Int2.Zero, size);
				root.Update(moment);

				//Draw
				if (size > Int2.Zero)
				{
					Console.SetCursorPosition(0, 0);
					root.DrawToConsole();
					Console.CursorVisible = false;
					Console.SetCursorPosition(0, 0);
				}

				//Sleep for delay
				var remain = moment.elapsed - stopwatch.Elapsed + UpdateDelay;
				if (remain > TimeSpan.Zero) Thread.Sleep(remain);
			}
		}

		public void Dispose()
		{
			if (Interlocked.Exchange(ref disposed, 1) == 1) return;

			root.Dispose();
		}
	}
}