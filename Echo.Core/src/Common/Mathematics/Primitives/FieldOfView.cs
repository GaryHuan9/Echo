using System;
using Echo.Core.Common.Packed;

namespace Echo.Core.Common.Mathematics.Primitives;

/// <summary>
/// A struct used to calculate and store a perspective camera's field of view and aspect ratio.
/// </summary>
public readonly struct FieldOfView
{
	public FieldOfView(Float2 fieldOfView) : this(fieldOfView.X, fieldOfView.Y, 0f)
	{
		Float2 radians = fieldOfView * Scalars.ToRadians(0.5f);
		aspect = (float)(Math.Tan(radians.X) / Math.Tan(radians.Y));
	}

	FieldOfView(float x, float y, float aspect)
	{
		this.x = x;
		this.y = y;
		this.aspect = aspect;
	}

	/// <summary>
	/// The horizontal field of view in degrees.
	/// </summary>
	public readonly float x;

	/// <summary>
	/// The vertical field of view in degrees.
	/// </summary>
	public readonly float y;

	/// <summary>
	/// The aspect ratio of the camera.
	/// NOTE: width divided by height.
	/// </summary>
	public readonly float aspect;

	/// <summary>
	/// Returns the <see cref="x"/> and <see cref="y"/> field of view values combined in degrees.
	/// </summary>
	public Float2 Degrees => new Float2(x, y);

	/// <summary>
	/// Changes the horizontal/<see cref="x"/> field of view while maintaining the same <see cref="aspect"/>.
	/// </summary>
	public FieldOfView SetHorizontal(float horizontal) => SetHorizontal(horizontal, aspect);

	/// <summary>
	/// Changes the vertical/<see cref="y"/> field of view while maintaining the same <see cref="aspect"/>.
	/// </summary>
	public FieldOfView SetVertical(float vertical) => SetVertical(vertical, aspect);

	/// <summary>
	/// Creates a <see cref="FieldOfView"/> using the indicated <paramref name="horizontal"/> field of view and <paramref name="aspect"/>.
	/// NOTE: <paramref name="horizontal"/> is used for <see cref="x"/> and <paramref name="aspect"/> is used for <see cref="aspect"/>.
	/// </summary>
	public static FieldOfView SetHorizontal(float horizontal, float aspect)
	{
		float width = (float)Math.Tan(horizontal * Scalars.ToRadians(0.5f));
		float y = Scalars.ToDegrees(2f) * (float)Math.Atan(width / aspect);

		return new FieldOfView(horizontal, y, aspect);
	}

	/// <summary>
	/// Creates a <see cref="FieldOfView"/> using the indicated <paramref name="vertical"/> field of view and <paramref name="aspect"/>.
	/// NOTE: <paramref name="vertical"/> is used for <see cref="y"/> and <paramref name="aspect"/> is used for <see cref="aspect"/>.
	/// </summary>
	public static FieldOfView SetVertical(float vertical, float aspect)
	{
		float height = (float)Math.Tan(vertical * Scalars.ToRadians(0.5f));
		float x = Scalars.ToDegrees(2f) * (float)Math.Atan(height * aspect);

		return new FieldOfView(x, vertical, aspect);
	}
}