using Echo.Core.Evaluation.Operations;
using Echo.Terminal.Core;

namespace Echo.Terminal.Interface.Report;

public class TileReportUI : AreaUI
{
	public TiledEvaluationOperation Operation { get; set; }

	protected override void Draw(in Domain.Drawer drawer)
	{
		TiledEvaluationOperation operation = Operation;
	}
}