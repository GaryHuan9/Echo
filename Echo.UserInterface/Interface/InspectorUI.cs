using System;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Scenic;
using Echo.UserInterface.Core.Areas;
using Echo.UserInterface.Core.Fields;
using Echo.UserInterface.Core;
using SFML.Window;

namespace Echo.UserInterface.Interface;

public class InspectorUI : WindowUI
{
	public InspectorUI() : base("Inspector")
	{
		transform.LeftPercent = 0.72f;
		transform.BottomPercent = 0.4f;

		group.Add(new ButtonUI {label = {Text = "Button"}}.Label("Click Me"));
		group.Add(new TextFieldUI {Text = "Test Field Here"}.Label("Type me"));
		group.Add(new FloatFieldUI().Label("Sample Count"));

		// Add
		// (
		// 	new AutoLayoutAreaUI { }.Add
		// 	(
		// 		new LabelUI
		// 		{
		// 			Text = "Hello World 1",
		// 			Align = LabelUI.Alignment.left
		// 		}
		// 	).Add
		// 	(
		// 		new ButtonUI
		// 		{
		// 			label =
		// 			{
		// 				Text = "Button 1",
		// 				Align = LabelUI.Alignment.right
		// 			}
		// 		}
		// 	).Add
		// 	(
		// 		new LabelUI
		// 		{
		// 			Text = "Hello World 2 pp"
		// 		}
		// 	).Add
		// 	(
		// 		new ButtonUI
		// 		{
		// 			label = {Text = "Button 2"}
		// 		}
		// 	).Add
		// 	(
		// 		new ButtonUI
		// 		{
		// 			label = {Text = "Button 3"}
		// 		}
		// 	).Add
		// 	(
		// 		new TextFieldUI {Text = "Test Field Hehe"}
		// 	).Add
		// 	(
		// 		new FloatFieldUI { }
		// 	).Add
		// 	(
		// 		new Float3FieldUI { }
		// 	)
		// );
	}

	bool lastEnabled;
	Int2 lastMouse;

	const float MouseSensitivity = 0.18f;
	const float MovementSpeed = 3.5f;

	public override void Update()
	{
		base.Update();

		// SceneViewUI sceneView = Root.Find<SceneViewUI>();
		// Camera camera = sceneView?.Profile.Scene?.camera;

		SceneViewUI sceneView = null;
		Camera camera = null;

		if (camera == null) return;

		if (Mouse.IsButtonPressed(Mouse.Button.Right))
		{
			Int2 mouse = Mouse.GetPosition().As();
			Float4x4 matrix = camera.LocalToWorld;

			if (!lastEnabled)
			{
				lastEnabled = true;
				lastMouse = mouse;
			}

			Int2 delta = mouse - lastMouse;

			Float2 input = new Int2
			(
				KeyDown(Keyboard.Key.D) - KeyDown(Keyboard.Key.A),
				KeyDown(Keyboard.Key.W) - KeyDown(Keyboard.Key.S)
			).Normalized;

			Float3 movement = input.X_Y * (float)Root.application.DeltaTime;
			Float3 change = camera.LocalToWorld.MultiplyDirection(movement);

			camera.Rotation += delta.YX_ * MouseSensitivity;
			camera.Position += change * MovementSpeed;

			lastMouse = mouse;

			if (matrix != camera.LocalToWorld) sceneView.RequestRedraw();

			static int KeyDown(Keyboard.Key key) => Convert.ToInt32(Keyboard.IsKeyPressed(key));
		}
		else lastEnabled = false;
	}
}