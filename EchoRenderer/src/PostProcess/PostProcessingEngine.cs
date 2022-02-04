using System;
using System.Collections.Generic;
using System.Threading;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Pooling;
using EchoRenderer.Common;
using EchoRenderer.Textures.Grid;

namespace EchoRenderer.PostProcess;

public class PostProcessingEngine : IDisposable
{
	public PostProcessingEngine(RenderBuffer renderBuffer)
	{
		this.renderBuffer = renderBuffer;
		workThread = new Thread(Work);

		texturePooler = new ArrayGridPooler(renderBuffer.size);
	}

	public readonly RenderBuffer renderBuffer;
	public readonly ArrayGridPooler texturePooler;

	readonly List<PostProcessingWorker> workers = new();

	readonly Thread workThread;
	readonly object processLocker = new();

	public bool Aborted { get; private set; }
	public bool Finished { get; private set; }

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
		lock (processLocker)
		{
			if (Finished) return;
			Monitor.Wait(processLocker);
		}
	}

	void Work()
	{
		foreach (var worker in workers)
		{
			if (Aborted) break;
			worker.Dispatch();
		}

		lock (processLocker)
		{
			Finished = true;
			Monitor.PulseAll(processLocker);
		}
	}

	public class ArrayGridPooler : PoolerBase<ArrayGrid>
	{
		public ArrayGridPooler(Int2 size) => this.size = size;

		readonly Int2 size;

		protected override int MaxPoolSize => 16;

		protected override ArrayGrid GetNewObject() => new(size);
		protected override void Reset(ArrayGrid target) { }
	}
}