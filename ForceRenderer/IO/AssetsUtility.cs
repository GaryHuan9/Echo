using System;
using System.IO;
using System.Reflection;

namespace ForceRenderer.IO
{
	public static class AssetsUtility
	{
		public static string WorkingDirectory { get; set; } = Directory.GetCurrentDirectory();

		/// <summary>
		/// Returns the absolute path to a path relative to the project folder.
		/// </summary>
		public static string GetAssetsPath(string path)
		{
			if (Path.IsPathFullyQualified(path)) return path;

			string[] parts = path.Split('/', '\\');
			string[] splits = new string[parts.Length + 1];

			splits[0] = WorkingDirectory;
			for (int i = 0; i < parts.Length; i++) splits[i + 1] = parts[i];

			return Path.GetFullPath(Path.Combine(splits));
		}
	}
}