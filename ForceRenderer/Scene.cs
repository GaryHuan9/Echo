using ForceRenderer.IO;
using ForceRenderer.Renderers;
using Object = ForceRenderer.Objects.Object;

namespace ForceRenderer.Scenes
{
	public class Scene : Object //If Scene derives from SceneObject, then we must exclude it when pressing the scene
	{
		public Cubemap Cubemap { get; set; }
	}
}