using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ForceRenderer.IO
{
	public abstract class LoadableAsset
	{
		protected abstract IReadOnlyList<string> AcceptableFileExtensions { get; }

		protected string GetAbsolutePath(string path)
		{
			string extension = Path.GetExtension(path);
			string result = null;

			if (string.IsNullOrEmpty(extension))
			{
				//No provided extension, check through all acceptable extensions
				string assetPath = AssetsUtility.GetAssetsPath(path);

				for (int i = 0; i < AcceptableFileExtensions.Count; i++)
				{
					result = Path.ChangeExtension(assetPath, AcceptableFileExtensions[i]);
					if (File.Exists(result)) break;
				}
			}
			else
			{
				if (AcceptableFileExtensions.Contains(extension)) result = AssetsUtility.GetAssetsPath(path);
				else throw new FileNotFoundException($"Incompatible file type at {path} for {GetType()}");
			}

			if (!string.IsNullOrEmpty(result) && File.Exists(result)) return result;
			throw new FileNotFoundException($"No file found at {path} for {GetType()}");
		}

		protected static string GetSiblingPath(string path, string sibling)
		{
			var directory = Path.GetDirectoryName(path);
			return Path.Combine(directory ?? "", sibling);
		}

		//Might move, temporary; returns a string constructed from multiple parts separated by spaces
		protected static string GetRemain(IEnumerable<string> parts, int start) => string.Join(' ', parts.Skip(start));
	}
}