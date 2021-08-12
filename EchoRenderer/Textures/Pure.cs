using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;

namespace EchoRenderer.Textures
{
	/// <summary>
	/// A pure-color <see cref="Texture"/>.
	/// </summary>
	public class Pure : Texture
	{
		public Pure(Vector128<float> color) : base(Wrappers.unbound) => this.color = color;

		public Pure(Float4 color) : this(Utilities.ToVector(color)) { }

		public Pure(float color) : this(Vector128.Create(color)) { }

		readonly Vector128<float> color;

		public Float3 Color => Utilities.ToFloat3(color);

		protected override Vector128<float> GetPixel(Float2 uv) => color;
	}
}