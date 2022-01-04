using System;
using System.Runtime.Intrinsics;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Textures.Grid
{
	/// <summary>
	/// A <see cref="Texture"/> only defined on integer positions and bounded between zero (inclusive) and <see cref="size"/> (exclusive).
	/// </summary>
	public abstract partial class TextureGrid : Texture
	{
		protected TextureGrid(Int2 size, IFilter filter) : base(Wrappers.repeat)
		{
			this.size = size;
			oneLess = size - Int2.one;

			aspect = (float)size.x / size.y;
			Filter = filter;
		}

		public readonly Int2 size;
		public readonly Int2 oneLess;

		public readonly float aspect; //Width over height

		IFilter _filter;

		public IFilter Filter
		{
			get => _filter;
			set => _filter = value ?? throw ExceptionHelper.Invalid(nameof(value), InvalidType.isNull);
		}

		/// <summary>
		/// Returns the average of the two <see cref="size"/> axes.
		/// We use an logarithmic equation since the average is nicer.
		/// </summary>
		public float LogSize
		{
			get
			{
				float logWidth = MathF.Log(size.x);
				float logHeight = MathF.Log(size.y);

				return MathF.Exp((logWidth + logHeight) / 2f);
			}
		}

		/// <summary>
		/// Gets and sets the <see cref="Vector128{T}"/> pixel data at a specific <paramref name="position"/>.
		/// The input <paramref name="position"/> must be between zero and <see cref="oneLess"/> (both inclusive).
		/// </summary>
		public abstract Vector128<float> this[Int2 position] { get; set; }

		protected sealed override Vector128<float> Evaluate(Float2 uv) => Filter.Convert(this, uv);

		/// <summary>
		/// Converts texture coordinate <paramref name="uv"/> to a integer position based on this <see cref="TextureGrid.size"/>.
		/// </summary>
		public Int2 ToPosition(Float2 uv) => (uv * size).Floored.Clamp(Int2.zero, oneLess);

		/// <summary>
		/// Converts a pixel integer <paramref name="position"/> to this <see cref="TextureGrid"/>'s texture coordinate.
		/// </summary>
		public Float2 ToUV(Int2 position) => (position + Float2.half) / size;

		/// <summary>
		/// Copies the data of a <see cref="Texture"/> of the same size pixel by pixel.
		/// An exception will be thrown if the sizes mismatch.
		/// </summary>
		public virtual void CopyFrom(Texture texture, bool parallel = true)
		{
			if (texture is TextureGrid textureGrid)
			{
				AssertAlignedSize(textureGrid);
				ForEach(position => this[position] = textureGrid[position], parallel);
			}
			else ForEach(position => this[position] = texture[ToUV(position)], parallel);
		}

		/// <summary>
		/// Enumerates through all pixels on <see cref="Texture"/> and invoke <paramref name="action"/>.
		/// </summary>
		public virtual void ForEach(Action<Int2> action, bool parallel = true)
		{
			if (parallel) Parallel.ForEach(size.Loop(), action);
			else
			{
				foreach (Int2 position in size.Loop()) action(position);
			}
		}

		protected void AssertAlignedSize(TextureGrid texture)
		{
			if (texture.size == size) return;
			throw ExceptionHelper.Invalid(nameof(texture), texture, "has a mismatched size!");
		}

		public override string ToString() => $"{base.ToString()} with size {size}";
	}
}