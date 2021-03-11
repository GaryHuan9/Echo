using System;
using System.Runtime.Intrinsics;
using System.Threading.Tasks;
using CodeHelpers.Mathematics;

namespace ForceRenderer.Textures
{
	public abstract class Generative2D : Texture
	{
		protected Generative2D(Int2 size) : base(size) => inverseSize = 1f / size.MaxComponent;

		Vector128<float>[] pixels;

		public override ref Vector128<float> this[int index]
		{
			get
			{
				if (pixels != null) return ref pixels[index];
				throw new Exception("Must bake before retrieving pixel data!");
			}
		}

		public Float2 Tiling { get; set; } = Float2.one;
		public Float2 Offset { get; set; } = Float2.zero;

		protected virtual bool ParallelBaking => true;

		readonly float inverseSize; //Used to calculate tiling

		/// <summary>
		/// Calculate all of the pixel data into an internal storage of the provided <see cref="Texture.size"/>.
		/// NOTE: This method must be invoked before trying to access any pixel/color information.
		/// </summary>
		public void Bake()
		{
			pixels ??= new Vector128<float>[length];

			// @formatter:off

			if (ParallelBaking) Parallel.For(0, length, BakePixel);
			else for (int i = 0; i < length; i++) BakePixel(i);

			// @formatter:on
		}

		protected abstract Vector128<float> Sample(Float2 position);

		void BakePixel(int index)
		{
			Float2 position = ToPosition(index) + Float2.half;
			position = position * Tiling * inverseSize + Offset;

			pixels[index] = Sample(position);
		}
	}
}