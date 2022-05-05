using System;
using System.Runtime.CompilerServices;
using System.Threading;
using CodeHelpers.Packed;
using CodeHelpers.Threads;
using Echo.Common.Mathematics.Primitives;
using Echo.Common.Memory;
using Echo.Core.Evaluation.Evaluators;
using Echo.Core.Evaluation.Operations;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grid;

namespace Echo.Core.Evaluation.Engines;

/// <summary>
/// A worker class that process/render on a specific tile.
/// Required parameters should be provided to the worker first, then the worker is dispatched.
/// After the work is done, the worker will wait for parameters and completed work to be swapped out and restart processing.
/// </summary>
public class TileWorker : IDisposable
{
	public TileWorker(TiledRenderProfile profile)
	{
		id = Interlocked.Increment(ref workerIdAccumulator);
		size = profile.TileSize;
		this.profile = profile;

		renderBuffer = profile.RenderBuffer;
		evaluator = profile.Method;

		//Allocate thread
		worker = new Thread(WorkThread)
		{
			IsBackground = true,
			Name = $"Tile Worker #{id} {size}x{size}"
		};

		//Create arena for thread. NOTE that HashCode returns a different value every runtime!
		int seed = HashCode.Combine(Environment.TickCount64, id, size);
		arena = evaluator.CreateArena(profile, (uint)seed);
	}

	readonly int id;
	readonly int size;
	readonly TiledRenderProfile profile;

	readonly RenderBuffer renderBuffer;
	readonly Evaluator evaluator;

	int _renderOffsetX;
	int _renderOffsetY;
	long _totalPixel;

	public int RenderOffsetX => InterlockedHelper.Read(ref _renderOffsetX);
	public int RenderOffsetY => InterlockedHelper.Read(ref _renderOffsetY);

	public Int2 RenderOffset => new(RenderOffsetX, RenderOffsetY);
	public long TotalPixel => Interlocked.Read(ref _totalPixel);

	long _completedSample;
	long _completedPixel;
	long _rejectedSample;

	public long CompletedSample => Interlocked.Read(ref _completedSample);
	public long CompletedPixel => Interlocked.Read(ref _completedPixel);
	public long RejectedSample => Interlocked.Read(ref _rejectedSample);

	readonly Thread worker;
	readonly Arena arena;

	readonly ManualResetEventSlim dispatchEvent = new(); //Event sets when the worker is dispatched
	public bool Working => dispatchEvent.IsSet;

	static int workerIdAccumulator;
	volatile bool aborted;

	/// <summary>
	/// Invoked on worker thread when all rendered pixels are stored in the buffer.
	/// Passes in the <see cref="TileWorker"/> for easy identification.
	/// </summary>
	public event Action<TileWorker> OnWorkCompletedMethods;

	public void Reset(Int2 renderOffset)
	{
		if (aborted) throw new Exception("Worker already aborted! It should not be used anymore!");
		if (Working) throw new Exception("Cannot reset when the worker is dispatched and already working!");

		Interlocked.Exchange(ref _renderOffsetX, renderOffset.X);
		Interlocked.Exchange(ref _renderOffsetY, renderOffset.Y);

		Int2 boarder = renderBuffer.size.Min(RenderOffset + (Int2)size); //Get the furthest buffer position
		Interlocked.Exchange(ref _totalPixel, (boarder - RenderOffset).Product);

		Interlocked.Exchange(ref _completedSample, 0);
		Interlocked.Exchange(ref _completedPixel, 0);
		Interlocked.Exchange(ref _rejectedSample, 0);
	}

	public void Dispatch()
	{
		if (aborted) throw new Exception("Worker already aborted!");
		if (Working) throw new Exception("Worker already dispatched!");

		if (!worker.IsAlive) worker.Start();
		dispatchEvent.Set();
	}

	void WorkThread()
	{
		while (!aborted)
		{
			dispatchEvent.Wait();
			if (aborted) goto end;

			try
			{
				for (int x = 0; x < size; x++)
				{
					for (int y = 0; y < size; y++)
					{
						if (aborted) goto end;
						WorkPixel(new Int2(x, y));
					}
				}
			}
			finally { dispatchEvent.Reset(); }

			if (!aborted) OnWorkCompletedMethods?.Invoke(this);
		}

	end:
		{ }
	}

	void WorkPixel(Int2 localPosition)
	{
		Int2 position = localPosition + RenderOffset;
		if (position.Clamp(Int2.Zero, renderBuffer.oneLess) != position) return; //Ignore pixels outside of buffer

		float aspect = 1f / renderBuffer.aspect;
		Accumulator pixel = new Accumulator();

		//Regular pixel sampling
		arena.Distribution.BeginPixel(position);
		Sample(profile.PixelSample);

		//Adaptive sampling (temporary)
		// float deviation = (float)pixel.Deviation.Clamp();
		// Sample((int)(deviation * profile.AdaptiveSample));

		//Store pixel
		pixel.Store(renderBuffer, position);
		Interlocked.Increment(ref _completedPixel);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void Sample(int count)
		{
			for (int i = 0; i < count; i++)
			{
				arena.Distribution.BeginSample();
				Float2 offset = arena.Distribution.Next2D();

				//Sample radiance
				Float2 uv = (position + offset) * renderBuffer.sizeR - Float2.Half;
				Ray ray = profile.Scene.camera.GetRay(uv.ReplaceY(uv.Y * aspect));
				RGB128 radiance = evaluator.Evaluate(ray, profile, arena);

				arena.allocator.Release();

				//Write to pixel
				bool successful = pixel.Add(radiance);
				Interlocked.Increment(ref _completedSample);

				if (!successful) Interlocked.Increment(ref _rejectedSample);
			}
		}
	}

	public void Abort()
	{
		if (aborted) throw new Exception("Worker already aborted!");
		aborted = true;

		if (worker.IsAlive)
		{
			dispatchEvent.Set(); //Release the wait block
			worker.Join();
		}
	}

	public void Dispose()
	{
		if (!aborted) Abort();
		dispatchEvent.Dispose();
	}

	public override int GetHashCode() => id;
	public override string ToString() => $"Tile Worker #{id} {size}x{size}";
}