namespace EchoRenderer.Common.Coloring;

public interface IColor
{
	/// <summary>
	/// Returns this <see cref="IColor"/> converted as an <see cref="RGBA128"/>.
	/// </summary>
	RGBA128 ToRGBA128();
}