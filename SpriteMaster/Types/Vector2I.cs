using System;

namespace SpriteMaster.Types
{
	using DrawingPoint = System.Drawing.Point;
	using XNAPoint = Microsoft.Xna.Framework.Point;
	using XTilePoint = xTile.Dimensions.Location;

	using DrawingSize = System.Drawing.Size;
	using XTileSize = xTile.Dimensions.Size;

	internal struct Vector2I :
		ICloneable,
		IComparable,
		IComparable<Vector2I>,
		IComparable<DrawingPoint>,
		IComparable<XNAPoint>,
		IComparable<XTilePoint>,
		IComparable<DrawingSize>,
		IComparable<XTileSize>,
		IEquatable<Vector2I>,
		IEquatable<DrawingPoint>,
		IEquatable<XNAPoint>,
		IEquatable<XTilePoint>,
		IEquatable<DrawingSize>,
		IEquatable<XTileSize>
	{
		public static readonly Vector2I Zero = new Vector2I(0, 0);
		public static readonly Vector2I One = new Vector2I(1, 1);
		public static readonly Vector2I MinusOne = new Vector2I(-1, -1);
		public static readonly Vector2I Empty = Zero;

		public int X;
		public int Y;

		public int Width
		{
			readonly get { return X; }
			set { X = value; }
		}

		public int Height
		{
			readonly get { return Y; }
			set { Y = value; }
		}

		public bool IsEmpty
		{
			get { return X == 0 && Y == 0; }
		}

		public Vector2I(in int x, in int y)
		{
			X = x;
			Y = y;
		}

		public Vector2I(in int v) : this(v, v) { }

		public Vector2I(in Vector2I vec) : this(vec.X, vec.Y) { }

		public Vector2I(in DrawingPoint v) : this(v.X, v.Y) { }

		public Vector2I(in XNAPoint v) : this(v.X, v.Y) { }

		public Vector2I(in XTilePoint v) : this(v.X, v.Y) { }

		public Vector2I(in DrawingSize v) : this(v.Width, v.Height) { }

		public Vector2I(in XTileSize v) : this(v.Width, v.Height) { }

		public Vector2I Set(in int x, in int y)
		{
			X = x;
			Y = y;
			return this;
		}

		public Vector2I Set(in int v)
		{
			return Set(v, v);
		}

		public Vector2I Set(in Vector2I vec)
		{
			return Set(vec.X, vec.Y);
		}

		public Vector2I Set(in DrawingPoint vec)
		{
			return Set(vec.X, vec.Y);
		}

		public Vector2I Set(in XNAPoint vec)
		{
			return Set(vec.X, vec.Y);
		}

		public Vector2I Set(in XTilePoint vec)
		{
			return Set(vec.X, vec.Y);
		}

		public Vector2I Set(in DrawingSize vec)
		{
			return Set(vec.Width, vec.Height);
		}

		public Vector2I Set(in XTileSize vec)
		{
			return Set(vec.Width, vec.Height);
		}

		public static implicit operator DrawingPoint(in Vector2I vec)
		{
			return new DrawingPoint(vec.X, vec.Y);
		}

		public static implicit operator XNAPoint(in Vector2I vec)
		{
			return new XNAPoint(vec.X, vec.Y);
		}

		public static implicit operator XTilePoint(in Vector2I vec)
		{
			return new XTilePoint(vec.X, vec.Y);
		}

		public static implicit operator DrawingSize(in Vector2I vec)
		{
			return new DrawingSize(vec.X, vec.Y);
		}

		public static implicit operator XTileSize(in Vector2I vec)
		{
			return new XTileSize(vec.X, vec.Y);
		}

		public static implicit operator Vector2I(in DrawingPoint vec)
		{
			return new Vector2I(vec);
		}

		public static implicit operator Vector2I(in XNAPoint vec)
		{
			return new Vector2I(vec);
		}

		public static implicit operator Vector2I(in XTilePoint vec)
		{
			return new Vector2I(vec);
		}

		public static implicit operator Vector2I(in DrawingSize vec)
		{
			return new Vector2I(vec);
		}

		public static implicit operator Vector2I(in XTileSize vec)
		{
			return new Vector2I(vec);
		}

		public readonly Vector2I Clone()
		{
			return new Vector2I(this);
		}

		readonly object ICloneable.Clone()
		{
			return Clone();
		}

		public static Vector2I operator +(in Vector2I lhs, in Vector2I rhs)
		{
			return new Vector2I(
				lhs.X + rhs.X,
				lhs.Y + rhs.Y
			);
		}

		public static Vector2I operator -(in Vector2I lhs, in Vector2I rhs)
		{
			return new Vector2I(
				lhs.X - rhs.X,
				lhs.Y - rhs.Y
			);
		}

		public static Vector2I operator *(in Vector2I lhs, in Vector2I rhs)
		{
			return new Vector2I(
				lhs.X * rhs.X,
				lhs.Y * rhs.Y
			);
		}

		public static Vector2I operator /(in Vector2I lhs, in Vector2I rhs)
		{
			return new Vector2I(
				lhs.X / rhs.X,
				lhs.Y / rhs.Y
			);
		}

		public static Vector2I operator %(in Vector2I lhs, in Vector2I rhs)
		{
			return new Vector2I(
				lhs.X % rhs.X,
				lhs.Y % rhs.Y
			);
		}

		public static Vector2I operator +(in Vector2I lhs, in int rhs)
		{
			return new Vector2I(
				lhs.X + rhs,
				lhs.Y + rhs
			);
		}

		public static Vector2I operator -(in Vector2I lhs, in int rhs)
		{
			return new Vector2I(
				lhs.X - rhs,
				lhs.Y - rhs
			);
		}

		public static Vector2I operator *(in Vector2I lhs, in int rhs)
		{
			return new Vector2I(
				lhs.X * rhs,
				lhs.Y * rhs
			);
		}

		public static Vector2I operator /(in Vector2I lhs, in int rhs)
		{
			return new Vector2I(
				lhs.X / rhs,
				lhs.Y / rhs
			);
		}

		public static Vector2I operator %(in Vector2I lhs, in int rhs)
		{
			return new Vector2I(
				lhs.X % rhs,
				lhs.Y % rhs
			);
		}

		public override readonly string ToString()
		{
			return $"{{{X}, {Y}}}";
		}

		public readonly int CompareTo(Vector2I other)
		{
			var results = new int[] {
				X.CompareTo(other.X),
				Y.CompareTo(other.Y)
			};
			foreach (var result in results)
			{
				if (result != 0)
				{
					return result;
				}
			}
			return 0;
		}

		public readonly int CompareTo(DrawingPoint other)
		{
			var results = new int[] {
				X.CompareTo(other.X),
				Y.CompareTo(other.Y)
			};
			foreach (var result in results)
			{
				if (result != 0)
				{
					return result;
				}
			}
			return 0;
		}

		public readonly int CompareTo(XNAPoint other)
		{
			var results = new int[] {
				X.CompareTo(other.X),
				Y.CompareTo(other.Y)
			};
			foreach (var result in results)
			{
				if (result != 0)
				{
					return result;
				}
			}
			return 0;
		}

		public readonly int CompareTo(XTilePoint other)
		{
			var results = new int[] {
				X.CompareTo(other.X),
				Y.CompareTo(other.Y)
			};
			foreach (var result in results)
			{
				if (result != 0)
				{
					return result;
				}
			}
			return 0;
		}

		public readonly int CompareTo(DrawingSize other)
		{
			var results = new int[] {
				X.CompareTo(other.Width),
				Y.CompareTo(other.Height)
			};
			foreach (var result in results)
			{
				if (result != 0)
				{
					return result;
				}
			}
			return 0;
		}

		public readonly int CompareTo(XTileSize other)
		{
			var results = new int[] {
				X.CompareTo(other.Width),
				Y.CompareTo(other.Height)
			};
			foreach (var result in results)
			{
				if (result != 0)
				{
					return result;
				}
			}
			return 0;
		}

		readonly int IComparable.CompareTo(object other)
		{
			switch (other)
			{
				case Vector2I vec:
					return CompareTo(vec);
				case DrawingPoint vec:
					return CompareTo(vec);
				case XNAPoint vec:
					return CompareTo(vec);
				case XTilePoint vec:
					return CompareTo(vec);
				case DrawingSize vec:
					return CompareTo(vec);
				case XTileSize vec:
					return CompareTo(vec);
				default:
					throw new ArgumentException();
			}
		}

		public readonly override int GetHashCode()
		{
			return X.GetHashCode() ^ Y.GetHashCode();
		}

		public readonly override bool Equals(object other)
		{
			switch (other)
			{
				case Vector2I vec:
					return Equals(vec);
				case DrawingPoint vec:
					return Equals(vec);
				case XNAPoint vec:
					return Equals(vec);
				case XTilePoint vec:
					return Equals(vec);
				case DrawingSize vec:
					return Equals(vec);
				case XTileSize vec:
					return Equals(vec);
				default:
					throw new ArgumentException();
			}
		}

		public readonly bool Equals(Vector2I other)
		{
			return
				X == other.X &&
				Y == other.Y;
		}

		public readonly bool Equals(DrawingPoint other)
		{
			return
				X == other.X &&
				Y == other.Y;
		}

		public readonly bool Equals(XNAPoint other)
		{
			return
				X == other.X &&
				Y == other.Y;
		}

		public readonly bool Equals(XTilePoint other)
		{
			return
				X == other.X &&
				Y == other.Y;
		}

		public readonly bool Equals(DrawingSize other)
		{
			return
				Width == other.Width &&
				Height == other.Height;
		}

		public readonly bool Equals(XTileSize other)
		{
			return
				Width == other.Width &&
				Height == other.Height;
		}

		public readonly bool NotEquals(Vector2I other)
		{
			return
				X != other.X ||
				Y != other.Y;
		}

		public readonly bool NotEquals(DrawingPoint other)
		{
			return
				X != other.X ||
				Y != other.Y;
		}

		public readonly bool NotEquals(XNAPoint other)
		{
			return
				X != other.X ||
				Y != other.Y;
		}

		public readonly bool NotEquals(XTilePoint other)
		{
			return
				X != other.X ||
				Y != other.Y;
		}

		public readonly bool NotEquals(DrawingSize other)
		{
			return
				Width != other.Width ||
				Height != other.Height;
		}

		public readonly bool NotEquals(XTileSize other)
		{
			return
				Width != other.Width ||
				Height != other.Height;
		}

		public static bool operator ==(in Vector2I lhs, in Vector2I rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(in Vector2I lhs, in Vector2I rhs)
		{
			return lhs.NotEquals(rhs);
		}
		public static bool operator ==(in Vector2I lhs, in DrawingPoint rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(in Vector2I lhs, in DrawingPoint rhs)
		{
			return lhs.NotEquals(rhs);
		}

		public static bool operator ==(in DrawingPoint lhs, in Vector2I rhs)
		{
			return rhs.Equals(lhs);
		}

		public static bool operator !=(in DrawingPoint lhs, in Vector2I rhs)
		{
			return rhs.NotEquals(lhs);
		}

		public static bool operator ==(in Vector2I lhs, in XNAPoint rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(in Vector2I lhs, in XNAPoint rhs)
		{
			return lhs.NotEquals(rhs);
		}

		public static bool operator ==(in XNAPoint lhs, in Vector2I rhs)
		{
			return rhs.Equals(lhs);
		}

		public static bool operator !=(in XNAPoint lhs, in Vector2I rhs)
		{
			return rhs.NotEquals(lhs);
		}

		public static bool operator ==(in Vector2I lhs, in XTilePoint rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(in Vector2I lhs, in XTilePoint rhs)
		{
			return lhs.NotEquals(rhs);
		}

		public static bool operator ==(in XTilePoint lhs, in Vector2I rhs)
		{
			return rhs.Equals(lhs);
		}

		public static bool operator !=(in XTilePoint lhs, in Vector2I rhs)
		{
			return rhs.NotEquals(lhs);
		}

		public static bool operator ==(in Vector2I lhs, in DrawingSize rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(in Vector2I lhs, in DrawingSize rhs)
		{
			return lhs.NotEquals(rhs);
		}

		public static bool operator ==(in DrawingSize lhs, in Vector2I rhs)
		{
			return rhs.Equals(lhs);
		}

		public static bool operator !=(in DrawingSize lhs, in Vector2I rhs)
		{
			return rhs.NotEquals(lhs);
		}

		public static bool operator ==(in Vector2I lhs, in XTileSize rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(in Vector2I lhs, in XTileSize rhs)
		{
			return lhs.NotEquals(rhs);
		}

		public static bool operator ==(in XTileSize lhs, in Vector2I rhs)
		{
			return rhs.Equals(lhs);
		}

		public static bool operator !=(in XTileSize lhs, in Vector2I rhs)
		{
			return rhs.NotEquals(lhs);
		}
	}
}
