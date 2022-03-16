namespace EchoRenderer.Common.Mathematics;

public readonly struct Probable<T>
{
	public Probable(in T content, float pdf)
	{
		this.content = content;
		this.pdf = FastMath.Max0(pdf);
	}

	public readonly T content;
	public readonly float pdf;
}