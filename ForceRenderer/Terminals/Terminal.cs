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

			commandsController = new CommandsController();
			displayThread.Start();
		}

		public float UpdateFrequency { get; set; } = 60f;
		bool aborted;

		readonly Thread displayThread;
		readonly CommandsController commandsController;

		void DisplayThread()
		{
			while (!aborted)
			{
				Console.Clear();

				commandsController.Update();

				Thread.Sleep((int)(1000f / UpdateFrequency));
			}
		}

		public void Aborted() => aborted = true;
	}
}