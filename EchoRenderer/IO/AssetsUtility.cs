using System;
using System.IO;
using System.Linq;

namespace EchoRenderer.IO
{
	public static class AssetsUtility
	{
		static AssetsUtility()
		{
			var locations = new[] {Environment.CurrentDirectory, AppContext.BaseDirectory};
			workingDirectory = locations.First(path => Directory.Exists(Path.Combine(path, "Assets")));
		}

		public static readonly string workingDirectory;

		/// <summary>
		/// Returns the absolute path to a path relative to the project folder.
		/// </summary>
		public static string GetAssetsPath(string path)
		{
			if (Path.IsPathFullyQualified(path)) return path;

			string[] parts = path.Split('/', '\\');
			string[] splits = new string[parts.Length + 1];

			splits[0] = workingDirectory;
			for (int i = 0; i < parts.Length; i++) splits[i + 1] = parts[i];

			return Path.GetFullPath(Path.Combine(splits));
		}
	}
}