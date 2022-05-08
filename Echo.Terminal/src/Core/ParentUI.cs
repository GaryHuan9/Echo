using CodeHelpers.Mathematics;
using CodeHelpers.Packed;

namespace Echo.Terminal.Core;

public class ParentUI : AreaUI
{
	public ParentUI()
	{
		_child0 = filled0;
		_child1 = filled1;
		ReorientChildren();
	}

	readonly FilledUI filled0 = new();
	readonly FilledUI filled1 = new();

	bool _horizontal;
	float _division = 0.5f;

	public bool Horizontal
	{
		get => _horizontal;
		set
		{
			if (_horizontal == value) return;
			_horizontal = value;
			ReorientChildren();
		}
	}

	public float Division
	{
		get => _division;
		set
		{
			if (_division.AlmostEquals(value)) return;
			_division = value;
			ReorientChildren();
		}
	}

	AreaUI _child0;
	AreaUI _child1;

	public AreaUI Child0
	{
		get => _child0 == filled0 ? null : _child0;
		set
		{
			if (Child0 == value) return;
			_child0 = value ?? filled0;
			ReorientChildren();
		}
	}

	public AreaUI Child1
	{
		get => _child1 == filled1 ? null : _child1;
		set
		{
			if (Child1 == value) return;
			_child1 = value ?? filled1;
			ReorientChildren();
		}
	}

	int MajorAxis => Horizontal ? 0 : 1;
	int MinorAxis => Horizontal ? 1 : 0;
	int Divider => (Domain.size[MajorAxis] * Division).Floor();
	int Width => Domain.size[MinorAxis];

	public override void Update()
	{
		if (Domain.size.MinComponent < 1) return;

		DrawDivider();

		_child0.Update();
		_child1.Update();
	}

	protected override void OnResize(Domain previous)
	{
		base.OnResize(previous);
		ReorientChildren();
	}

	void DrawDivider()
	{
		if (Horizontal)
		{
			int divider = Divider;
			int height = Domain.size.Y;

			for (int y = 0; y < height; y++) Domain[new Int2(divider, y)] = '\u2502';
		}
		else Domain.FillLine(Divider, '\u2500');

		//TODO: connect vertical and horizontal divisors
	}

	void ReorientChildren()
	{
		int axis = MajorAxis;
		Domain domain = Domain;

		if (domain.size[axis] > 1)
		{
			Int2 max0 = Int2.Create(axis, Divider, Width);
			Int2 min1 = Int2.Create(axis, max0[axis] + 1);

			_child0.Domain = domain[Int2.Zero, max0];
			_child1.Domain = domain[min1, domain.size];
		}
		else
		{
			_child0.Domain = domain[Int2.Zero, Int2.Zero];
			_child1.Domain = domain[Int2.Zero, Int2.Zero];
		}
	}
}