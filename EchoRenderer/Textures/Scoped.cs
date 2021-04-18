using System;

namespace EchoRenderer.Textures
{
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

	public readonly struct ScopedFilter : IDisposable
	{
		public ScopedFilter(Texture texture, IFilter filter)
		{
			this.texture = texture;

			original = texture.Filter;
			texture.Filter = filter;
		}

		readonly Texture texture;
		readonly IFilter original;

		public void Dispose() => texture.Filter = original;
	}
}