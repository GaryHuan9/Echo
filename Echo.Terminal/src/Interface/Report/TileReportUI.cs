using Echo.Core.Evaluation.Operations;
using Echo.Terminal.Core;

namespace Echo.Terminal.Interface.Report;

public class TileReportUI : AreaUI
{
	public TiledEvaluationOperation Operation { get; set; }

	public override void Update()
	{
		TiledEvaluationOperation operation = Operation;


	}
}