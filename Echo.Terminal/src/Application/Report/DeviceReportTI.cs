using System;
using System.Numerics;
using CodeHelpers.Diagnostics;
using Echo.Common.Compute;
using Echo.Common.Memory;
using Echo.Terminal.Core.Display;
using Echo.Terminal.Core.Interface;

namespace Echo.Terminal.Application.Report;

public class DeviceReportTI : AreaTI
{
	static readonly string[] workerStatusLabels = Enum.GetNames<Worker.State>();

	protected override void Paint(in Canvas canvas)
	{
		var device = Device.Instance;
		var brush = new Brush();

		if (device == null)
		{
			canvas.WriteLine(ref brush, "No compute device found.");
			canvas.WriteLine(ref brush, "Attach a device to begin.");
		}
		else
		{
			//Write device status
			int population = device.Population;

			canvas.WriteLine(ref brush, $"CPU compute device {(device.IsIdle ? "idle" : "running")} with population {population}");
			canvas.FillLine(ref brush);

			//Write worker statuses
			Span<Worker.State> statuses = stackalloc Worker.State[population];
			SpanFill<Worker.State> fill = statuses.AsFill();

			device.FillStatuses(ref fill);
			Assert.IsTrue(fill.IsFull);

			for (int i = 0; i < population; i++)
			{
				//Write worker number and label
				Worker.State status = fill.Filled[i];
				string label = GetWorkerStatusLabel(status);
				canvas.WriteLine(ref brush, $"Worker 0x{i:X2} {label}");
			}
		}

		canvas.FillAll(ref brush);
	}

	static string GetWorkerStatusLabel(Worker.State status)
	{
		//NOTE: this implementation is a bit sus, but it works because the enum is a flag

		uint integer = (uint)status;
		Assert.AreEqual(BitOperations.PopCount(integer), 1);
		int index = BitOperations.LeadingZeroCount(integer);
		return workerStatusLabels[31 - index];
	}
}