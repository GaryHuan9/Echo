using Echo.Core.InOut.EchoDescription;
using Echo.Core.Scenic.Hierarchies;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic.Lights;

/// <summary>
/// An <see cref="Entity"/> that produces light and emission.
/// </summary>
/// <remarks>Classes that inherit from this class usually either implement the <see cref="IPreparedLight"/>
/// interface or directly inherit from the <see cref="InfiniteLight"/> class.</remarks>
public abstract class LightEntity : Entity
{
	/// <summary>
	/// The main color and intensity of this <see cref="LightEntity"/>.
	/// </summary>
	[EchoSourceUsable]
	public RGB128 Intensity { get; set; } = RGB128.White;
}