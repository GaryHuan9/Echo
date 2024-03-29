﻿using System;
using System.Collections.Generic;
using System.Linq;
using Echo.Core.Scenic.Hierarchies;
using NUnit.Framework;

namespace Echo.UnitTests.Scenic;

[TestFixture]
public class EntityPackTests
{
	[Test]
	public void Simple0()
	{
		var scene = new Scene();
		Check(scene);

		var pack = new EntityPack();
		Check(pack);

		scene.Add(new PackInstance { Pack = pack });
		Check(scene, new[] { pack });
	}

	[Test]
	public void Simple1()
	{
		var pack = new EntityPack();
		var scene = new Scene { new() { new PackInstance { Pack = pack } } };
		Check(scene, new[] { pack });

		var instance = new PackInstance();
		scene = new Scene { instance };
		instance.Pack = pack;
		Check(scene, new[] { pack });

		instance.Pack = null;
		Check(scene);
	}

	[Test]
	public void Simple2()
	{
		var pack = new EntityPack();
		var scene = new Scene();
		Entity current = scene;

		for (int i = 0; i < 10; i++) current.Add(current = new PackInstance { Pack = pack });
		Check(scene, new[] { pack });
		Check(pack);
	}

	[Test]
	public void Simple3()
	{
		var pack0 = new EntityPack();
		var pack1 = new EntityPack();

		var scene = new Scene
		{
			new PackInstance { Pack = pack0 },
			new PackInstance { Pack = pack0 },
			new PackInstance { Pack = pack0 },
			new PackInstance { Pack = pack1 }
		};

		Check(scene, new[] { pack0, pack1 });
	}

	[Test]
	public void Complex0()
	{
		var pack0 = new EntityPack();
		var pack1 = new EntityPack();

		var scene = new Scene
		{
			new PackInstance { Pack = pack0 },
			new() { new PackInstance { Pack = pack1 } },
			new() { new Entity { new PackInstance { Pack = pack0 } } }
		};

		Check(scene, new[] { pack0, pack1 });

		Check(pack0);
		Check(pack1);

		var instance = new PackInstance();
		pack0.Add(instance);
		instance.Pack = pack1;

		Check(scene, new[] { pack0, pack1 });
		Check(pack0, new[] { pack1 });

		var pack2 = new EntityPack();
		pack0.Add(new PackInstance { Pack = pack2 });

		Check(scene, new[] { pack0, pack1 }, new[] { pack2 });
		Check(pack0, new[] { pack1, pack2 });
		Check(pack2);

		pack2.Add(new PackInstance { Pack = pack1 });
		Check(scene, new[] { pack0, pack1 }, new[] { pack2 });
		Check(pack0, new[] { pack1, pack2 });
		Check(pack2, new[] { pack1 });

		instance.Pack = null;

		Check(scene, new[] { pack0, pack1 }, new[] { pack2 });
		Check(pack0, new[] { pack2 }, new[] { pack1 });
		Check(pack2, new[] { pack1 });
		Check(pack1);

		scene = new Scene { new PackInstance { Pack = pack2 } };
		Check(scene, new[] { pack2 }, new[] { pack1 });

		scene.Add(new Entity { new PackInstance { Pack = pack1 } });
		scene.Add(new Entity { new PackInstance { Pack = pack2 } });
		Check(scene, new[] { pack1, pack2 });
		Check(pack2, new[] { pack1 });
		Check(pack1);
	}

