using System;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace Echo.UserInterface.Backend;

//
using static SDL2.SDL;
using static Native;

/// <summary>
/// Implementation reference:
/// https://github.com/ocornut/imgui/blob/e23c5edd5fdef85ea0f5418b1368adb94bf86230/backends/imgui_impl_sdl.cpp
/// https://github.com/ocornut/imgui/blob/e23c5edd5fdef85ea0f5418b1368adb94bf86230/backends/imgui_impl_sdlrenderer.cpp
/// </summary>
public sealed unsafe class ImGuiDevice : IDisposable
{
	public ImGuiDevice(IntPtr window, IntPtr renderer)
	{
		this.window = window;
		this.renderer = renderer;

		//Setup flags
		var io = ImGui.GetIO();

		io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
		io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
		io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

		//Create resources
		mouseCursors = CreateMouseCursors();
		fontTexture = CreateFontTexture(renderer);
		CreateClipboardSetup();

		//Link viewport data
		SDL_SysWMinfo info = default;
		SDL_VERSION(out info.version);

		if (SDL_GetWindowWMInfo(window, ref info) == SDL_bool.SDL_TRUE)
		{
			ImGuiViewportPtr viewport = ImGui.GetMainViewport();
			viewport.PlatformHandleRaw = info.info.win.window;
		}

		//Setup SDL hint
		SDL_SetHint(SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
	}

	readonly IntPtr window;
	readonly IntPtr renderer;

	IntPtr[] mouseCursors;
	IntPtr fontTexture;

	int mouseButtonDownCount;
	int pendingMouseLeaveFrame;

	bool disposed;

	static readonly SetClipboardTextFn SetClipboardText = (_, text) => SDL_SetClipboardText(text);
	static readonly GetClipboardTextFn GetClipboardText = _ => SDL_GetClipboardText();

	public void ProcessEvent(in SDL_Event sdlEvent)
	{
		var io = ImGui.GetIO();

		switch (sdlEvent.type)
		{
			case SDL_EventType.SDL_MOUSEMOTION:
			{
				io.AddMousePosEvent(sdlEvent.motion.x, sdlEvent.motion.y);
				break;
			}
			case SDL_EventType.SDL_MOUSEWHEEL:
			{
				io.AddMouseWheelEvent(sdlEvent.wheel.preciseX, sdlEvent.wheel.preciseY);
				break;
			}
			case SDL_EventType.SDL_MOUSEBUTTONDOWN:
			case SDL_EventType.SDL_MOUSEBUTTONUP:
			{
				ProcessMouseButtonEvent(io, sdlEvent.button);
				break;
			}
			case SDL_EventType.SDL_TEXTINPUT:
			{
				fixed (byte* ptr = sdlEvent.text.text) ImGuiNative.ImGuiIO_AddInputCharactersUTF8(io.NativePtr, ptr);
				break;
			}
			case SDL_EventType.SDL_KEYDOWN:
			case SDL_EventType.SDL_KEYUP:
			{
				ProcessKeyboardEvent(io, sdlEvent.key);
				break;
			}
			case SDL_EventType.SDL_WINDOWEVENT:
			{
				ProcessWindowEvent(io, sdlEvent.window);
				break;
			}
		}
	}

	public void NewFrame(in TimeSpan deltaTime)
	{
		var io = ImGui.GetIO();
		io.DeltaTime = (float)deltaTime.TotalSeconds;

		RefreshDisplaySize(io);
		UpdateMouseData(io);
		UpdateMouseCursor(io);
	}

	public void Render(ImDrawDataPtr data)
	{
		//Clear screen
		SDL_SetRenderDrawColor(renderer, 0, 0, 0, byte.MaxValue).ThrowOnError();
		SDL_RenderClear(renderer).ThrowOnError();

		float renderScaleX = data.FramebufferScale.X;
		float renderScaleY = data.FramebufferScale.Y;

		int width = (int)(data.DisplaySize.X * renderScaleX);
		int height = (int)(data.DisplaySize.Y * renderScaleY);

		if (width == 0 || height == 0) return;

		bool old_clipEnabled = SDL_RenderIsClipEnabled(renderer) == SDL_bool.SDL_TRUE;
		SDL_RenderGetViewport(renderer, out SDL_Rect old_viewport).ThrowOnError();
		SDL_RenderGetClipRect(renderer, out SDL_Rect old_clipRect);

		Vector2 clipOff = data.DisplayPos;
		Vector2 clipScale = new Vector2(renderScaleY, renderScaleY);

		SetupRenderState();

		for (int i = 0; i < data.CmdListsCount; i++)
		{
			ImDrawListPtr cmdList = data.CmdListsRange[i];
			var vertices = (ImDrawVert*)cmdList.VtxBuffer.Data;
			var indices = (ushort*)cmdList.IdxBuffer.Data;

			for (int j = 0; j < cmdList.CmdBuffer.Size; j++)
			{
				ImDrawCmdPtr cmd = cmdList.CmdBuffer[j];

				if (cmd.UserCallback == IntPtr.Zero)
				{
					Vector2 clipMin = new Vector2((cmd.ClipRect.X - clipOff.X) * clipScale.X, (cmd.ClipRect.Y - clipOff.Y) * clipScale.Y);
					Vector2 clipMax = new Vector2((cmd.ClipRect.Z - clipOff.X) * clipScale.X, (cmd.ClipRect.W - clipOff.Y) * clipScale.Y);

					clipMin = Vector2.Max(clipMin, Vector2.Zero);
					clipMax = Vector2.Min(clipMax, new Vector2(width, height));

					if (clipMax.X <= clipMin.X || clipMax.Y <= clipMin.Y) continue;

					SDL_Rect r = new SDL_Rect { x = (int)clipMin.X, y = (int)clipMin.Y, w = (int)(clipMax.X - clipMin.X), h = (int)(clipMax.Y - clipMin.Y) };
					SDL_RenderSetClipRect(renderer, (IntPtr)(&r));

					ImDrawVert* vertex = vertices + cmd.VtxOffset;

					float* xy = (float*)&vertex->pos;
					float* uv = (float*)&vertex->uv;
					int* color = (int*)&vertex->col;

					IntPtr texture = cmd.GetTexID();
					int stride = sizeof(ImDrawVert);

					SDL_RenderGeometryRaw(renderer, texture, xy, stride, color, stride, uv, stride, cmdList.VtxBuffer.Size - (int)cmd.VtxOffset, (IntPtr)(indices + cmd.IdxOffset), (int)cmd.ElemCount, sizeof(ushort));
				}
				else throw new NotSupportedException("???"); //How to use cmd.UserCallback?
			}
		}

		SDL_RenderSetViewport(renderer, (IntPtr)(&old_viewport)).ThrowOnError();
		SDL_RenderSetClipRect(renderer, old_clipEnabled ? (IntPtr)(&old_clipRect) : IntPtr.Zero).ThrowOnError();

		SDL_RenderPresent(renderer);
	}

	public void Dispose()
	{
		if (disposed) return;
		disposed = true;

		DestroyMouseCursors(ref mouseCursors);
		DestroyFontTexture(ref fontTexture);
		DestroyClipboardSetup();
	}

	void ProcessMouseButtonEvent(ImGuiIOPtr io, SDL_MouseButtonEvent mouseButtonEvent)
	{
		int mouseButton = (uint)mouseButtonEvent.button switch
		{
			SDL_BUTTON_LEFT => 0,
			SDL_BUTTON_RIGHT => 1,
			SDL_BUTTON_MIDDLE => 2,
			SDL_BUTTON_X1 => 3,
			SDL_BUTTON_X2 => 4,
			_ => -1
		};

		if (mouseButton < 0) return;

		bool down = mouseButtonEvent.type == SDL_EventType.SDL_MOUSEBUTTONDOWN;
		io.AddMouseButtonEvent(mouseButton, down);
		mouseButtonDownCount += down ? 1 : -1;
	}

	void ProcessKeyboardEvent(ImGuiIOPtr io, in SDL_KeyboardEvent keyboardEvent)
	{
		ref readonly SDL_Keysym key = ref keyboardEvent.keysym;
		bool down = keyboardEvent.type == SDL_EventType.SDL_KEYDOWN;

		io.AddKeyEvent(ImGuiKey.ModCtrl, (key.mod & SDL_Keymod.KMOD_CTRL) != 0);
		io.AddKeyEvent(ImGuiKey.ModShift, (key.mod & SDL_Keymod.KMOD_SHIFT) != 0);
		io.AddKeyEvent(ImGuiKey.ModAlt, (key.mod & SDL_Keymod.KMOD_ALT) != 0);
		io.AddKeyEvent(ImGuiKey.ModSuper, (key.mod & SDL_Keymod.KMOD_GUI) != 0);

		io.AddKeyEvent(SDL_KeycodeToImGuiKey(key.sym), down);
	}

	void ProcessWindowEvent(ImGuiIOPtr io, in SDL_WindowEvent windowEvent)
	{
		switch (windowEvent.windowEvent)
		{
			case SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
			{
				pendingMouseLeaveFrame = 0;
				break;
			}
			case SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
			{
				pendingMouseLeaveFrame = ImGui.GetFrameCount() + 1;
				break;
			}
			case SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
			{
				io.AddFocusEvent(true);
				break;
			}
			case SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
			{
				io.AddFocusEvent(false);
				break;
			}
		}
	}

	void RefreshDisplaySize(ImGuiIOPtr io)
	{
		SDL_GetWindowSize(window, out int width, out int height);
		var size = io.DisplaySize = new Vector2(width, height);

		SDL_GetRendererOutputSize(renderer, out int displayWidth, out int displayHeight).ThrowOnError();

		if ((SDL_GetWindowFlags(window) & (uint)SDL_WindowFlags.SDL_WINDOW_MINIMIZED) != 0) size = Vector2.Zero;
		if (size.X > 0f && size.Y > 0f) io.DisplayFramebufferScale = new Vector2(displayWidth, displayHeight) / size;
	}

	void UpdateMouseData(ImGuiIOPtr io)
	{
		if (pendingMouseLeaveFrame >= ImGui.GetFrameCount() && mouseButtonDownCount == 0)
		{
			io.AddMousePosEvent(float.MinValue, float.MinValue);
			pendingMouseLeaveFrame = 0;
		}

		SDL_CaptureMouse(mouseButtonDownCount != 0 ? SDL_bool.SDL_TRUE : SDL_bool.SDL_FALSE).ThrowOnError();

		if (window != SDL_GetKeyboardFocus()) return;
		if (io.WantSetMousePos) SDL_WarpMouseInWindow(window, (int)io.MousePos.X, (int)io.MousePos.Y);

		if (mouseButtonDownCount == 0)
		{
			SDL_GetGlobalMouseState(out int mouseGlobalX, out int mouseGlobalY).ThrowOnError();
			SDL_GetWindowPosition(window, out int windowX, out int windowY);

			io.AddMousePosEvent(mouseGlobalX - windowX, mouseGlobalY - windowY);
		}
	}

	void UpdateMouseCursor(ImGuiIOPtr io)
	{
		if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) != 0) return;

