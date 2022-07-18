using System;
using Echo.Core.Common.Diagnostics;

namespace Echo.Core.Common.Mathematics.Primitives;

public static class CurveHelper //https://www.desmos.com/calculator/46uoiq2bdh
{
	public static InputCheckMode CheckMode { get; set; } = InputCheckMode.clamp;

	static void CheckRange(ref float input)
	{
		switch (CheckMode)
		{
			case InputCheckMode.exception:
			{
				if ((input < 0f) | (input > 1f)) throw ExceptionHelper.Invalid(nameof(input), input, InvalidType.outOfBounds);
				break;
			}
			case InputCheckMode.clamp:
			{
				input = input.Clamp();
				break;
			}
			default: throw ExceptionHelper.NotPossible;
		}
	}

	public static float Flip(float input)
	{
		CheckRange(ref input);
		return 1f - input;
	}

	public static float Linear(float input)
	{
		CheckRange(ref input);
		return input;
	}

	public static float Sigmoid(float input)
	{
		CheckRange(ref input);
		float input2 = input * input;
		return 3f * input2 - 2f * input2 * input;
	}

	public static float EaseIn(float input)
	{
		CheckRange(ref input);
		return input * input;
	}

	public static float EaseOut(float input)
	{
		CheckRange(ref input);
		return -input * input + 2f * input;
	}

	/// <summary>
	/// <paramref name="acceleration"/> is how fast will input speed up/slow down
	/// 0 means no speed change (linear); negative means slowing down; positive means speeding up
	/// </summary>
	public static float Ease(float input, float acceleration)
	{
		CheckRange(ref input);

		if (acceleration.AlmostEquals()) return input;

		if (acceleration > 0f) return (float)Math.Pow(input, acceleration + 1f);
		return 1f - (float)Math.Pow(1f - input, -acceleration + 1f);
	}

	public static float EaseInSmooth(float input) => EaseIn(Sigmoid(input));
	public static float EaseOutSmooth(float input) => EaseOut(Sigmoid(input));

	/// <summary>
	/// <paramref name="steepness"/> is how quick the input will grow in the middle
	/// lowest is 0f which means default speed
	/// Graph: https://www.desmos.com/calculator/jyi4mdbxpj
	/// </summary>
	public static float Sigmoid(float input, float steepness)
	{
		CheckRange(ref input);
		if (steepness < 0f) throw ExceptionHelper.Invalid(nameof(steepness), steepness, InvalidType.outOfBounds);

		steepness += 1f;
		float bounds = 0.5f * (steepness - 1f) / steepness;

		if (input <= bounds) return 0f;
		if (input >= 1f - bounds) return 1f;

		return 0.5f + 0.5f * (float)Math.Sin((input * Math.PI - Math.PI * 0.5f) * steepness);
	}

	public enum InputCheckMode
	{
		exception,
		clamp
	}
}