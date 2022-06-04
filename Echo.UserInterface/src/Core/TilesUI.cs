using Echo.Common.Compute;
using Echo.Core.Evaluation.Operations;

namespace Echo.UserInterface.Core;

public class TilesUI : AreaUI
{
	public TilesUI() : base("Tiles") { }

	protected override void Draw()
	{
		if (Device.Instance?.StartedOperation is not TiledEvaluationOperation operation) return;

		// if (ImGui.BeginTable("Main", operation.TileCounts.X))
		// {
		// 	foreach (Int2 position in operation.TilePositions)
		// 	{
		// 		ImGui.tableset
		// 	}
		// }
	}
}