using System.Collections.ObjectModel;
using System.Linq;
using Echo.Core.Common.Mathematics;

namespace Echo.Core.Common.Packed;

partial struct Float2
{
	public static Float2 Right => new(1f, 0f);
	public static Float2 Left => new(-1f, 0f);

	public static Float2 Up => new(0f, 1f);
	public static Float2 Down => new(0f, -1f);

	public static Float2 Zero => (Float2)0f;
	public static Float2 One => (Float2)1f;
	public static Float2 NegativeOne => (Float2)(-1f);

	public static Float2 Half => (Float2)0.5f;
	public static Float2 NegativeHalf => (Float2)(-0.5f);

	public static Float2 MaxValue => (Float2)float.MaxValue;
	public static Float2 MinValue => (Float2)float.MinValue;

	public static Float2 PositiveInfinity => (Float2)float.PositiveInfinity;
	public static Float2 NegativeInfinity => (Float2)float.NegativeInfinity;

	public static Float2 Epsilon => (Float2)Scalars.Epsilon;
	public static Float2 NaN => (Float2)float.NaN;
}

partial struct Float3
{
	public static Float3 Right => new(1f, 0f, 0f);
	public static Float3 Left => new(-1f, 0f, 0f);

	public static Float3 Up => new(0f, 1f, 0f);
	public static Float3 Down => new(0f, -1f, 0f);

	public static Float3 Forward => new(0f, 0f, 1f);
	public static Float3 Backward => new(0f, 0f, -1f);

	public static Float3 Zero => (Float3)0f;
	public static Float3 One => (Float3)1f;
	public static Float3 NegativeOne => (Float3)(-1f);

	public static Float3 Half => (Float3)0.5f;
	public static Float3 NegativeHalf => (Float3)(-0.5f);

	public static Float3 MaxValue => (Float3)float.MaxValue;
	public static Float3 MinValue => (Float3)float.MinValue;

	public static Float3 PositiveInfinity => (Float3)float.PositiveInfinity;
	public static Float3 NegativeInfinity => (Float3)float.NegativeInfinity;

	public static Float3 Epsilon => (Float3)Scalars.Epsilon;
	public static Float3 NaN => (Float3)float.NaN;
}

partial struct Float4
{
	public static Float4 Right => new(1f, 0f, 0f, 0f);
	public static Float4 Left => new(-1f, 0f, 0f, 0f);

	public static Float4 Up => new(0f, 1f, 0f, 0f);
	public static Float4 Down => new(0f, -1f, 0f, 0f);

	public static Float4 Forward => new(0f, 0f, 1f, 0f);
	public static Float4 Backward => new(0f, 0f, -1f, 0f);

	public static Float4 Ana => new(0f, 0f, 0f, 1f);
	public static Float4 Kata => new(0f, 0f, 0f, -1f);

	public static Float4 Zero => (Float4)0f;
	public static Float4 One => (Float4)1f;
	public static Float4 NegativeOne => (Float4)(-1f);

	public static Float4 Half => (Float4)0.5f;
	public static Float4 NegativeHalf => (Float4)(-0.5f);

	public static Float4 MaxValue => (Float4)float.MaxValue;
	public static Float4 MinValue => (Float4)float.MinValue;

	public static Float4 PositiveInfinity => (Float4)float.PositiveInfinity;
	public static Float4 NegativeInfinity => (Float4)float.NegativeInfinity;

	public static Float4 Epsilon => (Float4)Scalars.Epsilon;
	public static Float4 NaN => (Float4)float.NaN;
}

partial struct Int2
{
	public static Int2 Right => new(1, 0);
	public static Int2 Left => new(-1, 0);

	public static Int2 Up => new(0, 1);
	public static Int2 Down => new(0, -1);

	public static Int2 Zero => (Int2)0;
	public static Int2 One => (Int2)1;
	public static Int2 NegativeOne => (Int2)(-1);

	public static Int2 MaxValue => (Int2)int.MaxValue;
	public static Int2 MinValue => (Int2)int.MinValue;

	public static readonly ReadOnlyCollection<Int2> units4 = new
	(
		new[]
		{
			new Int2(0, 0), new Int2(1, 0),
			new Int2(0, 1), new Int2(1, 1)
		}
	);

