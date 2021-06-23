using System;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Textures
{
	/// <summary>
	/// A way to manipulate the input texture coordinate to a <see cref="Texture"/> to the acceptable bounds.
	/// </summary>
	public interface IWrapper
	{
		/// <summary>
		/// Converts a texture coordinate.
		/// </summary>
		Float2 Convert(Float2 uv);
	}

	/// <summary>
	/// A struct to temporarily change a <see cref="Texture.Wrapper"/>
	/// and reverts the change after <see cref="Dispose"/> is invoked
	/// </summary>
	public readonly struct ScopedWrapper : IDisposable
	{
		public ScopedWrapper(Texture texture, IWrapper wrapper)
		{
			this.texture = texture;

			original = texture.Wrapper;
			texture.Wrapper = wrapper;
		}

		readonly Texture texture;
		readonly IWrapper original;

		public void Dispose() => texture.Wrapper = original;
	}

	public static class Wrappers
	{
		public static readonly IWrapper clamp = new Clamp();
		public static readonly IWrapper repeat = new Repeat();
		public static readonly IWrapper unbound = new Unbound();

		class Clamp : IWrapper
		{
			public Float2 Convert(Float2 uv) => uv.Clamp(0f, 1f);
		}

		class Repeat : IWrapper
		{
			public Float2 Convert(Float2 uv) => uv.Repeat(1f);
		}

		class Unbound : IWrapper
		{
			public Float2 Convert(Float2 uv) => uv;
		}
	}
}