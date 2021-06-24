using System.Diagnostics;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Threads;
using EchoRenderer.UI.Core;
using EchoRenderer.UI.Core.Areas;
using EchoRenderer.UI.Interface;
using SFML.Graphics;
using SFML.Window;

namespace EchoRenderer.UI
{
	public class Application : RenderWindow
	{
		public Application() : base(VideoMode.DesktopMode, nameof(EchoRenderer))
		{
			Closed += (_, _) => Close();

			stopwatch = Stopwatch.StartNew();

			//Create UI
			root = new RootUI(this)
				   {
					   new AreaUI
						   {
							   transform = {BottomMargin = 20f}
						   }.Add(new HierarchyUI())
							.Add(new SceneViewUI())
							.Add(new InspectorUI()),
					   new ApplicationStatusUI()
				   };
		}

		public double TotalTime { get; private set; }
		public double DeltaTime { get; private set; }

		readonly RootUI root;
		readonly Stopwatch stopwatch;

		public void Start()
		{
			UpdateTime();

			SetVerticalSyncEnabled(true);
			root.Resize(Size.Cast());
		}

		public void Update()
		{
			UpdateTime();

			root.Update();
			root.Draw(this);
		}

		void UpdateTime()
		{
			double time = stopwatch.Elapsed.TotalSeconds;

			DeltaTime = time - TotalTime;
			TotalTime = time;
		}

		static void Main()
		{
			ThreadHelper.MainThread = Thread.CurrentThread;
			RandomHelper.Seed = 47;

			Application application = new Application();

			application.Start();

			while (application.IsOpen)
			{
				application.DispatchEvents();
				application.Clear();

				application.Update();
				application.Display();
			}

			application.root.Dispose();
		}
	}
}