using System;
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
		Operation operation = device.StartedOperation;

		if (operation == null)
		{
			canvas.WriteLine(ref brush, "No dispatched operation found.");
			canvas.WriteLine(ref brush, "Start an operation to begin.");
		}
		else
		{
			if (rows.Length < operation.EventCount) rows = new EventRow[operation.EventCount];

			SpanFill<EventRow> fill = rows.AsFill();
			operation.FillEventRows(ref fill);

			foreach (EventRow row in fill.Filled) canvas.WriteLine(ref brush, $"{row.Label} {row.Count}");
		}

		//Fill remaining space
		canvas.FillAll(ref brush);
	}
}