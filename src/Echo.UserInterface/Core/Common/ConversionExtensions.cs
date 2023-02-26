using System.Numerics;
using Echo.Core.Common.Packed;

namespace Echo.UserInterface.Core.Common;

public static class ConversionExtensions
{
	public static Vector2 AsVector2(this Float2 value) => new(value.X, value.Y);
	public static Float2 AsFloat2(this Vector2 value) => new(value.X, value.Y);
}