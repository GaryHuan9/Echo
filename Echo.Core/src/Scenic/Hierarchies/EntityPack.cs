using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;

namespace Echo.Core.Scenic.Hierarchies;

/// <summary>
/// The root of any <see cref="Entity"/> hierarchy.
/// </summary>
public class EntityPack : Entity
{
	readonly Dictionary<EntityPack, ulong> directInstances = new();
	readonly HashSet<EntityPack> instanceParents = new();
	readonly HashSet<EntityPack> allInstances = new();
	bool instancesDirty;

	public override Float3 Position
	{
		set
		{
			if (value.EqualsExact(Position)) return;
			throw ModifyTransformException();
		}
	}

	public override Float3 Rotation
	{
		set
		{
			if (value.EqualsExact(Rotation)) return;
			throw ModifyTransformException();
		}
	}

	public override float Scale
	{
		set
		{
			if (value.Equals(Scale)) return;
			throw ModifyTransformException();
		}
	}

	/// <summary>
	/// The <see cref="EntityPack"/>s that are being directly instanced by this <see cref="EntityPack"/>.
	/// </summary>
	public IReadOnlyCollection<EntityPack> DirectInstances => directInstances.Keys;

	/// <summary>
	/// The <see cref="EntityPack"/>s that are directly instancing this <see cref="EntityPack"/>.
	/// </summary>
	public IReadOnlySet<EntityPack> InstanceParents => instanceParents;

	/// <summary>
	/// All of the <see cref="EntityPack"/>s instanced by this <see cref="EntityPack"/>, both
	/// directly and indirectly (through other directly instanced <see cref="EntityPack"/>s).
	/// </summary>
	public IReadOnlySet<EntityPack> AllInstances
	{
		get
		{
			RecalculateInstances();
			return allInstances;
		}
	}

	protected sealed override void CheckParent(Entity parent) => throw new SceneException($"Cannot give an {nameof(EntityPack)} a {nameof(Parent)}.");
	protected sealed override void CheckRoot(EntityPack root) => throw new SceneException($"Cannot give an {nameof(EntityPack)} a {nameof(Root)}.");

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
		if (exists) return;

		//If we just added a completely new directly instanced EntityPack, then we
		//must dirty our all instances collection so it can be recalculated again

		DirtyInstances();
		pack.instanceParents.Add(this);
	}

	internal void RemoveInstance(EntityPack pack)
	{
		ref ulong count = ref CollectionsMarshal.GetValueRefOrNullRef(directInstances, pack);
		if (Unsafe.IsNullRef(ref count)) throw new ArgumentException("Count mismatch.", nameof(pack));

		Ensure.IsTrue(count > 0);
		if (--count > 0) return;

		//If we just completely removed a directly instanced EntityPack, then we
		//must dirty our all instances collection so it can be recalculated again

		DirtyInstances();
		directInstances.Remove(pack);
		pack.instanceParents.Remove(this);
	}

	void RecalculateInstances()
	{
		if (!instancesDirty) return;

		instancesDirty = false;
		allInstances.Clear();

		foreach (EntityPack pack in DirectInstances)
		{
			allInstances.Add(pack);
			allInstances.UnionWith(pack.AllInstances);
		}
	}

	void DirtyInstances()
	{
		if (instancesDirty) return;
		instancesDirty = true;

		foreach (EntityPack parent in instanceParents) parent.DirtyInstances();
	}

	static SceneException ModifyTransformException() => new($"Cannot modify the transform of an {nameof(EntityPack)}.");
}