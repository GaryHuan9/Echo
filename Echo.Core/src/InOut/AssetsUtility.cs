using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Echo.Core.InOut;

public static class AssetsUtility
{
	/// <summary>
	/// Returns the absolute path to a path relative to the project folder.
	/// </summary>
	public static string GetAssetPath(string path)
	{
		if (Path.IsPathFullyQualified(path)) return path;
		return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, path));
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
			return GetAssetPath(path);
		}
	}

	public static string GetSiblingPath(string path, string sibling)
	{
		var directory = Path.GetDirectoryName(path);
		return Path.Combine(directory ?? "", sibling);
	}
}