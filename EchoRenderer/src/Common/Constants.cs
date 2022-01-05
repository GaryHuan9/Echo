using System.Runtime.Intrinsics;

namespace EchoRenderer.Common
{
	public static class Constants
	{
		public static Vector128<float> LuminanceVector => Vector128.Create(0.2126f, 0.7152f, 0.0722f, 0f);
	}
}