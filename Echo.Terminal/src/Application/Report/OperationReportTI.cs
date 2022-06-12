using System;
using Echo.Common;
using Echo.Common.Compute;
using Echo.Common.Compute.Statistics;
using Echo.Common.Memory;
using Echo.Terminal.Core.Display;

namespace Echo.Terminal.Application.Report;

public class OperationReportTI : ReportTI
{
	EventRow[] rows = Array.Empty<EventRow>();

	protected override void Paint(in Canvas canvas, Brush brush, Device device)
	{
		Operation operation = device.LatestOperation;

		if (operation == null)
		{
			canvas.WriteLine(ref brush, "No dispatched operation found.");
			canvas.WriteLine(ref brush, "Start an operation to begin.");
		}
		else
		{
			canvas.WriteLine(ref brush, $"Progress {operation.Progress:P2}");
			canvas.WriteLine(ref brush, $"Total Time {operation.TotalTime:hh\\:mm\\:ss}");
			canvas.WriteLine(ref brush, $"Time {operation.Time:hh\\:mm\\:ss}");

			Utility.EnsureCapacity(ref rows, operation.EventRowCount);

			SpanFill<EventRow> fill = rows;
			operation.FillEventRows(ref fill);

			foreach (EventRow row in fill.Filled) canvas.WriteLine(ref brush, $"{row.Label} {row.Count}");
		}

		//Fill remaining space
		canvas.FillAll(ref brush);
	}
}