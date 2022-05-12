using System;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Terminal.Core.Display;

namespace Echo.Terminal.Core.Interface;

public abstract class ParentTI : AreaTI
{
	protected ParentTI()
	{
		_child0 = filled0;
		_child1 = filled1;
		ReorientChildren();
	}

	readonly FilledTI filled0 = new();
	readonly FilledTI filled1 = new();

	Int2 dividerMin;
	Int2 dividerMax;

	bool _horizontal;
	float _balance = 0.5f;
	int _dividerSize;

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

	public float Balance
	{
		get => _balance;
		set
		{
			if (_balance.AlmostEquals(value)) return;
			if (value is < 0f or > 1f) throw new ArgumentOutOfRangeException(nameof(value));

			_balance = value;
			ReorientChildren();
		}
	}

	public int DividerSize
	{
		get => _dividerSize;
		set
		{
			if (_dividerSize == value) return;
			if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));

			_dividerSize = value;
			ReorientChildren();
		}
	}

	AreaTI _child0;
	AreaTI _child1;

	public AreaTI Child0
	{
		get => _child0 == filled0 ? null : _child0;
		set
		{
			if (Child0 == value) return;
			_child0 = value ?? filled0;
			ReorientChildren();
		}
	}

	public AreaTI Child1
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

	public override void Draw(in Domain domain)
	{
		if (IsZeroSize) return;

		Paint(domain.MakePainter(dividerMin, dividerMax, InvertY));

		_child0.Draw(domain);
		_child1.Draw(domain);
	}

	protected override void Reorient()
	{
		base.Reorient();
		ReorientChildren();
	}

	void ReorientChildren()
	{
		int majorAxis = MajorAxis;
		int minorAxis = MinorAxis;
		int remain = (Max - Min)[majorAxis] - DividerSize;

		if (remain > 0)
		{
			int divider = (remain * Balance).Round() + Min[majorAxis];
			Int2 max0 = Int2.Create(minorAxis, Max[minorAxis], divider);
			Int2 min1 = Int2.Create(minorAxis, Min[minorAxis], divider + DividerSize);

			dividerMin = max0.Min(min1);
			dividerMax = max0.Max(min1);

			_child0.SetTransform(Min, max0);
			_child1.SetTransform(min1, Max);
		}
		else
		{
			dividerMin = Min;
			dividerMax = Max;

			_child0.SetTransform(Min, Min);
			_child1.SetTransform(Max, Max);
		}
	}
}