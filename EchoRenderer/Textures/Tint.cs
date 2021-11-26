using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;

namespace EchoRenderer.Textures
{
	/// <summary>
	/// A custom linear transform that can be applied to a color.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 32)]
	public readonly struct Tint
	{
		Tint(in Float4 scale, in Float4 offset)
		{
			this.scale = Utilities.ToVector(scale);
			this.offset = Utilities.ToVector(offset);
		}

		[FieldOffset(00)] readonly Vector128<float> scale;
		[FieldOffset(16)] readonly Vector128<float> offset;

		public static readonly Tint identity = new(Float4.one, Float4.zero);

		public void Apply(ref Vector128<float> color) => Utilities.Fused(color, scale, offset);

		public static Tint Scale(in Float4 value) => new(value, Float4.zero);
		public static Tint Scale(in Float3 value) => Scale(Utilities.ToColor(value));

		public static Tint Offset(in Float4 value) => new(Float4.one, value);
		public static Tint Offset(in Float3 value) => Offset((Float4)value);

		public static Tint Inverse(in Float4 value) => new(-value, value);
		public static Tint Inverse(in Float3 value) => Inverse(Utilities.ToColor(value));
		public static Tint Inverse()                => Inverse(Float4.one);
	}
}