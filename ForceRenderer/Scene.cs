using System;
using System.Collections.Generic;
using CodeHelpers.Vectors;

namespace ForceRenderer
{
	public class Scene
	{
		List<SceneObject> sceneObjects = new List<SceneObject>();

		public void AddSceneObject(SceneObject sceneObject)
		{
			sceneObjects.Add(sceneObject);
		}

		public float SignedDistance(Float3 point)
		{
			float distance = float.MaxValue;

			for (int i = 0; i < sceneObjects.Count; i++)
			{
				float local = sceneObjects[i].SignedDistance(point);
				distance = Math.Min(distance, local);
			}

			return distance;
		}
	}
}