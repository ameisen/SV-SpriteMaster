namespace xBRZNet2.Common
{
	internal static class Mask
	{
		public const uint Alpha = 0xff000000;
		public const uint Red = 0x000000ff;
		public const uint Green = 0x0000ff00;
		public const uint Blue = 0x00ff0000;

		internal static class Shift
		{
			public const int Alpha = 24;
			public const int Red = 0;
			public const int Green = 8;
			public const int Blue = 16;
		}
	}
}
