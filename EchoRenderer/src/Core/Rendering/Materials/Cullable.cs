using CodeHelpers;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;

namespace EchoRenderer.Core.Rendering.Materials;

public class Cullable : Material
{
	NotNull<Material> _base = Invisible.instance;

	public Material Base
	{
		get => _base;
		set => _base = value;
	}

	public bool Backface { get; set; } = true;

	public override void Prepare()
	{
		base.Prepare();
		Base.Prepare();
	}

	public override void Scatter(ref Touch touch, Allocator allocator)
	{
		bool back = !FastMath.Positive(touch.outgoing.Dot(touch.point.normal));
		(back == Backface ? Invisible.instance : Base).Scatter(ref touch, allocator);
	}
}