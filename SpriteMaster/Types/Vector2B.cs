using System;

namespace SpriteMaster.Types
{
	internal struct Vector2B :
		ICloneable,
		IComparable,
		IComparable<Vector2B>,
		IEquatable<Vector2B>
	{
		public static readonly Vector2B True = new Vector2B(true, true);
		public static readonly Vector2B False = new Vector2B(false, false);

		public bool X;
		public bool Y;

		public bool Width
		{
			readonly get { return X; }
			set { X = value; }
		}

		public bool Height
		{
			readonly get { return Y; }
			set { Y = value; }
		}

		public bool Negative
		{
			readonly get { return X; }
			set { X = value; }
		}

		public bool Positive
		{
			readonly get { return Y; }
			set { Y = value; }
		}

		public bool this[in int index]
		{
			readonly get
			{
				switch (index)
				{
					case 0:
						return X;
					case 1:
						return Y;
					default:
						throw new IndexOutOfRangeException(nameof(index));
				}
			}
			set
			{
				switch (index)
				{
					case 0:
						X = value; return;
					case 1:
						Y = value; return;
					default:
						throw new IndexOutOfRangeException(nameof(index));
				}
			}
		}

		public Vector2B(in bool x, in bool y)
		{
			X = x;
			Y = y;
		}

		public Vector2B(in bool v)
		{
			Y = X = v;
		}

		public Vector2B(in Vector2B vec)
		{
			X = vec.X;
			Y = vec.Y;
		}

		public Vector2B Set(in Vector2B vec)
		{
			X = vec.X;
			Y = vec.Y;
			return this;
		}

		public Vector2B Set(in bool x, in bool y)
		{
			X = x;
			Y = y;
			return this;
		}

		public Vector2B Set(in bool v)
		{
			Y = X = v;
			return this;
		}

		public readonly Vector2B Clone()
		{
			return new Vector2B(X, Y);
		}

		readonly object ICloneable.Clone()
		{
			return Clone();
		}

		public static Vector2B operator &(in Vector2B lhs, in Vector2B rhs)
		{
			return new Vector2B(
				lhs.X && rhs.X,
				lhs.Y && rhs.Y
			);
		}

		public static Vector2B operator &(in Vector2B lhs, in bool rhs)
		{
			return new Vector2B(
				lhs.X && rhs,
				lhs.Y && rhs
			);
		}

		public static Vector2B operator |(in Vector2B lhs, in Vector2B rhs)
		{
			return new Vector2B(
				lhs.X || rhs.X,
				lhs.Y || rhs.Y
			);
		}

		public static Vector2B operator |(in Vector2B lhs, in bool rhs)
		{
			return new Vector2B(
				lhs.X || rhs,
				lhs.Y || rhs
			);
		}

		public override readonly string ToString () {
			return $"{{{X}, {Y}}}";
		}

		public readonly int CompareTo (object obj) {
			if (obj is Vector2B other) {
				return CompareTo(other);
			}
			else {
				throw new ArgumentException();
			}
		}

		public readonly int CompareTo (Vector2B other) {
			var xComp = X.CompareTo(other);
			var YComp = Y.CompareTo(other);
			if (xComp != 0)
				return xComp;
			if (YComp != 0)
				return YComp;
			return 0;
		}

		public readonly bool Equals (Vector2B other) {
			return X == other.X && Y == other.Y;
		}
	}
}