	[Test]
	public void Complex1()
	{
		var pack0 = new EntityPack();
		var pack1 = new EntityPack { new PackInstance { Pack = pack0 } };
		var pack2 = new EntityPack { new PackInstance { Pack = pack1 } };

		var pack3 = new EntityPack
		{
			new PackInstance { Pack = pack0 },
			new PackInstance { Pack = pack1 }
		};

		var instance1 = new PackInstance { Pack = pack1 };
		var instance2 = new PackInstance { Pack = pack2 };
		var instance3 = new PackInstance { Pack = pack3 };

		var scene = new Scene
		{
			instance1,
			instance2,
			instance3
		};

		Check(pack0);
		Check(pack1, new[] { pack0 });
		Check(pack2, new[] { pack1 }, new[] { pack0 });
		Check(pack3, new[] { pack0, pack1 });
		Check(scene, new[] { pack1, pack2, pack3 }, new[] { pack0 });

		var pack4 = new EntityPack();
		pack0.Add(new PackInstance { Pack = pack4 });

		Check(pack0, new[] { pack4 });
		Check(pack1, new[] { pack0 }, new[] { pack4 });
		Check(pack2, new[] { pack1 }, new[] { pack0, pack4 });
		Check(pack3, new[] { pack0, pack1 }, new[] { pack4 });
		Check(scene, new[] { pack1, pack2, pack3 }, new[] { pack0, pack4 });

		instance1.Pack = null;
		instance2.Pack = null;

		Check(scene, new[] { pack3 }, new[] { pack0, pack1, pack4 });

		instance2.Pack = pack2;
		instance3.Pack = pack2;

		Check(scene, new[] { pack2 }, new[] { pack0, pack1, pack4 });

		instance2.Pack = null;
		scene.Add(new PackInstance { Pack = pack0 });
		instance3.Pack = pack0;

		Check(scene, new[] { pack0 }, new[] { pack4 });
	}

	[Test]
	public void Recursive0()
	{
		var pack = new EntityPack();
		Check(pack);

		CheckRecursive(() => pack.Add(new PackInstance { Pack = pack }));
	}

	[Test]
	public void Recursive1()
	{
		var pack = new EntityPack();
		var instance = new PackInstance();

		pack.Add(instance);
		CheckRecursive(() => instance.Pack = pack);
	}

	[Test]
	public void Recursive2()
	{
		var pack0 = new EntityPack();
		var pack1 = new EntityPack();
		var pack2 = new EntityPack();
		var pack3 = new EntityPack();

		pack0.Add(new PackInstance { Pack = pack1 });
		pack1.Add(new PackInstance { Pack = pack2 });
		pack2.Add(new PackInstance { Pack = pack3 });

		CheckRecursive(() => pack3.Add(new PackInstance { Pack = pack0 }));
	}

	[Test]
	public void Recursive3()
	{
		var pack0 = new EntityPack();
		var pack1 = new EntityPack();

		var pack2 = new EntityPack
		{
			new() { new Entity { new PackInstance { Pack = pack0 } } }
		};

		var instance = new PackInstance();
		pack0.Add(instance);
		instance.Pack = pack1;

		instance = new PackInstance();
		var pack3 = new EntityPack { instance, new PackInstance { Pack = pack2 } };
		instance.Pack = pack0;

		CheckRecursive(() => pack1.Add(new PackInstance { Pack = pack3 }));
	}

	[Test]
	public void Grid([Values(1, 2, 4)] int depth, [Values(0, 1, 2, 7, 300)] int width)
	{
		var scene = new Scene();
		EntityPack previous = scene;
		List<EntityPack> packs = new();

		for (int y = 0; y < depth; y++)
		{
			var pack = new EntityPack();

			for (int x = 0; x < width; x++) previous.Add(new PackInstance { Pack = pack });

			previous = pack;
			packs.Add(pack);
		}

		if (width > 0)
		{
			EntityPack pack = scene;

			for (int i = 0; i < depth; i++)
			{
				Check(pack, new[] { packs[i] }, packs.Skip(i));
				pack = pack.DirectInstances.Single();
			}

			Check(pack);
		}
		else
		{
			Check(scene);

			foreach (EntityPack pack in packs) Check(pack);
		}
	}

	static void Check(EntityPack pack, IEnumerable<EntityPack> direct = null, IEnumerable<EntityPack> indirect = null)
	{
		direct ??= Enumerable.Empty<EntityPack>();
		indirect ??= Enumerable.Empty<EntityPack>();

		var set = new HashSet<EntityPack>(direct);

		Assert.That(pack.DirectInstances, Is.EquivalentTo(set));
		Assert.That(set, Has.All.Matches<EntityPack>(child => child.InstanceParents.Contains(pack)));

		set.UnionWith(indirect);
		Assert.That(pack.AllInstances, Is.EquivalentTo(set));
	}

	static void CheckRecursive(Action action)
	{
		try
		{
			action();
		}
		catch (SceneException exception)
		{
			if (exception.Message.Contains("recurs", StringComparison.InvariantCultureIgnoreCase)) return;
			Assert.Fail();
		}
	}
}