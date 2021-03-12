using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ForceRenderer.IO
{
	public interface ILoadableAsset
	{
		IReadOnlyList<string> AcceptableFileExtensions { get; }
	}

	public static class LoadableAssetExtensions
	{
		public static string GetAbsolutePath(this ILoadableAsset asset, string path)
		{
			string extension = Path.GetExtension(path);
			string result = null;

			if (string.IsNullOrEmpty(extension))
			{
				//No provided extension, check through all acceptable extensions
				string assetPath = FormAbsolute(path);

				foreach (string fileExtension in asset.AcceptableFileExtensions)
				{
					result = Path.ChangeExtension(assetPath, fileExtension);
					if (File.Exists(result)) break;
				}
			}
			else
			{
				if (asset.AcceptableFileExtensions.Contains(extension)) result = FormAbsolute(path);
				else throw new FileNotFoundException($"Incompatible file type at {path} for {asset.GetType()}");
			}

			if (!string.IsNullOrEmpty(result) && File.Exists(result)) return result;
			throw new FileNotFoundException($"No file found at {result} for {asset.GetType()}");

			static string FormAbsolute(string path)
			{
				path = Path.GetFullPath(path);

				if (Path.IsPathFullyQualified(path)) return path;
				return AssetsUtility.GetAssetsPath(path);
			}
		}

		public static string GetSiblingPath(this ILoadableAsset asset, string path, string sibling)
		{
			var directory = Path.GetDirectoryName(path);
			return Path.Combine(directory ?? "", sibling);
		}
	}
}