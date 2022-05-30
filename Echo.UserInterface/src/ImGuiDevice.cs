using System;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using SDL2;
using static SDL2.SDL;

namespace Echo.UserInterface;

public unsafe class ImGuiDevice
{
	public ImGuiDevice(IntPtr window, IntPtr renderer)
	{
		this.window = window;
		this.renderer = renderer;

		CreateCursor(ImGuiMouseCursor.Arrow, SDL_SystemCursor.SDL_SYSTEM_CURSOR_ARROW);
		CreateCursor(ImGuiMouseCursor.TextInput, SDL_SystemCursor.SDL_SYSTEM_CURSOR_IBEAM);
		CreateCursor(ImGuiMouseCursor.ResizeAll, SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZEALL);
		CreateCursor(ImGuiMouseCursor.ResizeNS, SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENS);
		CreateCursor(ImGuiMouseCursor.ResizeEW, SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZEWE);
		CreateCursor(ImGuiMouseCursor.ResizeNESW, SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENESW);
		CreateCursor(ImGuiMouseCursor.ResizeNWSE, SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENWSE);
		CreateCursor(ImGuiMouseCursor.Hand, SDL_SystemCursor.SDL_SYSTEM_CURSOR_HAND);
		CreateCursor(ImGuiMouseCursor.NotAllowed, SDL_SystemCursor.SDL_SYSTEM_CURSOR_NO);

		void CreateCursor(ImGuiMouseCursor imGui, SDL_SystemCursor sdl) => mouseCursors[(int)imGui] = SDL_CreateSystemCursor(sdl);
	}

	readonly IntPtr window;
	readonly IntPtr renderer;

	readonly IntPtr[] mouseCursors = new IntPtr[(int)ImGuiMouseCursor.COUNT];

	int pendingMouseLeaveFrame;
	int mouseButtonsDown;

	static readonly SetClipboardTextFn SetClipboardText = text => SDL_SetClipboardText(text);
	static readonly GetClipboardTextFn GetClipboardText = SDL_GetClipboardText;

