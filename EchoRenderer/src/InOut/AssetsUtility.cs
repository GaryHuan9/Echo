using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EchoRenderer.InOut;

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

	/// <summary>
	/// Returns the absolute path of an asset file from an input <paramref name="path"/>.
	/// This method will look through all <paramref name="fileExtensions"/> if no extension is provided.
	/// </summary>
	/// <exception cref="FileNotFoundException">Thrown when no file is found after looking through all extensions.</exception>
	public static string GetAbsolutePath(IReadOnlyList<string> fileExtensions, string path)
	{
		string extension = Path.GetExtension(path);
		string result = null;

		if (string.IsNullOrEmpty(extension))
		{
			//No provided extension, check through all acceptable extensions
			string assetPath = FormAbsolute(path);

			foreach (string fileExtension in fileExtensions)
			{
				result = Path.ChangeExtension(assetPath, fileExtension);
				if (File.Exists(result)) break;
			}
		}
		else
		{
			if (fileExtensions.Contains(extension)) result = FormAbsolute(path);
			else throw new FileNotFoundException($"Incompatible file type at {path}");
		}

		if (!string.IsNullOrEmpty(result) && File.Exists(result)) return result;
		throw new FileNotFoundException($"No file found at {result}");

		static string FormAbsolute(string path)
		{
			if (Path.IsPathFullyQualified(path)) return path;
			return GetAssetsPath(path);
		}
	}

	public static string GetSiblingPath(string path, string sibling)
	{
		var directory = Path.GetDirectoryName(path);
		return Path.Combine(directory ?? "", sibling);
	}
}