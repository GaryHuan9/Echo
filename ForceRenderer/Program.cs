using System.Drawing;
using System.Drawing.Imaging;

namespace ForceRenderer
{
	class Program
	{
		static void Main()
		{
			using Bitmap bitmap = new Bitmap(50, 50);

			for (int x = 0; x < 50; x++)
			{
				bitmap.SetPixel(x, 0, Color.Brown);
				bitmap.SetPixel(x, 49, Color.Brown);
			}

			bitmap.SetPixel(2, 2, Color.Aqua);
			bitmap.Save("green.png", ImageFormat.Png);
		}
	}
}