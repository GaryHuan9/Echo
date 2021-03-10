using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;

namespace ForceRenderer.Textures
{
	public class Simplex2D : Texture
	{
		public Simplex2D(Int2 size) : base(size) { }

		public override Vector128<float> this[int index]
		{
			get => throw new System.NotImplementedException();
			set => throw new System.NotImplementedException();
		}
	}
}