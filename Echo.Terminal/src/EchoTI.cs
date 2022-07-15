using CodeHelpers.Packed;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Common.Compute;
using Echo.Core.Evaluation.Evaluators;
using Echo.Core.Evaluation.Operation;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Scenic.Examples;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Evaluation;
using Echo.Terminal.Application;
using Echo.Terminal.Application.Report;
using Echo.Terminal.Core.Interface;

namespace Echo.Terminal;

public class EchoTI : RootTI
{
	public EchoTI()
	{
		Child0 = new CommandTI();
		Child1 = new BisectionTI
		{
			Horizontal = true,
			Balance = 0.48f,

			Child0 = new TileReportTI(),
			Child1 = new BisectionTI
			{
				Horizontal = true,
				Balance = 0.55f,

				Child0 = new OperationReportTI(),
				Child1 = new DeviceReportTI()
			}
		};

		Balance = 0.16f;

		device = Device.Create();
	}

	readonly Device device;

	public override void ProcessArguments(string[] arguments)
	{
		base.ProcessArguments(arguments);

		var scene = new SingleBunny();
		var preparer = new ScenePreparer(scene);

		var evaluationProfile = new EvaluationProfile
		{
			Evaluator = new PathTracedEvaluator(),
			Distribution = new StratifiedDistribution { Extend = 16 },
			Buffer = new RenderBuffer(new Int2(960, 540)),
			Pattern = new SpiralPattern(),
			MinEpoch = 1,
			MaxEpoch = 20
		};

		var operation = new EvaluationOperation.Factory
		{
			NextScene = preparer.Prepare(),
			NextProfile = evaluationProfile
		};

		device.Dispatch(operation);
	}

	// public override void Update(in Moment moment)
	// {
	// 	base.Update(in moment);
	//
	// 	while (Console.KeyAvailable)
	// 	{
	// 		ConsoleKeyInfo key = Console.ReadKey(true);
	// 		if (key.KeyChar == 'p') device.Pause();
	// 		if (key.KeyChar == 'r') device.Resume();
	// 	}
	// }

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (disposing) device?.Dispose();
	}
}