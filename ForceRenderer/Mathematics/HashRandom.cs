namespace ForceRenderer.Mathematics
{
	public static class HashRandom
	{
		public static int Next(int value)
		{
			long number = (value + 0xE120FC15) * 0x4A39B70D;
			number = (long)(int)((number >> 32) ^ number) * 0x12FAD5C9;
			return (int)((number >> 32) ^ number);
		}
	}
}