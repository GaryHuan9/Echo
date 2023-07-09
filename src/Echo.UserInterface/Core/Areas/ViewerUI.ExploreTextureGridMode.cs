using System;
using System.Numerics;
using System.Threading;
using Echo.Core.Common;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.Scenic.Cameras;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;
using ImGuiNET;

namespace Echo.UserInterface.Core.Areas;

using DepthTexture = TextureGrid<NormalDepth128>;

partial class ViewerUI
{
	sealed class ExploreTextureGridMode : Mode
	{
		public ExploreTextureGridMode(EchoUI root) : base(root) { }

		TextureGrid mainTexture;
		DepthTexture depthTexture;
		Camera camera;

		Float3 viewPosition;
		Float3 viewRotation;
		float viewFieldOfView;
		float viewSpeed = 0.03f;
		float viewSensitivity = 0.1f;

		int pointsLength;
		AlignedArray<float> pointsX;
		AlignedArray<float> pointsY;
		AlignedArray<float> pointsZ;
		AlignedArray<uint> pointsColor;

		bool resetBuffer;
		float[] depthBuffer;
		uint[] colorBuffer;

		public override float AspectRatio => mainTexture.aspects.X;

		public override bool LockPlane => true;

		public void Reset(TextureGrid newMainTexture, DepthTexture newDepthTexture, Camera newCamera)
		{
			if (mainTexture == newMainTexture && depthTexture == newDepthTexture && camera == newCamera) return;

			mainTexture = newMainTexture;
			depthTexture = newDepthTexture;
			camera = newCamera;
			ResetCamera();

			int capacity = mainTexture.size.Product;
			Utility.EnsureCapacity(ref pointsX, capacity);
			Utility.EnsureCapacity(ref pointsY, capacity);
			Utility.EnsureCapacity(ref pointsZ, capacity);
			Utility.EnsureCapacity(ref pointsColor, capacity);

			Interlocked.Exchange(ref pointsLength, 0);
			ActionQueue.Enqueue("Populate Point Cloud", PopulatePoints);
		}

		public override bool Draw(ImDrawListPtr drawList, in Bounds plane, Float2? cursorUV)
		{
			Int2 size = Int2.Min((plane.extend * 2f).Rounded, mainTexture.size);

			if (RecreateDisplay(size))
			{
				int length = size.Product;

				Utility.EnsureCapacity(ref depthBuffer, length);
				Utility.EnsureCapacity(ref colorBuffer, length);
				resetBuffer = true;
			}

			if (UpdateViewFromInput() || resetBuffer)
			{
				//If this is too slow in the future, we can rewrite it to use SIMD and progress update across frames
				bool updated = UpdateBufferFromView(size);
				if (updated) resetBuffer = false;
			}

			UpdateDisplayFromBuffer(size);
			DrawDisplay(drawList, plane);
			return true;
		}

