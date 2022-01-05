﻿using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Textures.Directional;

namespace EchoRenderer.Textures
{
	/// <summary>
	/// A readonly pure-color <see cref="Texture"/> and <see cref="IDirectionalTexture"/>.
	/// </summary>
	public class Pure : Texture, IDirectionalTexture
	{
		public Pure(in Vector128<float> color) : base(Wrappers.unbound) => this.color = color;

		public Pure(in Float4 color) : this(Utilities.ToVector(color)) { }
		public Pure(in Float3 color) : this(Utilities.ToColor(color)) { }
		public Pure(float     color) : this(Utilities.ToColor(color)) { }

		readonly Vector128<float> color;

		public Float3 Color => Utilities.ToFloat3(color);

		public override Int2 ImportanceSamplingResolution => Int2.one;

		protected override Vector128<float> Evaluate(Float2 uv) => color;

		public Vector128<float> Evaluate(in Float3 direction) => color;

		public static explicit operator Pure(in Float3 color) => new(color);
		public static explicit operator Pure(in Float4 color) => new(color);
	}
}