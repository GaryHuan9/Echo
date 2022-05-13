using CodeHelpers.Packed;
using Echo.Core.Compute;
using Echo.Core.Evaluation.Evaluators;
using Echo.Core.Evaluation.Operations;
using Echo.Core.Scenic.Examples;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Grid;
using Echo.Terminal.Application;
using Echo.Terminal.Application.Report;
using Echo.Terminal.Core.Interface;

namespace Echo.Terminal;

public class EchoTI : RootTI
{
	public EchoTI()
	{
		tileReport = new TileReportTI();
		deviceReport = new DeviceReportTI();

		Child0 = new CommandTI();
		Child1 = new BisectionTI
		{
			Horizontal = true,
			Balance = 0.48f,

			Child0 = tileReport,
			Child1 = new BisectionTI
			{
				Horizontal = true,
				Balance = 0.55f,

				Child1 = deviceReport
			}
		};

		Balance = 0.16f;

		deviceReport.Device = new Device();

		var scene = new SingleBunny();

		var prepareProfile = new ScenePrepareProfile();

		var evaluationProfile = new TiledEvaluationProfile
		{
			Scene = new PreparedScene(scene, prepareProfile),
			Evaluator = new PathTracedEvaluator(),
			Buffer = new RenderBuffer(new Int2(960, 540))
		};

		var operation = new TiledEvaluationOperation
		{
			Profile = evaluationProfile
		};

		deviceReport.Device.Dispatch(operation);
	}

	readonly TileReportTI tileReport;
	readonly DeviceReportTI deviceReport;
}