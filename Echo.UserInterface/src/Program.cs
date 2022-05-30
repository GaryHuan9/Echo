using System;
using System.Diagnostics;
using System.Threading;
using ImGuiNET;
using static SDL2.SDL;

using var program = new Echo.UserInterface.Program();
return program.Launch();

namespace Echo.UserInterface
{
	sealed unsafe class Program : IDisposable
	{
		public Program()
		{
			application = new Application();

			if (SDL_Init(SDL_INIT_EVERYTHING) != 0) throw new Exception();

			const SDL_WindowFlags WindowFlags = SDL_WindowFlags.SDL_WINDOW_RESIZABLE |
												SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI |
												SDL_WindowFlags.SDL_WINDOW_MAXIMIZED;

			const SDL_RendererFlags RendererFlags = SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
													SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC;

			window = SDL_CreateWindow("Echo User Interface", 0, 0, 1920, 1080, WindowFlags);
			renderer = SDL_CreateRenderer(window, -1, RendererFlags);
		}

		readonly Application application;

		readonly IntPtr window;
		readonly IntPtr renderer;

		public TimeSpan UpdateDelay { get; set; } = TimeSpan.FromSeconds(1f / 60f);

		public int Launch()
		{
			if (renderer == IntPtr.Zero) return -1;

			var stopwatch = Stopwatch.StartNew();
			TimeSpan lastTime = TimeSpan.Zero;
			application.Initialize();

			ImGui.CreateContext();
			ImGui.StyleColorsDark();

			ImGuiDevice device = new ImGuiDevice(window, renderer);
			ImGuiDisplay display = new ImGuiDisplay(renderer);

			device.Init();
			display.Init();

			bool running = true;

			while (running)
			{
				//Setup for frame
				TimeSpan time = stopwatch.Elapsed;
				TimeSpan delta = time - lastTime;
				lastTime = time;

				while (SDL_PollEvent(out SDL_Event sdlEvent) != 0)
				{
					device.ProcessEvent(sdlEvent);

					switch (sdlEvent.type)
					{
						case SDL_EventType.SDL_QUIT:
						case SDL_EventType.SDL_WINDOWEVENT when sdlEvent.window.windowEvent == SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE && sdlEvent.window.windowID == SDL_GetWindowID(window):
						{
							running = false;
							break;
						}
					}
				}

				display.NewFrame();
				device.NewFrame((float)delta.TotalSeconds);
				ImGui.NewFrame();

				application.Update();

				ImGui.Render();
				SDL_SetRenderDrawColor(renderer, 0, 0, 0, byte.MaxValue);
				SDL_RenderClear(renderer);
				display.RenderDrawData(ImGui.GetDrawData());
				SDL_RenderPresent(renderer);

				//Sleep for delay
				var remain = time - stopwatch.Elapsed + UpdateDelay;
				if (remain > TimeSpan.Zero) Thread.Sleep(remain);
			}

			display.Shutdown();
			device.Shutdown();
			ImGui.DestroyContext();

			return 0;
		}

		public void Dispose()
		{
			// gpu?.Dispose();
			// renderer?.Dispose();
			// application?.Dispose();

			SDL_DestroyRenderer(renderer);
			SDL_DestroyWindow(window);
			SDL_Quit();
		}
	}
}