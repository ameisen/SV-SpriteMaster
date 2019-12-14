using System.Drawing;
using System.Runtime.InteropServices;

namespace SpriteMaster.Types
{
	[StructLayout(LayoutKind.Explicit, Pack = 0)]
	struct Texel
	{
		[FieldOffset(0)]
		public int Packed;
		[FieldOffset(0)]
		public uint UPacked;
		[FieldOffset(0)]
		public byte A;
		[FieldOffset(1)]
		public byte B;
		[FieldOffset(2)]
		public byte G;
		[FieldOffset(3)]
		public byte R;

		public Color Color
		{
			get { return Color.FromArgb(A, R, G, B); }
			set { A = value.A; R = value.R; G = value.G; B = value.B; }
		}

		internal static Texel From(in Color color)
		{
			return new Texel()
			{
				Color = color
			};
		}

		internal static Texel FromARGB(in Color color)
		{
			return new Texel()
			{
				A = color.A,
				B = color.R,
				G = color.G,
				R = color.B
			};
		}
	}
}