	public void Init()
	{
		ImGuiIOPtr io = ImGui.GetIO();

		io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
		io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;

		//TODO: setup clipboard
		io.SetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(SetClipboardText);
		io.GetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(GetClipboardText);
		io.ClipboardUserData = IntPtr.Zero;

		//TODO: setup mouse cursor

		SDL_SetHint(SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
	}

	public bool ProcessEvent(in SDL_Event sdlEvent)
	{
		ImGuiIOPtr io = ImGui.GetIO();

		switch (sdlEvent.type)
		{
			case SDL_EventType.SDL_MOUSEMOTION:
			{
				io.AddMousePosEvent(sdlEvent.motion.x, sdlEvent.motion.y);
				return true;
			}
			case SDL_EventType.SDL_MOUSEWHEEL:
			{
				io.AddMouseWheelEvent(Math.Sign(sdlEvent.wheel.x), Math.Sign(sdlEvent.wheel.y));
				return true;
			}
			case SDL_EventType.SDL_MOUSEBUTTONDOWN:
			case SDL_EventType.SDL_MOUSEBUTTONUP:
			{
				int mouseButton = (uint)sdlEvent.button.button switch
				{
					SDL_BUTTON_LEFT => 0,
					SDL_BUTTON_RIGHT => 1,
					SDL_BUTTON_MIDDLE => 2,
					SDL_BUTTON_X1 => 3,
					SDL_BUTTON_X2 => 4,
					_ => -1
				};

				if (mouseButton < 0) break;

				bool down = sdlEvent.type == SDL_EventType.SDL_MOUSEBUTTONDOWN;
				int bits = 1 << mouseButton;

				io.AddMouseButtonEvent(mouseButton, down);

				if (down) mouseButtonsDown |= bits;
				else mouseButtonsDown &= ~bits;
				return true;
			}
			case SDL_EventType.SDL_TEXTINPUT:
			{
				fixed (byte* ptr = sdlEvent.text.text)
				{
					ImGuiNative.ImGuiIO_AddInputCharactersUTF8(io.NativePtr, ptr);
				}

				return true;
			}
			case SDL_EventType.SDL_KEYDOWN:
			case SDL_EventType.SDL_KEYUP:
			{
				ref readonly SDL_Keysym key = ref sdlEvent.key.keysym;
				bool down = sdlEvent.type == SDL_EventType.SDL_KEYDOWN;

				UpdateKeyModifiers(key.mod);
				io.AddKeyEvent(KeycodeToImGuiKey(key.sym), down);
				//TODO: support legacy indexing
				return true;
			}
			case SDL_EventType.SDL_WINDOWEVENT:
			{
				SDL_WindowEventID windowEvent = sdlEvent.window.windowEvent;

				if (windowEvent == SDL_WindowEventID.SDL_WINDOWEVENT_ENTER) pendingMouseLeaveFrame = 0;
				if (windowEvent == SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE) pendingMouseLeaveFrame = ImGui.GetFrameCount() + 1;

				if (windowEvent == SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED) io.AddFocusEvent(true);
				if (windowEvent == SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST) io.AddFocusEvent(false);
				return true;
			}
		}

		return false;
	}

	public void NewFrame(float deltaTime)
	{
		ImGuiIOPtr io = ImGui.GetIO();

		//Setup display size
		SDL_GetWindowSize(window, out int width, out int height);

		if ((SDL_GetWindowFlags(window) & (uint)SDL_WindowFlags.SDL_WINDOW_MINIMIZED) != 0)
		{
			width = 0;
			height = 0;
		}

		SDL_GetRendererOutputSize(renderer, out int displayWidth, out int displayHeight);

		io.DisplaySize = new Vector2(width, height);

		if (width > 0 && height > 0) io.DisplayFramebufferScale = new Vector2((float)displayWidth / width, (float)displayHeight / height);

		//Setup time step
		io.DeltaTime = deltaTime;

		if (pendingMouseLeaveFrame != 0 && pendingMouseLeaveFrame >= ImGui.GetFrameCount() && mouseButtonsDown == 0)
		{
			io.AddMousePosEvent(float.MinValue, float.MinValue);
			pendingMouseLeaveFrame = 0;
		}

		UpdateMouseData();
		UpdateMouseCursor();
	}

	public void Shutdown()
	{
		ImGuiIOPtr io = ImGui.GetIO();

		//TODO: free clipboard data

		io.SetClipboardTextFn = IntPtr.Zero;
		io.GetClipboardTextFn = IntPtr.Zero;

		foreach (IntPtr cursor in mouseCursors) SDL_FreeCursor(cursor);
	}

	void UpdateMouseData()
	{
		ImGuiIOPtr io = ImGui.GetIO();

		SDL_CaptureMouse(mouseButtonsDown != 0 ? SDL_bool.SDL_TRUE : SDL_bool.SDL_FALSE);
		bool isFocused = window == SDL_GetKeyboardFocus();

		if (isFocused)
		{
			if (io.WantSetMousePos) SDL_WarpMouseInWindow(window, (int)io.MousePos.X, (int)io.MousePos.Y);

			if (mouseButtonsDown == 0)
			{
				SDL_GetGlobalMouseState(out int mouseGlobalX, out int mouseGlobalY);
				SDL_GetWindowPosition(window, out int windowX, out int windowY);

				io.AddMousePosEvent(mouseGlobalX - windowX, mouseGlobalY - windowY);
			}
		}
	}

	void UpdateMouseCursor()
	{
		ImGuiIOPtr io = ImGui.GetIO();

		if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) != 0) return;

