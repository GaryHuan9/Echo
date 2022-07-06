using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;

namespace Echo.Core.Scenic.Hierarchies;

/// <summary>
/// The root of any <see cref="Entity"/> hierarchy.
/// </summary>
public class EntityPack : Entity
{
	public EntityPack() => SetRoot(this);

	readonly HashSet<EntityPack> allInstances = new();
	readonly Dictionary<EntityPack, ulong> directInstances = new();
	bool allInstancesDirty;

	public IReadOnlySet<EntityPack> AllInstances
	{
		get
		{
			RecalculateInstances();
			return allInstances;
		}
	}

	public IReadOnlyCollection<EntityPack> DirectInstances => directInstances.Keys;

	public override Float3 Position
	{
		set
		{
			if (value.EqualsExact(Position)) return;
			ThrowModifyTransformException();
		}
	}

	public override Float3 Rotation
	{
		set
		{
			if (value.EqualsExact(Rotation)) return;
			ThrowModifyTransformException();
		}
	}

	public override float Scale
	{
		set
		{
			if (value.Equals(Scale)) return;
			ThrowModifyTransformException();
		}
	}

	/// <summary>
	/// The maximum number of instanced layers allowed (excluding the root).
	/// Can be increased if needed at a performance and stack memory penalty.
	/// </summary>
	public const int MaxLayer = 5;

	protected sealed override bool CanAddParent(Entity parent) => false;

	protected sealed override bool CanAddRoot(EntityPack root) => root == this;

	protected override void AddImpl(Entity child)
	{
		base.AddImpl(child);

		foreach (Entity entity in child.LoopChildren(true, true))
		{
			if (entity is not PackInstance instance) continue;
			if (instance.Pack != null) AddInstance(instance.Pack);
		}
	}

	internal void AddInstance(EntityPack pack)
	{
		ref ulong count = ref CollectionsMarshal.GetValueRefOrAddDefault(directInstances, pack, out bool exists);
		++count;

		//If we just added a completely new directly instanced EntityPack, then we
		//must dirty our all instances collection so it can be recalculated again

		allInstancesDirty |= !exists;
	}

	internal void RemoveInstance(EntityPack pack)
	{
		ref ulong count = ref CollectionsMarshal.GetValueRefOrNullRef(directInstances, pack);
		if (Unsafe.IsNullRef(ref count)) throw new ArgumentException("Count mismatch.", nameof(pack));

		Assert.IsTrue(count > 0);
		if (--count > 0) return;

		//If we just completely removed a directly instanced EntityPack, then we
		//must dirty our all instances collection so it can be recalculated again

		allInstancesDirty = true;
		directInstances.Remove(pack);
	}

	void RecalculateInstances()
	{
		if (!allInstancesDirty) return;

		allInstancesDirty = false;
		allInstances.Clear();

		foreach (EntityPack pack in DirectInstances)
		{
			allInstances.Add(pack);
			allInstances.UnionWith(pack.AllInstances);
		}
	}

	static void ThrowModifyTransformException() => throw new Exception($"Cannot modify {nameof(EntityPack)} transform!");
}