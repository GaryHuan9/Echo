using System.Runtime.Intrinsics;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Textures.DimensionTwo
{
	public class Crop2D : Texture2D
	{
		/// <summary>
		/// Creates the rectangular cropped reference of <paramref name="source"/>.
		/// <paramref name="min"/> is inclusive and <paramref name="max"/> is exclusive.
		/// </summary>
		public Crop2D(Texture2D source, Int2 min, Int2 max) : base(max - min, source.Filter)
		{
			this.source = source;
			this.min = min;

			Assert.IsTrue(max > this.min);
		}

		readonly Texture2D source;
		readonly Int2 min;

		public override Vector128<float> this[Int2 position]
		{
			get => source[min + position];
			set => source[min + position] = value;
		}
	}
}