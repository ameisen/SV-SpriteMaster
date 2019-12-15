using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SpriteMaster.Types
{
	[StructLayout(LayoutKind.Explicit, Pack = 0)]
	struct Texel : ICloneable
	{
		public enum Ordering
		{
			ABGR,
			ARGB,
			RGBA,
			BGRA,

			Default = ABGR
		}

		public const Texel.Ordering Order = Texel.Ordering.Default;

		public enum Offset : uint
		{
			Alpha = (Order == Texel.Ordering.ABGR || Order == Texel.Ordering.ARGB) ? 3 : 0,
			Blue = (Order == Texel.Ordering.ABGR) ? 2 : (Order == Texel.Ordering.ARGB) ? 0 : (Order == Texel.Ordering.RGBA) ? 1 : 0,
			Green = (Order == Texel.Ordering.ABGR || Order == Texel.Ordering.ARGB) ? 1 : 2,
			Red = (Order == Texel.Ordering.ABGR) ? 0 : (Order == Texel.Ordering.ARGB) ? 2 : (Order == Texel.Ordering.RGBA) ? 3 : 1,

			A = Alpha,
			B = Blue,
			G = Green,
			R = Red
		}

		// C# doesn't allow unsigned shift values. I have no idea why.
		public enum Shift : int
		{
			Alpha = 8 * (int)Offset.Alpha,
			Blue = 8 * (int)Offset.Blue,
			Green = 8 * (int)Offset.Green,
			Red = 8 * (int)Offset.Red,

			A = Alpha,
			B = Blue,
			G = Green,
			R = Red
		}

		public enum Mask : uint
		{
			Alpha = 0xFFU << Shift.Alpha,
			Blue = 0xFFU << Shift.Blue,
			Green = 0xFFU << Shift.Green,
			Red = 0xFFU << Shift.Red,

			A = Alpha,
			B = Blue,
			G = Green,
			R = Red
		}

		public static class Colors {
			public static readonly Texel Transparent = new Texel() { UPacked = 0x00000000 };
			public static readonly Texel Black = new Texel() { A = 0xFF, R = 0, G = 0, B = 0 };
			public static readonly Texel White = new Texel() { A = 0xFF, R = 0xFF, G = 0xFF, B = 0xFF };
			public static readonly Texel Blue = new Texel() { A = 0xFF, R = 0, G = 0, B = 0xFF };
			public static readonly Texel Green = new Texel() { A = 0xFF, R = 0, G = 0xFF, B = 0 };
			public static readonly Texel Red = new Texel() { A = 0xFF, R = 0xFF, G = 0, B = 0 };
			public static readonly Texel Magenta = new Texel() { A = 0xFF, R = 0xFF, G = 0, B = 0xFF };
		}

		[FieldOffset(0)]
		public int Packed;

		[FieldOffset(3 - (int)Offset.A)]
		public byte A;

		[FieldOffset(3 - (int)Offset.B)]
		public byte B;

		[FieldOffset(3 - (int)Offset.G)]
		public byte G;

		[FieldOffset(3 - (int)Offset.R)]
		public byte R;

		public byte Alpha
		{
			readonly get { return A; }
			set { A = value; }
		}

		public byte Blue
		{
			readonly get { return B; }
			set { B = value; }
		}

		public byte Green
		{
			readonly get { return G; }
			set { G = value; }
		}

		public byte Red
		{
			readonly get { return R; }
			set { R = value; }
		}

		public uint UPacked
		{
			readonly get { return unchecked((uint)Packed); }
			set { Packed = unchecked((int)value); }
		}

		public bool IsTransparent
		{
			get { return A != 0; }
		}

		public Color Color
		{
			readonly get { return Color.FromArgb(A, R, G, B); }
			set { A = value.A; R = value.R; G = value.G; B = value.B; }
		}

		internal Texel(in byte A = 0xFF, in byte R = 0, in byte G = 0, in byte B = 0)
		{
			this.Packed = 0; // TODO : Strictly unnecessary, but C# isn't aware that Packed is aliasing the byte values. I could change it to a property.
			this.A = A;
			this.R = R;
			this.G = G;
			this.B = B;
		}

		internal Texel(in Texel texel) : this(A: texel.A, R: texel.R, G: texel.G, B: texel.B) { }

		internal Texel(in int packed, in Ordering order = Ordering.ABGR) : this(From(packed, order)) { }

		internal Texel(in uint packed, in Ordering order = Ordering.ABGR) : this(From(packed, order)) { }

		internal static Texel From(in byte A = 0xFF, in byte R = 0, in byte G = 0, in byte B = 0)
		{
			return new Texel()
			{
				A = A,
				R = R,
				G = G,
				B = B
			};
		}

		internal static Texel From(in Color color)
		{
			return new Texel()
			{
				Color = color
			};
		}

		internal int To(in Ordering order = Ordering.ABGR)
		{
			// Early out
			if (order == Order)
			{
				return Packed;
			}

			int Ashift, Rshift, Gshift, Bshift;

			switch (order)
			{
				case Ordering.ABGR:
					Ashift = 24;
					Bshift = 16;
					Gshift = 8;
					Rshift = 0;
					break;
				case Ordering.ARGB:
					Ashift = 24;
					Rshift = 16;
					Gshift = 8;
					Bshift = 0;
					break;
				case Ordering.BGRA:
					Bshift = 24;
					Gshift = 16;
					Rshift = 8;
					Ashift = 0;
					break;
				case Ordering.RGBA:
					Rshift = 24;
					Gshift = 16;
					Bshift = 8;
					Ashift = 0;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(order));
			}

			return unchecked((int)(
				((uint)A << Ashift) |
				((uint)R << Rshift) |
				((uint)G << Gshift) |
				((uint)B << Bshift)
			));
		}

		internal static Texel From(in int color, in Ordering order = Ordering.ABGR)
		{
			return From(unchecked((uint)color), order);
		}



		internal static Texel From(in uint color, in Ordering order = Ordering.ABGR)
		{
			// Early out
			if (order == Order)
			{
				return new Texel()
				{
					UPacked = color
				};
			}

			int Ashift, Rshift, Gshift, Bshift;

			switch (order)
			{
				case Ordering.ABGR:
					Ashift = 24;
					Bshift = 16;
					Gshift = 8;
					Rshift = 0;
					break;
				case Ordering.ARGB:
					Ashift = 24;
					Rshift = 16;
					Gshift = 8;
					Bshift = 0;
					break;
				case Ordering.BGRA:
					Bshift = 24;
					Gshift = 16;
					Rshift = 8;
					Ashift = 0;
					break;
				case Ordering.RGBA:
					Rshift = 24;
					Gshift = 16;
					Bshift = 8;
					Ashift = 0;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(order));
			}

			return new Texel()
			{
				A = color.ExtractByte(Ashift),
				R = color.ExtractByte(Rshift),
				G = color.ExtractByte(Gshift),
				B = color.ExtractByte(Bshift)
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

		internal static Texel FromARGB(in int color)
		{
			return From(color, Ordering.ARGB);
		}

		internal static Texel FromARGB(in uint color)
		{
			return From(color, Ordering.ARGB);
		}

		internal static Texel FromABGR(in int color)
		{
			return From(color, Ordering.ABGR);
		}

		internal static Texel FromABGR(in uint color)
		{
			return From(color, Ordering.ABGR);
		}

		public readonly Texel Clone()
		{
			return new Texel() { UPacked = UPacked };
		}

		readonly object ICloneable.Clone()
		{
			return Clone();
		}
	}
}
