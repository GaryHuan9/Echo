// ReSharper disable ParameterHidesMember

namespace Echo.Core.Common.Packed;

partial struct Float2
{
	public void Deconstruct(out float X, out float Y)
	{
		X = this.X;
		Y = this.Y;
	}
}

partial struct Float3
{
	public void Deconstruct(out float X, out float Y, out float Z)
	{
		X = this.X;
		Y = this.Y;
		Z = this.Z;
	}
}

partial struct Float4
{
	public void Deconstruct(out float X, out float Y, out float Z, out float W)
	{
		X = this.X;
		Y = this.Y;
		Z = this.Z;
		W = this.W;
	}
}

partial struct Int2
{
	public void Deconstruct(out int X, out int Y)
	{
		X = this.X;
		Y = this.Y;
	}
}

partial struct Int3
{
	public void Deconstruct(out int X, out int Y, out int Z)
	{
		X = this.X;
		Y = this.Y;
		Z = this.Z;
	}
}

partial struct Int4
{
	public void Deconstruct(out int X, out int Y, out int Z, out int W)
	{
		X = this.X;
		Y = this.Y;
		Z = this.Z;
		W = this.W;
	}
}

// ReSharper restore ParameterHidesMember