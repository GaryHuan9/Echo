using System;
using System.Collections.Generic;
using System.Threading;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.ObjectPooling;
using EchoRenderer.Textures.DimensionTwo;

namespace EchoRenderer.Rendering.PostProcessing
{
	public class PostProcessingEngine : IDisposable
	{
		public PostProcessingEngine(RenderBuffer renderBuffer)
		{
			this.renderBuffer = renderBuffer;
			workThread = new Thread(Work);

			texturePooler = new Array2DPooler(renderBuffer.size);
		}

		public readonly RenderBuffer renderBuffer;
		public readonly Array2DPooler texturePooler;

		readonly List<PostProcessingWorker> workers = new List<PostProcessingWorker>();

		readonly Thread workThread;
		readonly object processLocker = new object();

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

		public class Array2DPooler : PoolerBase<Array2D>
		{
			public Array2DPooler(Int2 size) => this.size = size;

			readonly Int2 size;

			protected override int MaxPoolSize => 16;

			protected override Array2D GetNewObject() => new(size);
			protected override void Reset(Array2D target) { }
		}
	}
}