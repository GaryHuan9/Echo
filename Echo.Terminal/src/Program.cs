using System;
using System.Threading;
using CodeHelpers.Packed;
using Echo.Core.Compute;
using Echo.Core.Evaluation.Evaluators;
using Echo.Core.Evaluation.Operations;
using Echo.Core.Scenic.Examples;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Grid;

namespace Echo.Terminal;

public class Program
{
	public static void Main()
	{
		using var terminal = new Terminal<EchoTerminalUI>();

		Console.ReadKey();
		return;

		Console.WriteLine
		(
			@"
        ..      .
     x88f` `..x88. .>             .uef^""
   :8888   xf`*8888%            :d88E              u.
  :8888f .888  `""`          .   `888E        ...ue888b
  88888' X8888. >""8x   .udR88N   888E .z8k   888R Y888r
  88888  ?88888< 888> <888'888k  888E~?888L  888R I888>
  88888   ""88888 ""8%  9888 'Y""   888E  888E  888R I888>
  88888 '  `8888>     9888       888E  888E  888R I888>
  `8888> %  X88!      9888       888E  888E u8888cJ888
   `888X  `~""""`   :   ?8888u../  888E  888E  ""*888*P""
     ""88k.      .~     ""8888P'  m888N= 888>    'Y""
       `""""*==~~`         ""P'     `Y""   888
                                      J88""
                                      @%
                                    :"""
		);

		using Device device = new Device();

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

		device.Dispatch(operation);
		// device.AwaitIdle();

		bool paused = false;

		while (!device.IsIdle)
		{
			Thread.Sleep(10);

			if (Console.KeyAvailable)
			{
				Console.ReadKey(true);
				Console.WriteLine("Keyed.");
				// device.Abort();
				// device.AwaitIdle();

				if (paused) device.Resume();
				else
				{
					device.Pause();
					evaluationProfile.Buffer.Save("render.png");
				}

				paused = !paused;
			}
		}

		Console.WriteLine("Done.");
		evaluationProfile.Buffer.Save("render.png");

		Console.ReadKey();
	}
}