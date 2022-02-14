namespace EchoRenderer.Common;

public interface IProfile
{
	/// <summary>
	/// Can be invoked to authenticate the validity of this <see cref="IProfile"/>.
	/// </summary>
	void Validate();
}