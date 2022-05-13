using CodeHelpers.Packed;
using Echo.Core.Evaluation.Operations;
using Echo.Terminal.Core.Display;
using Echo.Terminal.Core.Interface;

namespace Echo.Terminal.Application.Report;

public class TileReportTI : AreaTI
{
	public TiledEvaluationOperation Operation { get; set; }

	protected override void Paint(in Painter painter)
	{
		TiledEvaluationOperation operation = Operation;

		Int2 cursor = Int2.Zero;

		painter.FillAll(cursor);
	}
}