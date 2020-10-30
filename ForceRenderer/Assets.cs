using System;
using System.Collections.Generic;
using System.IO;

namespace ForceRenderer
{
	public static class Assets
	{
		static Assets()
		{
			basePath = Directory.GetParent(Environment.CurrentDirectory).Parent?.Parent?.FullName;
			if (basePath == null) throw new Exception("Cannot find assets directory!");
		}

		public readonly static string basePath;

		public static string GetAssetsPath(string path, string extension)
		{
			string[] parts = path.Split('/', '\\');
			string[] splits = new string[parts.Length + 1];

			splits[0] = basePath;

			for (int i = 0; i < parts.Length; i++) splits[i + 1] = parts[i];
			return Path.ChangeExtension(Path.Combine(splits), extension);
		}
	}
}