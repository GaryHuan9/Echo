using System;
using System.Threading;

namespace ForceRenderer.Terminals
{
	public class Terminal
	{
		public Terminal()
		{
			displayThread = new Thread(DisplayThread)
							{
								IsBackground = true,
								Priority = ThreadPriority.AboveNormal,
								Name = "Terminal"
							};
			displayThread.Start();
		}

		public float UpdateFrequency { get; set; } = 60f;

		readonly Thread displayThread;

		void DisplayThread()
		{
			Console.Clear();



			Thread.Sleep((int)(1000f / UpdateFrequency));
		}
	}
}