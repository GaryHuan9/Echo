using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Core.Texturing.Directional;

namespace EchoRenderer.Core.Texturing;

/// <summary>
/// A readonly pure-color <see cref="Texture"/> and <see cref="IDirectionalTexture"/>.
/// </summary>
public class Pure : Texture, IDirectionalTexture
{
	public Pure(in Float4 color) : this(Utilities.ToVector(color)) { }
	public Pure(in Float3 color) : this(Utilities.ToColor(color)) { }
	public Pure(float color) : this(Utilities.ToColor(color)) { }

	Pure(in Vector128<float> color) : base(Wrappers.unbound) => this.color = color;

	readonly Vector128<float> color;

	public override Int2 ImportanceSamplingResolution => Int2.one;

	Vector128<float> IDirectionalTexture.Average => color;

	protected override Vector128<float> Evaluate(Float2 uv) => color;

	Vector128<float> IDirectionalTexture.Evaluate(in Float3 direction) => color;

	public static explicit operator Pure(in Float4 color) => new(color);
	public static explicit operator Pure(in Float3 color) => new(color);
	public static explicit operator Pure(float color) => new(color);
}