	public static readonly ReadOnlyCollection<Int2> edges4 = new
	(
		new[]
		{
			new Int2(1, 0), new Int2(0, 1),
			new Int2(-1, 0), new Int2(0, -1)
		}
	);

	public static readonly ReadOnlyCollection<Int2> vertices4 = new
	(
		new[]
		{
			new Int2(1, 1), new Int2(-1, 1),
			new Int2(-1, -1), new Int2(1, -1)
		}
	);

	public static readonly ReadOnlyCollection<Int2> edgesVertices8 = new(edges4.Concat(vertices4).ToArray());
}

partial struct Int3
{
	public static Int3 Right => new(1, 0, 0);
	public static Int3 Left => new(-1, 0, 0);

	public static Int3 Up => new(0, 1, 0);
	public static Int3 Down => new(0, -1, 0);

	public static Int3 Forward => new(0, 0, 1);
	public static Int3 Backward => new(0, 0, -1);

	public static Int3 Zero => (Int3)0;
	public static Int3 One => (Int3)1;
	public static Int3 NegativeOne => (Int3)(-1);

	public static Int3 MaxValue => (Int3)int.MaxValue;
	public static Int3 MinValue => (Int3)int.MinValue;

	public static readonly ReadOnlyCollection<Int3> units8 = new
	(
		new[]
		{
			new Int3(0, 0, 0), new Int3(1, 0, 0), new Int3(0, 0, 1), new Int3(1, 0, 1),
			new Int3(0, 1, 0), new Int3(1, 1, 0), new Int3(0, 1, 1), new Int3(1, 1, 1)
		}
	);

	public static readonly ReadOnlyCollection<Int3> faces6 = new
	(
		new[]
		{
			new Int3(1, 0, 0), new Int3(-1, 0, 0),
			new Int3(0, 1, 0), new Int3(0, -1, 0),
			new Int3(0, 0, 1), new Int3(0, 0, -1)
		}
	);

	public static readonly ReadOnlyCollection<Int3> vertices8 = new
	(
		new[]
		{
			new Int3(1, 1, 1), new Int3(-1, 1, 1), new Int3(1, 1, -1), new Int3(-1, 1, -1),
			new Int3(1, -1, 1), new Int3(-1, -1, 1), new Int3(1, -1, -1), new Int3(-1, -1, -1)
		}
	);

	public static readonly ReadOnlyCollection<Int3> edges12 = new
	(
		new[]
		{
			new Int3(1, 1, 0), new Int3(0, 1, 1), new Int3(-1, 1, 0), new Int3(0, 1, -1),
			new Int3(1, 0, 1), new Int3(-1, 0, 1), new Int3(-1, 0, -1), new Int3(1, 0, -1),
			new Int3(1, -1, 0), new Int3(0, -1, 1), new Int3(-1, -1, 0), new Int3(0, -1, -1)
		}
	);

	public static readonly ReadOnlyCollection<Int3> facesVertices14 = new(faces6.Concat(vertices8).ToArray());
	public static readonly ReadOnlyCollection<Int3> facesEdges18 = new(faces6.Concat(edges12).ToArray());
	public static readonly ReadOnlyCollection<Int3> verticesEdges20 = new(vertices8.Concat(edges12).ToArray());

	public static readonly ReadOnlyCollection<Int3> facesVerticesEdges26 = new(faces6.Concat(vertices8).Concat(edges12).ToArray());
}

partial struct Int4
{
	public static Int4 Right { get; } = new(1, 0, 0, 0);
	public static Int4 Left { get; } = new(-1, 0, 0, 0);

	public static Int4 Up { get; } = new(0, 1, 0, 0);
	public static Int4 Down { get; } = new(0, -1, 0, 0);

	public static Int4 Forward { get; } = new(0, 0, 1, 0);
	public static Int4 Backward { get; } = new(0, 0, -1, 0);

	public static Int4 Ana { get; } = new(0, 0, 0, 1);
	public static Int4 Kata { get; } = new(0, 0, 0, -1);

	public static Int4 Zero => (Int4)0;
	public static Int4 One => (Int4)1;
	public static Int4 NegativeOne => (Int4)(-1);

	public static Int4 MaxValue => (Int4)int.MaxValue;
	public static Int4 MinValue => (Int4)int.MinValue;
}