		var imguiCursor = ImGui.GetMouseCursor();
		if (io.MouseDrawCursor || imguiCursor == ImGuiMouseCursor.None)
		{
			SDL_ShowCursor((int)SDL_bool.SDL_FALSE);
		}
		else
		{
			IntPtr cursor = mouseCursors[(int)imguiCursor];
			if (cursor == IntPtr.Zero) cursor = mouseCursors[(int)ImGuiMouseCursor.Arrow];

			SDL_SetCursor(cursor);
			SDL_ShowCursor((int)SDL_bool.SDL_TRUE);
		}
	}

	static void UpdateKeyModifiers(SDL_Keymod modifier)
	{
		ImGuiIOPtr io = ImGui.GetIO();

		io.AddKeyEvent(ImGuiKey.ModCtrl, (modifier & SDL_Keymod.KMOD_CTRL) != 0);
		io.AddKeyEvent(ImGuiKey.ModShift, (modifier & SDL_Keymod.KMOD_SHIFT) != 0);
		io.AddKeyEvent(ImGuiKey.ModAlt, (modifier & SDL_Keymod.KMOD_ALT) != 0);
		io.AddKeyEvent(ImGuiKey.ModSuper, (modifier & SDL_Keymod.KMOD_GUI) != 0);
	}

	static ImGuiKey KeycodeToImGuiKey(SDL_Keycode keycode) => keycode switch
	{
		SDL_Keycode.SDLK_TAB => ImGuiKey.Tab,
		SDL_Keycode.SDLK_LEFT => ImGuiKey.LeftArrow,
		SDL_Keycode.SDLK_RIGHT => ImGuiKey.RightArrow,
		SDL_Keycode.SDLK_UP => ImGuiKey.UpArrow,
		SDL_Keycode.SDLK_DOWN => ImGuiKey.DownArrow,
		SDL_Keycode.SDLK_PAGEUP => ImGuiKey.PageUp,
		SDL_Keycode.SDLK_PAGEDOWN => ImGuiKey.PageDown,
		SDL_Keycode.SDLK_HOME => ImGuiKey.Home,
		SDL_Keycode.SDLK_END => ImGuiKey.End,
		SDL_Keycode.SDLK_INSERT => ImGuiKey.Insert,
		SDL_Keycode.SDLK_DELETE => ImGuiKey.Delete,
		SDL_Keycode.SDLK_BACKSPACE => ImGuiKey.Backspace,
		SDL_Keycode.SDLK_SPACE => ImGuiKey.Space,
		SDL_Keycode.SDLK_RETURN => ImGuiKey.Enter,
		SDL_Keycode.SDLK_ESCAPE => ImGuiKey.Escape,
		SDL_Keycode.SDLK_QUOTE => ImGuiKey.Apostrophe,
		SDL_Keycode.SDLK_COMMA => ImGuiKey.Comma,
		SDL_Keycode.SDLK_MINUS => ImGuiKey.Minus,
		SDL_Keycode.SDLK_PERIOD => ImGuiKey.Period,
		SDL_Keycode.SDLK_SLASH => ImGuiKey.Slash,
		SDL_Keycode.SDLK_SEMICOLON => ImGuiKey.Semicolon,
		SDL_Keycode.SDLK_EQUALS => ImGuiKey.Equal,
		SDL_Keycode.SDLK_LEFTBRACKET => ImGuiKey.LeftBracket,
		SDL_Keycode.SDLK_BACKSLASH => ImGuiKey.Backslash,
		SDL_Keycode.SDLK_RIGHTBRACKET => ImGuiKey.RightBracket,
		SDL_Keycode.SDLK_BACKQUOTE => ImGuiKey.GraveAccent,
		SDL_Keycode.SDLK_CAPSLOCK => ImGuiKey.CapsLock,
		SDL_Keycode.SDLK_SCROLLLOCK => ImGuiKey.ScrollLock,
		SDL_Keycode.SDLK_NUMLOCKCLEAR => ImGuiKey.NumLock,
		SDL_Keycode.SDLK_PRINTSCREEN => ImGuiKey.PrintScreen,
		SDL_Keycode.SDLK_PAUSE => ImGuiKey.Pause,
		SDL_Keycode.SDLK_KP_0 => ImGuiKey.Keypad0,
		SDL_Keycode.SDLK_KP_1 => ImGuiKey.Keypad1,
		SDL_Keycode.SDLK_KP_2 => ImGuiKey.Keypad2,
		SDL_Keycode.SDLK_KP_3 => ImGuiKey.Keypad3,
		SDL_Keycode.SDLK_KP_4 => ImGuiKey.Keypad4,
		SDL_Keycode.SDLK_KP_5 => ImGuiKey.Keypad5,
		SDL_Keycode.SDLK_KP_6 => ImGuiKey.Keypad6,
		SDL_Keycode.SDLK_KP_7 => ImGuiKey.Keypad7,
		SDL_Keycode.SDLK_KP_8 => ImGuiKey.Keypad8,
		SDL_Keycode.SDLK_KP_9 => ImGuiKey.Keypad9,
		SDL_Keycode.SDLK_KP_PERIOD => ImGuiKey.KeypadDecimal,
		SDL_Keycode.SDLK_KP_DIVIDE => ImGuiKey.KeypadDivide,
		SDL_Keycode.SDLK_KP_MULTIPLY => ImGuiKey.KeypadMultiply,
		SDL_Keycode.SDLK_KP_MINUS => ImGuiKey.KeypadSubtract,
		SDL_Keycode.SDLK_KP_PLUS => ImGuiKey.KeypadAdd,
		SDL_Keycode.SDLK_KP_ENTER => ImGuiKey.KeypadEnter,
		SDL_Keycode.SDLK_KP_EQUALS => ImGuiKey.KeypadEqual,
		SDL_Keycode.SDLK_LCTRL => ImGuiKey.LeftCtrl,
		SDL_Keycode.SDLK_LSHIFT => ImGuiKey.LeftShift,
		SDL_Keycode.SDLK_LALT => ImGuiKey.LeftAlt,
		SDL_Keycode.SDLK_LGUI => ImGuiKey.LeftSuper,
		SDL_Keycode.SDLK_RCTRL => ImGuiKey.RightCtrl,
		SDL_Keycode.SDLK_RSHIFT => ImGuiKey.RightShift,
		SDL_Keycode.SDLK_RALT => ImGuiKey.RightAlt,
		SDL_Keycode.SDLK_RGUI => ImGuiKey.RightSuper,
		SDL_Keycode.SDLK_APPLICATION => ImGuiKey.Menu,
		SDL_Keycode.SDLK_0 => ImGuiKey._0,
		SDL_Keycode.SDLK_1 => ImGuiKey._1,
		SDL_Keycode.SDLK_2 => ImGuiKey._2,
		SDL_Keycode.SDLK_3 => ImGuiKey._3,
		SDL_Keycode.SDLK_4 => ImGuiKey._4,
		SDL_Keycode.SDLK_5 => ImGuiKey._5,
		SDL_Keycode.SDLK_6 => ImGuiKey._6,
		SDL_Keycode.SDLK_7 => ImGuiKey._7,
		SDL_Keycode.SDLK_8 => ImGuiKey._8,
		SDL_Keycode.SDLK_9 => ImGuiKey._9,
		SDL_Keycode.SDLK_a => ImGuiKey.A,
		SDL_Keycode.SDLK_b => ImGuiKey.B,
		SDL_Keycode.SDLK_c => ImGuiKey.C,
		SDL_Keycode.SDLK_d => ImGuiKey.D,
		SDL_Keycode.SDLK_e => ImGuiKey.E,
		SDL_Keycode.SDLK_f => ImGuiKey.F,
		SDL_Keycode.SDLK_g => ImGuiKey.G,
		SDL_Keycode.SDLK_h => ImGuiKey.H,
		SDL_Keycode.SDLK_i => ImGuiKey.I,
		SDL_Keycode.SDLK_j => ImGuiKey.J,
		SDL_Keycode.SDLK_k => ImGuiKey.K,
		SDL_Keycode.SDLK_l => ImGuiKey.L,
		SDL_Keycode.SDLK_m => ImGuiKey.M,
		SDL_Keycode.SDLK_n => ImGuiKey.N,
		SDL_Keycode.SDLK_o => ImGuiKey.O,
		SDL_Keycode.SDLK_p => ImGuiKey.P,
		SDL_Keycode.SDLK_q => ImGuiKey.Q,
		SDL_Keycode.SDLK_r => ImGuiKey.R,
		SDL_Keycode.SDLK_s => ImGuiKey.S,
		SDL_Keycode.SDLK_t => ImGuiKey.T,
		SDL_Keycode.SDLK_u => ImGuiKey.U,
		SDL_Keycode.SDLK_v => ImGuiKey.V,
		SDL_Keycode.SDLK_w => ImGuiKey.W,
		SDL_Keycode.SDLK_x => ImGuiKey.X,
		SDL_Keycode.SDLK_y => ImGuiKey.Y,
		SDL_Keycode.SDLK_z => ImGuiKey.Z,
		SDL_Keycode.SDLK_F1 => ImGuiKey.F1,
		SDL_Keycode.SDLK_F2 => ImGuiKey.F2,
		SDL_Keycode.SDLK_F3 => ImGuiKey.F3,
		SDL_Keycode.SDLK_F4 => ImGuiKey.F4,
		SDL_Keycode.SDLK_F5 => ImGuiKey.F5,
		SDL_Keycode.SDLK_F6 => ImGuiKey.F6,
		SDL_Keycode.SDLK_F7 => ImGuiKey.F7,
		SDL_Keycode.SDLK_F8 => ImGuiKey.F8,
		SDL_Keycode.SDLK_F9 => ImGuiKey.F9,
		SDL_Keycode.SDLK_F10 => ImGuiKey.F10,
		SDL_Keycode.SDLK_F11 => ImGuiKey.F11,
		SDL_Keycode.SDLK_F12 => ImGuiKey.F12,
		_ => ImGuiKey.None
	};

	delegate void SetClipboardTextFn(string text);
	delegate string GetClipboardTextFn();
}