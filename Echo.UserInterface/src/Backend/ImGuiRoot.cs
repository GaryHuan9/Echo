using System;
using System.Diagnostics;
using System.Threading;
using ImGuiNET;
using static SDL2.SDL;

namespace Echo.UserInterface.Backend;

public sealed class ImGuiRoot<T> : IDisposable where T : IApplication, new()
{
	public ImGuiRoot()
	{
		application = new T();

		SDL_Init(SDL_INIT_EVERYTHING).ThrowOnError();

		const SDL_WindowFlags WindowFlags = SDL_WindowFlags.SDL_WINDOW_RESIZABLE |
											SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI |
											SDL_WindowFlags.SDL_WINDOW_MAXIMIZED;

		const SDL_RendererFlags RendererFlags = SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
												SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC;

		window = SDL_CreateWindow(application.Label, 0, 0, 1920, 1080, WindowFlags);
		renderer = SDL_CreateRenderer(window, -1, RendererFlags);
		if (renderer == IntPtr.Zero) throw new BackendException();
	}

	readonly T application;
	readonly IntPtr window;
	readonly IntPtr renderer;

	public void Launch()
	{
		var stopwatch = Stopwatch.StartNew();

		ImGui.CreateContext();

		application.Initialize();
		MainLoop(stopwatch);

		ImGui.DestroyContext();
	}

	public void Dispose()
	{
		application?.Dispose();

		SDL_DestroyWindow(window);
		SDL_DestroyRenderer(renderer);
		SDL_Quit();
	}

	void MainLoop(Stopwatch stopwatch)
	{
		using var device = new ImGuiDevice(window, renderer);

		TimeSpan lastTime = TimeSpan.Zero;

		while (!application.RequestTermination)
		{
			//Setup for frame
			TimeSpan time = stopwatch.Elapsed;
			TimeSpan delta = time - lastTime;
			lastTime = time;

			//Handle events
			while (SDL_PollEvent(out SDL_Event sdlEvent) != 0)
			{
				if (IsQuitEvent(sdlEvent)) return;
				device.ProcessEvent(sdlEvent);
			}

			//Begin frame
			device.NewFrame(delta);
			ImGui.NewFrame();
			application.Update();

			//Render frame
			ImGui.EndFrame();
			ImGui.Render();
			device.Render(ImGui.GetDrawData());

			//Sleep for delay
			var remain = time - stopwatch.Elapsed + application.UpdateDelay;
			if (remain > TimeSpan.Zero) Thread.Sleep(remain);
		}
	}

	bool IsQuitEvent(in SDL_Event sdlEvent) => sdlEvent.type switch
	{
		SDL_EventType.SDL_QUIT => true,
		SDL_EventType.SDL_WINDOWEVENT => sdlEvent.window.windowEvent == SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE &&
										 sdlEvent.window.windowID == SDL_GetWindowID(window),
		_ => false
	};
}