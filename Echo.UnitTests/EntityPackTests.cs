using System.Collections.Generic;
using System.Linq;
using Echo.Core.Scenic.Hierarchies;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace Echo.UnitTests;

[TestFixture]
public class EntityPackTests
{
	[Test]
	public void Simple0()
	{
		var scene = new Scene();
		CheckInstances(scene, Is.Empty);

		var pack = new EntityPack();
		CheckInstances(pack, Is.Empty);

		Assert.That(pack.DirectInstances, Is.Empty);
		Assert.That(pack.AllInstances, Is.Empty);

		scene.Add(new PackInstance { Pack = pack });
		Check();

		scene.Add(new Entity { new PackInstance { Pack = pack } });
		Check();

		Entity current = scene;
		for (int i = 0; i < 10; i++) current.Add(current = new PackInstance { Pack = pack });
		Check();

		void Check() => CheckInstances(scene, Has.Count.EqualTo(1).And.All.EqualTo(pack));
	}

	[Test]
	public void Simple1()
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
		Check();

		scene = new Scene
		{
			new PackInstance { Pack = pack0 },
			new() { new PackInstance { Pack = pack1 } },
			new() { new Entity { new PackInstance { Pack = pack0 } } }
		};
		Check();

		CheckInstances(pack0, Is.Empty);
		CheckInstances(pack1, Is.Empty);

		pack1.Add(new PackInstance { Pack = pack0 });

		scene = new Scene { new PackInstance { Pack = pack1 } };
		Assert.That(scene.DirectInstances.Single(), Is.EqualTo(pack1));
		Assert.That(scene.AllInstances, Has.Count.EqualTo(2));
		Assert.That(scene.AllInstances, Has.Member(pack0).And.Member(pack1));

		scene.Add(new Entity { new PackInstance { Pack = pack0 } });
		scene.Add(new Entity { new PackInstance { Pack = pack1 } });
		Check();

		CheckInstances(pack0, Is.Empty);
		CheckInstances(pack1, Has.Count.EqualTo(1).And.All.EqualTo(pack0));

		void Check() => CheckInstances(scene, Has.Count.EqualTo(2).And.Contains(pack0).And.Contains(pack1));
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
			Assert.That(scene.DirectInstances.Single(), Is.EqualTo(packs[0]));
			Assert.That(scene.AllInstances, Is.EquivalentTo(packs));
		}
		else CheckInstances(scene, Is.Empty);
	}

	static void CheckInstances(EntityPack pack, IResolveConstraint constraint)
	{
		Assert.That(pack.DirectInstances, constraint);
		Assert.That(pack.AllInstances, constraint);
	}
}