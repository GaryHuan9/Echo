using System;
using System.Numerics;
using System.Runtime.InteropServices;
using CodeHelpers.Diagnostics;
using ImGuiNET;

namespace Echo.UserInterface;

using static SDL2.SDL;

public unsafe class ImGuiDisplay
{
	public ImGuiDisplay(IntPtr renderer) => this.renderer = renderer;

	readonly IntPtr renderer;

	IntPtr fontTexture;

	public void Init()
	{
		ImGuiIOPtr io = ImGui.GetIO();

		io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
	}

	public void NewFrame()
	{
		ImGuiIOPtr io = ImGui.GetIO();

		if (fontTexture == IntPtr.Zero)
		{
			io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height);

			fontTexture = SDL_CreateTexture(renderer, SDL_PIXELFORMAT_ABGR8888, (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC, width, height);
			Assert.AreNotEqual(fontTexture, IntPtr.Zero);

			SDL_UpdateTexture(fontTexture, IntPtr.Zero, pixels, 4 * width);
			SDL_SetTextureBlendMode(fontTexture, SDL_BlendMode.SDL_BLENDMODE_BLEND);
			SDL_SetTextureScaleMode(fontTexture, SDL_ScaleMode.SDL_ScaleModeLinear);

			io.Fonts.SetTexID(fontTexture);
		}
	}

	public void RenderDrawData(ImDrawDataPtr data)
	{
		SDL_RenderGetScale(renderer, out float scaleX, out float scaleY);

		float renderScaleX = scaleX == 1f ? data.FramebufferScale.X : 1f;
		float renderScaleY = scaleY == 1f ? data.FramebufferScale.Y : 1f;

		int width = (int)(data.DisplaySize.X * renderScaleX);
		int height = (int)(data.DisplaySize.Y * renderScaleY);

		if (width == 0 || height == 0) return;

		bool old_clipEnabled = SDL_RenderIsClipEnabled(renderer) == SDL_bool.SDL_TRUE;
		SDL_RenderGetViewport(renderer, out SDL_Rect old_viewport);
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

		SDL_RenderSetViewport(renderer, (IntPtr)(&old_viewport));
		SDL_RenderSetClipRect(renderer, old_clipEnabled ? (IntPtr)(&old_clipRect) : IntPtr.Zero);
	}

	public void Shutdown()
	{
		ImGuiIOPtr io = ImGui.GetIO();

		if (fontTexture != IntPtr.Zero)
		{
			io.Fonts.SetTexID(IntPtr.Zero);
			SDL_DestroyTexture(fontTexture);
			fontTexture = IntPtr.Zero;
		}
	}

	void SetupRenderState()
	{
		SDL_RenderSetViewport(renderer, IntPtr.Zero);
		SDL_RenderSetClipRect(renderer, IntPtr.Zero);
	}

	[DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
	static extern int SDL_RenderGeometryRaw(IntPtr renderer,
											IntPtr texture,
											float* xy,
											int xy_stride,
											int* color,
											int color_stride,
											float* uv,
											int uv_stride,
											int num_vertices,
											IntPtr indices,
											int num_indices,
											int size_indices);


	[DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
	static extern int SDL_RenderSetViewport(IntPtr renderer, IntPtr rect);
}