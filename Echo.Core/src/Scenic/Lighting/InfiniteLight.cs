using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Scenic.Lighting;

public class InfiniteLight : LightEntity
{
	/// <summary>
	/// Invoked before rendering; after geometry and other lights are prepared.
	/// Can be used to initialize this infinite light to prepare it for rendering.
	/// </summary>
	public virtual void Prepare(PreparedScene scene) { }
}