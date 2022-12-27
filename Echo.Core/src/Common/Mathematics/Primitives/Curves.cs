using System;

namespace Echo.Core.Common.Mathematics.Primitives;

/// <summary>
/// A collection of strictly increasing curves that map from [0, 1] to [0, 1] (the normal range).
/// </summary>
/// <remarks>
/// If the input is outside of the normal range, it is clamped.
/// The curves can be visualized here: https://www.desmos.com/calculator/g6wsyz4pgs
/// </remarks>
public static class Curves
{
	/// <summary>
	/// A curve where the start and finish are more gradual than the middle.
	/// </summary>
	public static float Sigmoid(float input)
	{
		input = FastMath.Clamp01(input);
		
		//The following is a manipulated version of the canonical 3x^2-2x^3 smooth step function
		//It is carefully adjusted and tested to fully satisfy the strictly increasing requirement
		
		float input2 = input * input;
		float fma = MathF.FusedMultiplyAdd(-2f, input2, input);
		return MathF.FusedMultiplyAdd(input, fma, 2f * input2);
	}

	/// <summary>
	/// A curve with a slow start and a fast finish.
	/// </summary>
	public static float EaseIn(float input)
	{
		input = FastMath.Clamp01(input);
		return input * input;
	}

	/// <summary>
	/// A curve with a fast start and a slow finish.
	/// </summary>
	public static float EaseOut(float input)
	{
		input = FastMath.Clamp01(input);
		return MathF.FusedMultiplyAdd(input, -input, 2f * input);
	}

	/// <summary>
	/// A curve with a flat slow start and a flat fast finish.
	/// </summary>
	public static float EaseInSmooth(float input) => EaseIn(Sigmoid(input));

	/// <summary>
	/// A curve with a flat fast start and a flat slow finish.
	/// </summary>
	public static float EaseOutSmooth(float input) => EaseOut(Sigmoid(input));
}