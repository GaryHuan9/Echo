using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;

namespace EchoRenderer.Textures
{
	/// <summary>
	/// A readonly pure-color <see cref="Texture"/>.
	/// </summary>
	public class Pure : Texture
	{
		public Pure(in Vector128<float> color) : base(Wrappers.unbound) => this.color = color;

		public Pure(in Float4 color) : this(Utilities.ToVector(color)) { }
		public Pure(in Float3 color) : this(Utilities.ToColor(color)) { }
		public Pure(float     color) : this(Utilities.ToColor(color)) { }

		readonly Vector128<float> color;

		public Float3 Color => Utilities.ToFloat3(color);

		protected override Vector128<float> Evaluate(Float2 uv) => color;

		public static explicit operator Pure(in Float3 color) => new(color);
		public static explicit operator Pure(in Float4 color) => new(color);
	}
}