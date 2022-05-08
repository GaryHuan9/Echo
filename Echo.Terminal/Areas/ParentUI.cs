using CodeHelpers.Mathematics;
using CodeHelpers.Packed;

namespace Echo.Terminal.Areas;

public class ParentUI : AreaUI
{
	public ParentUI()
	{
		_child0 = filled0;
		_child1 = filled1;
		ReorientChildren();
	}

	readonly FilledUI filled0 = new() { Filling = '0' };
	readonly FilledUI filled1 = new() { Filling = '1' };

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

		int axis = MajorAxis;
		int divider = Divider;
		int width = Width;

		for (int i = 0; i < width; i++) Domain[Int2.Create(axis, divider, i)] = 'o';

		_child0.Update();
		_child1.Update();
	}

	protected override void OnResize(Domain previous)
	{
		base.OnResize(previous);
		ReorientChildren();
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