using System;
using System.Numerics;
using System.Threading.Tasks;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Packed;
using Echo.Core.InOut.Images;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;
using Echo.UserInterface.Backend;
using ImGuiNET;
using SDL2;

namespace Echo.UserInterface.Core.Areas;

public sealed class AboutUI : AreaUI
{
	public AboutUI(EchoUI root) : base(root) { }

	Float2 logoAspects;
	IntPtr logoTexture;

	protected override string Name => "About";

	protected override void NewFrameWindow()
	{
		if (logoTexture == IntPtr.Zero) LoadLogo();
		Vector2 region = ImGui.GetContentRegionAvail();

		if (logoTexture != IntPtr.Zero && ImGui.BeginChild("Logo", region with { X = region.X * 0.3f }))
		{
			float lineHeight = ImGui.GetTextLineHeight();
			float windowWidth = ImGui.GetWindowWidth();

			float width = windowWidth * 0.6f;
			float height = width * logoAspects.Y;
			height = MathF.Min(height, lineHeight * 8);
			width = height * logoAspects.X;

			ImGui.NewLine();
			ImGui.SetCursorPosX((windowWidth - width) / 2f);
			ImGui.Image(logoTexture, new Vector2(width, height));

			ImGui.EndChild();
			ImGui.SameLine();
		}

		if (ImGui.BeginChild("Texts"))
		{
			ImGui.NewLine();

			NameWithVersion("Echo Photorealistic Rendering Core", GetVersion(typeof(Device)));
			NameWithVersion("Graphical Render User Interface", GetVersion(typeof(AboutUI)));

			ImGui.NewLine();

			NameWithVersion(".NET Runtime", Environment.Version);
			ImGui.Text(Environment.OSVersion.VersionString);

			ImGui.NewLine();

			ImGui.Text("Powered by Magick.NET, ImGui.NET, and SDL2-CS.");
			ImGui.Text("Copyright (C) 2020-2023 Gary Huang, et al.");
			ImGui.Text("All rights reserved.");

			ImGui.EndChild();

			static void NameWithVersion(string name, Version version)
			{
				ImGui.Text(name);
				ImGui.SameLine();
				ImGui.TextDisabled($"[Version {version.ToString(3)}]");
			}
		}
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (!disposing) return;

		if (logoTexture == IntPtr.Zero) return;
		root.backend.DestroyTexture(ref logoTexture);
	}

	unsafe void LoadLogo()
	{
		const string LogoPath = "ext/Logo/logo-square.png";
		var serializer = Serializer.Find(LogoPath);
		serializer = serializer with { sRGB = false };

		var logo = TextureGrid.Load<RGBA128>(LogoPath, serializer);
		uint[] pixels = new uint[logo.size.Product];
		logoAspects = logo.aspects;

		Parallel.For(0, logo.size.Y, y =>
		{
			for (int x = 0; x < logo.size.X; x++)
			{
				var bytes = ColorConverter.Float4ToBytes(logo.Get(new Int2(x, y)));
				pixels[x + y * logo.size.X] = ColorConverter.GatherBytes(bytes);
			}
		});

		fixed (uint* pinned = pixels)
		{
			int pitch = -logo.size.X * sizeof(uint);
			uint* pointer = pinned + logo.size.Product - logo.size.X;
			logoTexture = root.backend.CreateTexture(logo.size, false);
			SDL.SDL_UpdateTexture(logoTexture, IntPtr.Zero, new IntPtr(pointer), pitch).ThrowOnError();
		}
	}

	static Version GetVersion(Type type) => type.Assembly.GetName().Version;
}