		ImGuiMouseCursor cursor = ImGui.GetMouseCursor();

		if (!io.MouseDrawCursor && cursor != ImGuiMouseCursor.None)
		{
			IntPtr mouseCursor = mouseCursors[(int)cursor];
			const int Fallback = (int)ImGuiMouseCursor.Arrow;

			if (mouseCursor == IntPtr.Zero) mouseCursor = mouseCursors[Fallback];

			SDL_SetCursor(mouseCursor);
			SDL_ShowCursor((int)SDL_bool.SDL_TRUE).ThrowOnError();
		}
		else SDL_ShowCursor((int)SDL_bool.SDL_FALSE).ThrowOnError();
	}

	void SetupRenderState()
	{
		SDL_RenderSetViewport(renderer, IntPtr.Zero).ThrowOnError();
		SDL_RenderSetClipRect(renderer, IntPtr.Zero).ThrowOnError();
	}

	static IntPtr[] CreateMouseCursors()
	{
		var cursors = new IntPtr[(int)ImGuiMouseCursor.COUNT];

		cursors[(int)ImGuiMouseCursor.Arrow] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_ARROW);
		cursors[(int)ImGuiMouseCursor.TextInput] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_IBEAM);
		cursors[(int)ImGuiMouseCursor.ResizeAll] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZEALL);
		cursors[(int)ImGuiMouseCursor.ResizeNS] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENS);
		cursors[(int)ImGuiMouseCursor.ResizeEW] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZEWE);
		cursors[(int)ImGuiMouseCursor.ResizeNESW] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENESW);
		cursors[(int)ImGuiMouseCursor.ResizeNWSE] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENWSE);
		cursors[(int)ImGuiMouseCursor.Hand] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_HAND);
		cursors[(int)ImGuiMouseCursor.NotAllowed] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_NO);

		return cursors;
	}

	static void DestroyMouseCursors(ref IntPtr[] cursors)
	{
		if (cursors == null) return;

		foreach (IntPtr cursor in cursors)
		{
			if (cursor == IntPtr.Zero) continue;
			SDL_FreeCursor(cursor);
		}

		cursors = null;
	}

	static IntPtr CreateFontTexture(IntPtr renderer)
	{
		var io = ImGui.GetIO();

		io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height);

		IntPtr texture = SDL_CreateTexture
		(
			renderer, SDL_PIXELFORMAT_ABGR8888,
			(int)SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC,
			width, height
		);

		if (texture == IntPtr.Zero) throw new BackendException();

		SDL_UpdateTexture(texture, IntPtr.Zero, pixels, 4 * width).ThrowOnError();
		SDL_SetTextureBlendMode(texture, SDL_BlendMode.SDL_BLENDMODE_BLEND).ThrowOnError();
		SDL_SetTextureScaleMode(texture, SDL_ScaleMode.SDL_ScaleModeLinear).ThrowOnError();

		io.Fonts.SetTexID(texture);
		return texture;
	}

	static void DestroyFontTexture(ref IntPtr texture)
	{
		if (texture == IntPtr.Zero) return;

		var io = ImGui.GetIO();

		io.Fonts.SetTexID(IntPtr.Zero);
		SDL_DestroyTexture(texture);
		texture = IntPtr.Zero;
	}

	static void CreateClipboardSetup()
	{
		var io = ImGui.GetIO();

		io.SetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(SetClipboardText);
		io.GetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(GetClipboardText);
	}

	static void DestroyClipboardSetup()
	{
		var io = ImGui.GetIO();

		io.SetClipboardTextFn = IntPtr.Zero;
		io.GetClipboardTextFn = IntPtr.Zero;
	}

	delegate void SetClipboardTextFn(IntPtr _, string text);
	delegate string GetClipboardTextFn(IntPtr _);
}