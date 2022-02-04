using System;
using System.Diagnostics;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Threads;
using EchoRenderer.UserInterface.Core;
using EchoRenderer.UserInterface.Core.Areas;
using EchoRenderer.UserInterface.Interface;
using SFML.Graphics;
using SFML.Window;

namespace EchoRenderer.UserInterface;

public class Application : RenderWindow, IDisposable
{
	Application() : base(VideoMode.DesktopMode, nameof(EchoRenderer), Styles.Default, new ContextSettings(0, 0) {SRgbCapable = true})
	{
		Closed += (_, _) => Close();
		stopwatch = Stopwatch.StartNew();

		root = new RootUI(this);

		root.Add
		(
			new AreaUI
				{
					transform = {BottomMargin = Theme.Current.LayoutHeight}
				}.Add(new HierarchyUI())
				 .Add(new SceneViewUI())
				 .Add(new InspectorUI())
				 .Add(new ProfileUI())
		);

		root.Add
		(
			new AutoLayoutAreaUI
				{
					transform =
					{
						TopMargin = -Theme.Current.LayoutHeight,
						TopPercent = 1f
					},
					Horizontal = true,
					Margins = false
				}.Add(new ApplicationStatusUI())
				 .Add(new ExitButtonUI())
		);
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

		AreaUI.Transform.IncrementFrame();

		root.Draw();
	}

	void UpdateTime()
	{
		double time = stopwatch.Elapsed.TotalSeconds;

		DeltaTime = time - TotalTime;
		TotalTime = time;
	}

	void IDisposable.Dispose()
	{
		root.Dispose();
		base.Dispose();

		GC.SuppressFinalize(this);
	}

	static void Main()
	{
		ThreadHelper.MainThread = Thread.CurrentThread;
		RandomHelper.Seed = 47;

		using Application application = new Application();

		application.Start();

		while (application.IsOpen)
		{
			application.DispatchEvents();
			application.Clear();

			application.Update();
			application.Display();
		}
	}
}