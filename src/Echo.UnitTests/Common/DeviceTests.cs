using System;
using System.Collections.Immutable;
using System.Threading;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Compute.Async;
using NUnit.Framework;

namespace Echo.UnitTests.Common;

[TestFixture]
[SingleThreaded]
public class DeviceTests
{
	[SetUp]
	public void SetUp() => device = new Device();

	[TearDown]
	public void TearDown()
	{
		device.Dispose();
		device = null;
	}

	Device device;

	const int PauseCount = 10;

	//NOTE: testing for time is not a good idea! I tried :(

	[Test]
	[Sequential]
	public void Simple([Values(1.0f, 2.0f, 2.7f, 0.3f, 0.0f)] float multiplier,
					   [Values(8f, 11f, 10f, 6f, 1f)] float tolerance)
	{
		Operation operation = device.Schedule(new SleepOperation.Factory(multiplier));

		PauseSeries(device);
		device.Operations.Await(operation);
		AssertCompleted(operation);
	}

	[Test]
	[Sequential]
	public void Multiple([Values(1.0f, 0.7f, 2.1f, 0.1f, 0.0f)] float multiplier0,
						 [Values(1.0f, 1.5f, 0.0f, 0.1f, 0.0f)] float multiplier1,
						 [Values(1.0f, 1.0f, 0.3f, 0.1f, 1.9f)] float multiplier2,
						 [Values(0.3f, 0.5f, 0.4f, 0.3f, 0.2f)] float tolerance)
	{
		var operations = new[]
		{
			device.Schedule(new SleepOperation.Factory(multiplier0)),
			device.Schedule(new SleepOperation.Factory(multiplier1)),
			device.Schedule(new SleepOperation.Factory(multiplier2))
		};

		foreach (Operation operation in operations)
		{
			PauseSeries(device);
			device.Operations.Await(operation);
			AssertCompleted(operation);
		}
	}

	[Test]
	public void AsyncSimple()
	{
		Operation operation = device.Schedule(AsyncOperation.New(Main));

		PauseSeries(device);
		device.Operations.Await(operation);
		AssertCompleted(operation);

		static async ComputeTask Main(AsyncOperation operation)
		{
			Stage stage = new Stage();
			stage.Next(1);

			await operation.Schedule(() => stage.Next(2));
			stage.Next(3);

			await operation.Schedule(() => stage.Next(4));
			stage.Next(5);

			await operation.Schedule(_ => stage.Skip(), 100);
			stage.Next(6 + 100);
		}
	}

	[Test]
	public void AsyncComplex()
	{
		Operation operation = device.Schedule(AsyncOperation.New(Main));

		PauseSeries(device);
		device.Operations.Await(operation);
		AssertCompleted(operation);

		static async ComputeTask Main(AsyncOperation operation)
		{
			Stage stage = new Stage();
			stage.Next(1);

			var task0 = operation.Schedule(stage.Skip);
			var task1 = operation.Schedule(stage.Skip);

			await task0;
			await task1;
			stage.Next(4);

			var pairs = new (uint expect, ComputeTask<uint> task)[100];

			for (uint i = 0; i < pairs.Length; i++)
			{
				pairs[i] = (i + 5, Construct(operation, stage, i + 5));
			}

			Utility.NewRandom().Shuffle((Span<(uint, ComputeTask<uint>)>)pairs);

			foreach ((uint expect, ComputeTask<uint> task) in pairs)
			{
				Assert.That(await task, Is.EqualTo(expect));
			}

			stage.Next((uint)pairs.Length + 5);
		}

		static async ComputeTask<uint> Construct(AsyncOperation operation, Stage stage, uint expect)
		{
			stage.Next(expect);
			uint result = stage.Current;

			await operation.Schedule(() =>
			{
				SpinWait wait = new SpinWait();

				while (!wait.NextSpinWillYield) wait.SpinOnce();
				for (int i = 0; i < 16; i++) wait.SpinOnce();
			});

			return result;
		}
	}

	void AssertCompleted(Operation operation)
	{
		Assert.That(operation.IsCompleted);
		Assert.That(operation.TotalProcedureCount, Is.EqualTo(operation.CompletedProcedureCount));
		Assert.That(operation.Progress, Is.EqualTo(1f).Roughly());
		Assert.That(operation.WorkerCount, Is.EqualTo(device.Population));
	}

	/// <summary>
	/// Be annoying
	/// </summary>
	static void PauseSeries(Device device)
	{
		for (int i = 0; i < PauseCount; i++)
		{
			device.Pause();
			Thread.Yield();
			device.Resume();
			Thread.Yield();
		}
	}

	class SleepOperation : Operation
	{
		SleepOperation(ImmutableArray<IWorker> workers, float multiplier) : base(workers, (uint)MathF.Round(workers.Length * multiplier)) { }

		const int SleepLength = 100;

		protected override void Execute(ref Procedure procedure, IWorker worker)
		{
			procedure.Begin(1);
			Thread.Sleep(SleepLength);
			procedure.Advance();
		}

		public readonly struct Factory : IOperationFactory
		{
			public Factory(float multiplier) => this.multiplier = multiplier;

			readonly float multiplier;

			public Operation CreateOperation(ImmutableArray<IWorker> workers) => new SleepOperation(workers, multiplier);
		}
	}

	class Stage
	{
		uint data;

		public uint Current => Volatile.Read(ref data);

		public void Next(uint expected)
		{
			uint incremented = Interlocked.Increment(ref data);
			Assert.That(incremented, Is.EqualTo(expected));
		}

		public void Skip() => Interlocked.Increment(ref data);
	}
}