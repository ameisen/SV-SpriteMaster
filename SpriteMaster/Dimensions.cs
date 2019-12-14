using Microsoft.Xna.Framework.Graphics;
using System;
using System.Drawing;

namespace SpriteMaster
{
	internal struct Dimensions
	{
		internal int X;
		internal int Y;

		internal int Width { get { return X; } set { X = value; } }
		internal int Height { get { return Y; } set { Y = value; } }

		internal int Area { get { return X * Y; } }

		internal Dimensions(int x, int y)
		{
			X = x;
			Y = y;
		}

		internal Dimensions(in Texture2D tex)
		{
			X = tex.Width;
			Y = tex.Height;
		}

		internal Dimensions(in Bitmap tex)
		{
			X = tex.Width;
			Y = tex.Height;
		}

		internal Dimensions(in Rectangle rec)
		{
			X = rec.Width;
			Y = rec.Height;
		}

		internal Dimensions(in Microsoft.Xna.Framework.Rectangle rec)
		{
			X = rec.Width;
			Y = rec.Height;
		}

		internal readonly int Min()
		{
			return Math.Min(X, Y);
		}

		internal readonly int Max()
		{
			return Math.Max(X, Y);
		}

		internal readonly Dimensions Min (in Dimensions v)
		{
			return From(
				Math.Min(X, v.X),
				Math.Min(Y, v.Y)
			);
		}

		internal readonly Dimensions Max(in Dimensions v)
		{
			return From(
				Math.Max(X, v.X),
				Math.Max(Y, v.Y)
			);
		}

		internal readonly Dimensions Clamp(in Dimensions min, in Dimensions max)
		{
			return From(
				Helpers.Clamp(X, min.X, max.X),
				Helpers.Clamp(Y, min.Y, max.Y)
			);
		}


		internal readonly Dimensions Min(int v)
		{
			return From(
				Math.Min(X, v),
				Math.Min(Y, v)
			);
		}

		internal readonly Dimensions Max(int v)
		{
			return From(
				Math.Max(X, v),
				Math.Max(Y, v)
			);
		}

		internal readonly Dimensions Clamp(int min, int max)
		{
			return From(
				Helpers.Clamp(X, min, max),
				Helpers.Clamp(Y, min, max)
			);
		}

		public static bool operator ==(in Dimensions l, in Dimensions r)
		{
			return (l.Width == r.Width) && (l.Height == r.Height);
		}

		public static bool operator !=(in Dimensions l, in Dimensions r)
		{
			return (l.Width != r.Width) || (l.Height != r.Height);
		}

		public override readonly bool Equals(object o)
		{
			if (!(o is Dimensions))
			{
				return false;
			}
			var d = (Dimensions)o;
			return X == d.X && Y == d.Y;
		}

		public override readonly int GetHashCode()
		{
			return X.GetHashCode() ^ Y.GetHashCode();
		}

		public static bool operator== (in Dimensions d, in Texture2D tex)
		{
			return (d.Width == tex.Width) && (d.Height == tex.Height);
		}

		public static bool operator !=(in Dimensions d, in Texture2D tex)
		{
			return (d.Width != tex.Width) || (d.Height != tex.Height);
		}

		public static bool operator ==(in Dimensions d, in Bitmap tex)
		{
			return (d.Width == tex.Width) && (d.Height == tex.Height);
		}

		public static bool operator !=(in Dimensions d, in Bitmap tex)
		{
			return (d.Width != tex.Width) || (d.Height != tex.Height);
		}

		public static Dimensions operator* (in Dimensions l, in Dimensions r)
		{
			return From(l.X * r.X, l.Y * r.Y);
		}

		public static Dimensions operator *(in Dimensions d, int v)
		{
			return From(d.X * v, d.Y * v);
		}

		public static Dimensions operator *(in Dimensions d, float v)
		{
			return From(d.X * v, d.Y * v);
		}

		public static Dimensions operator *(in Dimensions d, double v)
		{
			return From(d.X * v, d.Y * v);
		}

		public static Dimensions operator /(in Dimensions l, in Dimensions r)
		{
			return From(l.X / r.X, l.Y / r.Y);
		}

		public static Dimensions operator /(in Dimensions d, int v)
		{
			return From(d.X / v, d.Y / v);
		}

		public static Dimensions operator /(in Dimensions d, float v)
		{
			return From(d.X / v, d.Y / v);
		}

		public static Dimensions operator /(in Dimensions d, double v)
		{
			return From(d.X / v, d.Y / v);
		}

		public static Dimensions operator %(in Dimensions l, in Dimensions r)
		{
			return From(l.X % r.X, l.Y % r.Y);
		}

		public static Dimensions operator %(in Dimensions d, int v)
		{
			return From(d.X % v, d.Y % v);
		}

		public static Dimensions operator %(in Dimensions d, float v)
		{
			return From(d.X % v, d.Y % v);
		}

		public static Dimensions operator %(in Dimensions d, double v)
		{
			return From(d.X % v, d.Y % v);
		}

		internal static Dimensions From(in Dimensions d)
		{
			return new Dimensions(d.X, d.Y);
		}

		internal static Dimensions From(int x, int y)
		{
			return new Dimensions(x, y);
		}

		internal static Dimensions From(in Texture2D tex)
		{
			return new Dimensions(tex);
		}

		internal static Dimensions From(in Bitmap tex)
		{
			return new Dimensions(tex);
		}

		internal static Dimensions From(in Rectangle rec)
		{
			return new Dimensions(rec);
		}

		internal static Dimensions From(in Microsoft.Xna.Framework.Rectangle rec)
		{
			return new Dimensions(rec);
		}

		internal static Dimensions RoundFrom(float x, float y)
		{
			return new Dimensions(
				Helpers.RoundToInt(x),
				Helpers.RoundToInt(y)
			);
		}

		internal static Dimensions RoundNextFrom(float x, float y)
		{
			return new Dimensions(
				Helpers.RoundToNextInt(x),
				Helpers.RoundToNextInt(y)
			);
		}

		internal static Dimensions From(float x, float y)
		{
			return RoundFrom(x, y);
		}

		internal static Dimensions RoundFrom(double x, double y)
		{
			return new Dimensions(
				Helpers.RoundToInt(x),
				Helpers.RoundToInt(y)
			);
		}

		internal static Dimensions RoundNextFrom(double x, double y)
		{
			return new Dimensions(
				Helpers.RoundToNextInt(x),
				Helpers.RoundToNextInt(y)
			);
		}

		internal static Dimensions From(double x, double y)
		{
			return RoundFrom(x, y);
		}
	}
}
