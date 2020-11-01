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

		/// <summary>
		/// Returns the absolute path to a path relative to the project folder.
		/// </summary>
		public static string GetAssetsPath(string path)
		{
			string[] parts = path.Split('/', '\\');
			string[] splits = new string[parts.Length + 1];

			for (int i = 0; i < parts.Length; i++) splits[i + 1] = parts[i];

			splits[0] = basePath;
			return Path.Combine(splits);
		}
	}
}