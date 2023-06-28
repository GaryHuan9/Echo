using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Echo.Core.Common.Packed;
using ImGuiNET;
using SDL2;

namespace Echo.UserInterface.Backend;

using static SDL;
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
		io = ImGui.GetIO();

		//Assign global names and flags
		AssignBackendNames();
		io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
		io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
		io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

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
		SDL_SetHint(SDL_HINT_RENDER_SCALE_QUALITY, "nearest"); //Can be nearest, linear, or best
	}

	public void Initialize()
	{
		//Create resources
		CreateMouseCursors();
		CreateFontTexture();
		CreateClipboardSetup();
	}

	readonly IntPtr window;
	readonly IntPtr renderer;
	ImGuiIOPtr io;

	IntPtr[] mouseCursors;
	IntPtr fontTexture;

	int mouseButtonDownCount;
	int pendingMouseLeaveFrame;

	bool disposed;

	static readonly SetClipboardTextFn SetClipboardText = (_, text) => SDL_SetClipboardText(text);
	static readonly GetClipboardTextFn GetClipboardText = _ => SDL_GetClipboardText();

	public void ProcessEvent(in SDL_Event sdlEvent)
	{
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
				ProcessMouseButtonEvent(sdlEvent.button);
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
				ProcessKeyboardEvent(sdlEvent.key);
				break;
			}
			case SDL_EventType.SDL_WINDOWEVENT:
			{
				ProcessWindowEvent(sdlEvent.window);
				break;
			}
		}
	}

	public void NewFrame(in TimeSpan deltaTime)
	{
		io.DeltaTime = (float)deltaTime.TotalSeconds;

		RefreshDisplaySize();
		UpdateMouseData();
		UpdateMouseCursor();
	}

	public void Render(ImDrawDataPtr data)
	{
		//Clear screen
		SDL_SetRenderDrawColor(renderer, 0, 0, 0, byte.MaxValue).ThrowOnError();
		SDL_RenderClear(renderer).ThrowOnError();

		//Setup clip information
		Vector2 scale = data.FramebufferScale;
		Float4 clipSize = Widen(scale * data.DisplaySize);
		if (clipSize.X <= 0f || clipSize.Y <= 0f) return;

		//Render
		SDL_RenderSetScale(renderer, scale.X, scale.Y).ThrowOnError();
		SDL_RenderSetViewport(renderer, IntPtr.Zero).ThrowOnError();
		SDL_RenderSetClipRect(renderer, IntPtr.Zero).ThrowOnError();

		Float4 clipOffset = Widen(data.DisplayPos);
		var lists = data.CmdListsRange;

		for (int i = 0; i < lists.Count; i++) ExecuteCommandList(lists[i], clipOffset, clipSize);

		SDL_RenderPresent(renderer);

		static Float4 Widen(Vector2 vector) => new Float4(vector.AsVector128()).XYXY;
	}

	public IntPtr CreateTexture(Int2 size, bool streaming, bool bigEndian = false)
	{
		int access = streaming ?
			(int)SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING :
			(int)SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC;
		uint format = bigEndian ? SDL_PIXELFORMAT_RGBA8888 : SDL_PIXELFORMAT_ABGR8888;

		IntPtr texture = SDL_CreateTexture(renderer, format, access, size.X, size.Y);
		if (texture == IntPtr.Zero) throw new BackendException("Failed to create texture.");
		SDL_SetTextureBlendMode(texture, SDL_BlendMode.SDL_BLENDMODE_BLEND).ThrowOnError();

		return texture;
	}

	public void DestroyTexture(ref IntPtr texture)
	{
		if (texture == IntPtr.Zero) return;

		SDL_DestroyTexture(texture);
		texture = IntPtr.Zero;
	}

	public void Dispose()
	{
		if (disposed) return;
		disposed = true;

		DestroyMouseCursors();
		DestroyFontTexture();
		DestroyClipboardSetup();
		io = null;
	}

	void AssignBackendNames()
	{
		SDL_GetRendererInfo(renderer, out SDL_RendererInfo info).ThrowOnError();

		var name = (Marshal.PtrToStringAnsi(info.name) ?? "unknown").ToUpper();
		var size = new Int2(info.max_texture_width, info.max_texture_height);

		io.NativePtr->BackendPlatformName = (byte*)Marshal.StringToHGlobalAnsi("SDL2 & Dear ImGui for C#");
		io.NativePtr->BackendRendererName = (byte*)Marshal.StringToHGlobalAnsi($"{name} {size.X}x{size.Y}");
	}

	void CreateMouseCursors()
	{
		mouseCursors = new IntPtr[(int)ImGuiMouseCursor.COUNT];

		mouseCursors[(int)ImGuiMouseCursor.Arrow] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_ARROW);
		mouseCursors[(int)ImGuiMouseCursor.TextInput] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_IBEAM);
		mouseCursors[(int)ImGuiMouseCursor.ResizeAll] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZEALL);
		mouseCursors[(int)ImGuiMouseCursor.ResizeNS] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENS);
		mouseCursors[(int)ImGuiMouseCursor.ResizeEW] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZEWE);
		mouseCursors[(int)ImGuiMouseCursor.ResizeNESW] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENESW);
		mouseCursors[(int)ImGuiMouseCursor.ResizeNWSE] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENWSE);
		mouseCursors[(int)ImGuiMouseCursor.Hand] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_HAND);
		mouseCursors[(int)ImGuiMouseCursor.NotAllowed] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_NO);
	}

	void DestroyMouseCursors()
	{
		if (mouseCursors == null) return;

		foreach (IntPtr cursor in mouseCursors)
		{
			if (cursor == IntPtr.Zero) continue;
			SDL_FreeCursor(cursor);
		}

		mouseCursors = null;
	}

	void CreateFontTexture()
	{
		io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height);
		fontTexture = CreateTexture(new Int2(width, height), false);

		SDL_UpdateTexture(fontTexture, IntPtr.Zero, pixels, width * sizeof(uint)).ThrowOnError();
		SDL_SetTextureBlendMode(fontTexture, SDL_BlendMode.SDL_BLENDMODE_BLEND).ThrowOnError();
		SDL_SetTextureScaleMode(fontTexture, SDL_ScaleMode.SDL_ScaleModeLinear).ThrowOnError();

		io.Fonts.SetTexID(fontTexture);
	}

	void DestroyFontTexture()
	{
		DestroyTexture(ref fontTexture);
		io.Fonts.SetTexID(IntPtr.Zero);
	}

	void CreateClipboardSetup()
	{
		io.SetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(SetClipboardText);
		io.GetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(GetClipboardText);
	}

	void DestroyClipboardSetup()
	{
		io.SetClipboardTextFn = IntPtr.Zero;
		io.GetClipboardTextFn = IntPtr.Zero;
	}

	void ProcessMouseButtonEvent(SDL_MouseButtonEvent mouseButtonEvent)
	{
		int mouseButton = (uint)mouseButtonEvent.button switch
		{
			SDL_BUTTON_LEFT   => 0,
			SDL_BUTTON_RIGHT  => 1,
			SDL_BUTTON_MIDDLE => 2,
			SDL_BUTTON_X1     => 3,
			SDL_BUTTON_X2     => 4,
			_                 => -1
		};

		if (mouseButton < 0) return;

		bool down = mouseButtonEvent.type == SDL_EventType.SDL_MOUSEBUTTONDOWN;
		io.AddMouseButtonEvent(mouseButton, down);
		mouseButtonDownCount += down ? 1 : -1;
	}

	void ProcessKeyboardEvent(in SDL_KeyboardEvent keyboardEvent)
	{
		ref readonly SDL_Keysym key = ref keyboardEvent.keysym;
		bool down = keyboardEvent.type == SDL_EventType.SDL_KEYDOWN;

		io.AddKeyEvent(ImGuiKey.ModCtrl, (key.mod & SDL_Keymod.KMOD_CTRL) != 0);
		io.AddKeyEvent(ImGuiKey.ModShift, (key.mod & SDL_Keymod.KMOD_SHIFT) != 0);
		io.AddKeyEvent(ImGuiKey.ModAlt, (key.mod & SDL_Keymod.KMOD_ALT) != 0);
		io.AddKeyEvent(ImGuiKey.ModSuper, (key.mod & SDL_Keymod.KMOD_GUI) != 0);

		io.AddKeyEvent(SDL_KeycodeToImGuiKey(key.sym), down);
	}

	void ProcessWindowEvent(in SDL_WindowEvent windowEvent)
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

	void RefreshDisplaySize()
	{
		SDL_GetWindowSize(window, out int width, out int height);
		var size = io.DisplaySize = new Vector2(width, height);

		SDL_GetRendererOutputSize(renderer, out int displayWidth, out int displayHeight).ThrowOnError();

		if ((SDL_GetWindowFlags(window) & (uint)SDL_WindowFlags.SDL_WINDOW_MINIMIZED) != 0) size = Vector2.Zero;
		if (size.X > 0f && size.Y > 0f) io.DisplayFramebufferScale = new Vector2(displayWidth, displayHeight) / size;
	}

	void UpdateMouseData()
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
			_ = SDL_GetGlobalMouseState(out int globalX, out int globalY);
			SDL_GetWindowPosition(window, out int windowX, out int windowY);

			io.AddMousePosEvent(globalX - windowX, globalY - windowY);
		}
	}

	void UpdateMouseCursor()
	{
		if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) != 0) return;

		ImGuiMouseCursor cursor = ImGui.GetMouseCursor();

		if (!io.MouseDrawCursor && cursor != ImGuiMouseCursor.None)
		{
			IntPtr mouseCursor = mouseCursors[(int)cursor];
			const int Fallback = (int)ImGuiMouseCursor.Arrow;

			if (mouseCursor == IntPtr.Zero) mouseCursor = mouseCursors[Fallback];

			SDL_SetCursor(mouseCursor);
			_ = SDL_ShowCursor((int)SDL_bool.SDL_TRUE);
		}
		else _ = SDL_ShowCursor((int)SDL_bool.SDL_FALSE);
	}

	void ExecuteCommandList(ImDrawListPtr list, Float4 clipOffset, Float4 clipSize)
	{
		ImPtrVector<ImDrawCmdPtr> buffer = list.CmdBuffer;
		var vertices = (ImDrawVert*)list.VtxBuffer.Data;
		var indices = (ushort*)list.IdxBuffer.Data;

		for (int i = 0; i < buffer.Size; i++)
		{
			ImDrawCmdPtr command = buffer[i];

			if (command.UserCallback == IntPtr.Zero)
			{
				var clipRect = new Float4(command.ClipRect.AsVector128()) - clipOffset;

				Float4 clipMin = clipRect.Max(Float4.Zero); //(minX, minY, ____, ____).Max(zero)
				Float4 clipMax = clipRect.Min(clipSize);    //(____, ____, maxX, maxY).Min(size)

				if (clipMax.Z <= clipMin.X || clipMax.W <= clipMin.Y) continue;

				clipMin = clipMin.XYXY; //(minX, minY, minX, minY)
				clipMax = clipMax.__ZW; //(0000, 0000, maxX, maxY)

				Float4 clip = (clipMax - clipMin).Absoluted;     //(-minX, -minY, maxX - minX, maxY - minY).Absoluted
				var rect = Sse2.ConvertToVector128Int32(clip.v); //(    X,     Y,       width,      height)
				ImDrawVert* vertex = vertices + command.VtxOffset;
				int stride = sizeof(ImDrawVert);

				SDL_RenderSetClipRect(renderer, (IntPtr)(&rect)).ThrowOnError();

				SDL_RenderGeometryRaw
				(
					renderer, command.TextureId,
					(float*)&vertex->pos, stride,
					(int*)&vertex->col, stride,
					(float*)&vertex->uv, stride,
					list.VtxBuffer.Size - (int)command.VtxOffset,
					(IntPtr)(indices + command.IdxOffset),
					(int)command.ElemCount, sizeof(ushort)
				).ThrowOnError();
			}
			else
			{
				var callback = Marshal.GetDelegateForFunctionPointer<ImDrawCallback>(command.UserCallback);
				callback(new IntPtr(list), new IntPtr(command)); //Perform user callback, not really used
			}
		}
	}

	delegate void SetClipboardTextFn(IntPtr _, string text);
	delegate string GetClipboardTextFn(IntPtr _);
	delegate void ImDrawCallback(IntPtr list, IntPtr cmd);
}