		public override void DrawMenuBar()
		{
			base.DrawMenuBar();

			if (ImGui.BeginMenu("Camera"))
			{
				if (ImGui.MenuItem("Help")) LogList.Add("Hold down Left Mouse Button to change the view: WASD to move and mouse drag to rotate.");

				if (ImGui.MenuItem("Reset")) ResetCamera();

				if (ImGui.BeginMenu("Settings"))
				{
					ImGui.SliderFloat("Field of View", ref viewFieldOfView, 10f, 170f);
					ImGui.SliderFloat("Speed", ref viewSpeed, 1E-3f, 1E2f, "%f", ImGuiSliderFlags.Logarithmic);
					ImGui.SliderFloat("Sensitivity", ref viewSensitivity, 0f, 1f);

					ImGui.EndMenu();
				}

				if (ImGui.BeginMenu("Transform"))
				{
					Vector3 position = new Vector3(viewPosition.X, viewPosition.Y, viewPosition.Z);
					Vector3 rotation = new Vector3(viewRotation.X, viewRotation.Y, viewRotation.Z);

					ImGui.InputFloat3("Position", ref position);
					ImGui.InputFloat3("Rotation", ref rotation);

					Float3 newPosition = new Float3(position.X, position.Y, position.Z);
					Float3 newRotation = new Float3(rotation.X, rotation.Y, rotation.Z);

					if (viewPosition != newPosition || viewRotation != newRotation)
					{
						viewPosition = newPosition;
						viewRotation = newRotation;
						resetBuffer = true;
					}

					ImGui.EndMenu();
				}

				ImGui.EndMenu();
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (!disposing) return;

			pointsX?.Dispose();
			pointsY?.Dispose();
			pointsZ?.Dispose();
			pointsColor?.Dispose();
		}

		void ResetCamera()
		{
			viewPosition = camera.ContainedPosition;
			viewRotation = camera.ContainedRotation.Angles;
			viewFieldOfView = camera is PerspectiveCamera perspective ? perspective.FieldOfView : 90f;

			resetBuffer = true;
		}

		bool UpdateViewFromInput()
		{
			if (!ImGui.IsWindowFocused(ImGuiFocusedFlags.RootWindow)) return false;
			if (!ImGui.IsMouseDown(ImGuiMouseButton.Left)) return false;

			bool changed = false;

			Int3 keyboard = new Int3
			(
				Pressed(ImGuiKey.D) - Pressed(ImGuiKey.A),
				Pressed(ImGuiKey.Space) - Pressed(ImGuiKey.LeftShift),
				Pressed(ImGuiKey.W) - Pressed(ImGuiKey.S)
			);

			if (keyboard != Int3.Zero)
			{
				float delta = (float)root.Moment.delta.TotalMilliseconds * viewSpeed;
				viewPosition += new Versor(viewRotation) * keyboard.Normalized * delta;

				changed = true;
			}

			var mouse = ImGui.GetIO().MouseDelta;

			Float3 newRotation = new Float3
			(
				(viewRotation.X + mouse.Y * viewSensitivity).Clamp(-90f, 90f),
				viewRotation.Y + mouse.X * viewSensitivity, 0f
			);

			if (newRotation != viewRotation)
			{
				viewRotation = newRotation;
				changed = true;
			}

			return changed;

			static int Pressed(ImGuiKey key) => ImGui.IsKeyDown(key) ? 1 : 0;
		}

		bool UpdateBufferFromView(Int2 size)
		{
			int length = Volatile.Read(ref pointsLength);
			if (length == 0) return false;

			depthBuffer.AsSpan(0, size.Product).Fill(float.NegativeInfinity);
			colorBuffer.AsSpan(0, size.Product).Clear();

			float fov = Scalars.ToRadians(viewFieldOfView);
			float widthScale = 1f / MathF.Tan(fov / 2f);
			float heightScale = widthScale * mainTexture.aspects.X;

			Float4x4 projection = new Float4x4
			(
				widthScale, 0f, 0f, 0f,
				0f, heightScale, 0f, 0f,
				0f, 0f, 1f, 1f,
				0f, 0f, 1f, 0f
			);

			Float4x4 transform = projection * Float4x4.Transformation(viewPosition, viewRotation, Float3.One).Inverse;

			for (int i = 0; i < length; i++)
			{
				Float4 converted = transform * new Float4(pointsX[i], pointsY[i], pointsZ[i], 1f);
				Float3 point = new Float3(converted.X, converted.Y, converted.Z) / converted.W;

				if (point.Z < 1f) continue;

				Float2 uv = new Float2(point.X / 2f + 0.5f, point.Y / -2f + 0.5f);
				Int2 position = (Int2)(uv * size);

				if (Int2.Zero <= position && position < size)
				{
					int index = position.Y * size.X + position.X;
					if (point.Z <= depthBuffer[index]) continue;

					depthBuffer[index] = point.Z;
					colorBuffer[index] = pointsColor[i];
				}
			}

			return true;
		}

		unsafe void UpdateDisplayFromBuffer(Int2 size)
		{
			LockDisplay(out uint* pixels);

			fixed (uint* pointer = colorBuffer)
			{
				ulong length = sizeof(uint) * (ulong)size.Product;
				Buffer.MemoryCopy(pointer, pixels, length, length);
			}

			UnlockDisplay();
		}

		void PopulatePoints()
		{
			int length = mainTexture.size.Product;

			Ensure.IsTrue(length <= pointsX.Length);
			Ensure.IsTrue(length <= pointsY.Length);
			Ensure.IsTrue(length <= pointsZ.Length);
			Ensure.IsTrue(length <= pointsColor.Length);
			Ensure.AreEqual(mainTexture.size, depthTexture.size);

			int index = 0;

			for (int y = 0; y < mainTexture.size.Y; y++)
			for (int x = 0; x < mainTexture.size.X; x++)
			{
				Int2 position = new Int2(x, y);
				float distance = depthTexture.Get(position).depth;
				var spawner = new RaySpawner(mainTexture, position);
				Float3 point = camera.SpawnRay(spawner).GetPoint(distance);

				pointsX[index] = point.X;
				pointsY[index] = point.Y;
				pointsZ[index] = point.Z;
				pointsColor[index] = ColorToUInt32(mainTexture[position]);

				index++;
			}

			Thread.MemoryBarrier();
			Volatile.Write(ref pointsLength, length);
		}
	}
}