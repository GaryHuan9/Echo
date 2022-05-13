using System;
using System.Numerics;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using Echo.Common.Memory;
using Echo.Core.Compute;
using Echo.Terminal.Core.Display;
using Echo.Terminal.Core.Interface;

namespace Echo.Terminal.Application.Report;

public class DeviceReportTI : AreaTI
{
	public Device Device { get; set; }

	static readonly string[] workerStatusLabels = Enum.GetNames<Worker.State>();

	protected override void Paint(in Painter painter)
	{
		Int2 cursor = Int2.Zero;
		Device device = Device;

		if (device == null)
		{
			cursor = painter.WriteLine(cursor, "No compute device found...");
			cursor = painter.WriteLine(cursor, "Attach a device to begin.");
		}
		else
		{
			//Write device status
			int population = device.Population;

			const string IdleLabel = "CPU compute device idle with population ";
			const string RunningLabel = "CPU compute device running with population ";

			cursor = painter.Write(cursor, device.IsIdle ? IdleLabel : RunningLabel);
			cursor = painter.WriteLine(cursor, population);
			cursor = painter.FillLine(cursor);

			//Write worker statuses
			Span<Worker.State> statuses = stackalloc Worker.State[population];
			SpanFill<Worker.State> fill = statuses.AsFill();

			device.FillStatuses(ref fill);
			Assert.IsTrue(fill.IsFull);

			for (int i = 0; i < fill.Filled.Length; i++)
			{
				//Write worker number
				Worker.State status = fill.Filled[i];
				cursor = painter.Write(cursor, "Worker (");
				cursor = painter.Write(cursor, i);
				cursor = painter.Write(cursor, ") ");

				//Find and write label (this implementation is a bit sus)
				int index = 31 - BitOperations.LeadingZeroCount((uint)status);
				cursor = painter.WriteLine(cursor, workerStatusLabels[index]);
			}
		}

		painter.FillAll(cursor);
	}
}