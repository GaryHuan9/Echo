using System.Runtime.Intrinsics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics;

namespace EchoRenderer.Common;

public static class Constants
{
	public static Vector128<float> LuminanceVector => Vector128.Create(0.2126f, 0.7152f, 0.0722f, 0f);
	public static readonly Float3 radianceEpsilon = Utilities.ToFloat3(PackedMath.CreateLuminance(12E-3f));
}