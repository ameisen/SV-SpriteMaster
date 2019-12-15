using System.Runtime.CompilerServices;

namespace xBRZNet2.Blend
{
	internal struct BlendResult
	{
		public char F;
		public char G;
		public char J;
		public char K;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Reset()
		{
			F = G = J = K = (char)0;
		}
	}
}
