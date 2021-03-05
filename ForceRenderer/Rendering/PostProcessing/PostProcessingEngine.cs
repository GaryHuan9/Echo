using System;
using System.Collections.Generic;
using System.Threading;
using CodeHelpers.Diagnostics;
using CodeHelpers.Files;
using ForceRenderer.Textures;

namespace ForceRenderer.Rendering.PostProcessing
{
	public class PostProcessingEngine : IDisposable
	{
		public PostProcessingEngine(Texture renderBuffer)
		{
			workThread = new Thread(Work);
			this.renderBuffer = renderBuffer;
		}

		readonly List<PostProcessingWorker> workers = new List<PostProcessingWorker>();

		readonly Thread workThread;
		readonly object processLocker = new object();

		public readonly Texture renderBuffer;
		public bool Aborted { get; private set; }

		public void AddWorker(PostProcessingWorker worker)
		{
			Assert.AreEqual(worker.engine, this);

			if (!workThread.IsAlive) workers.Add(worker);
			else throw new Exception("Already dispatched!");
		}

		public void Dispatch()
		{
			if (Aborted) throw new OperationAbortedException();

			workThread.IsBackground = true;
			workThread.Start();
		}

		public void Abort()
		{
			if (!Aborted) Aborted = true;
			else throw new OperationAbortedException();
		}

		public void Dispose()
		{
			if (!Aborted) Abort();
			if (workThread.IsAlive) workThread.Join();
		}

		public void WaitForProcess()
		{
			lock (processLocker) Monitor.Wait(processLocker);
		}

		void Work()
		{
			foreach (var worker in workers)
			{
				if (Aborted) break;
				worker.Dispatch();
			}

			lock (processLocker) Monitor.PulseAll(processLocker);
		}
	}
}