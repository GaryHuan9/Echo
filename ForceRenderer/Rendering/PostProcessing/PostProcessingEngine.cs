using System;
using System.Collections.Generic;
using System.Threading;

namespace ForceRenderer.Rendering.PostProcessing
{
	public class PostProcessingEngine : IDisposable
	{
		public PostProcessingEngine()
		{
			manageThread = new Thread(Manage);
		}

		readonly List<PostProcessingWorker> workers = new List<PostProcessingWorker>();
		readonly Thread manageThread;

		bool aborted;

		public void AddWorker(PostProcessingWorker worker) { }

		public void Dispatch() { }

		public void Abort()
		{
			aborted = true;
			foreach (var worker in workers) worker.Abort();
		}

		public void Dispose()
		{
			if (!aborted) Abort();
		}

		void Manage()
		{
			foreach (var worker in workers) worker.Abort();
		}
	}
}