using System;

namespace SpriteMaster.Types
{
	internal struct Vector2T<T> : ICloneable where T : struct
	{
		public T X;
		public T Y;

		public T this[in int index]
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

		public Vector2T(in T x, in T y)
		{
			X = x;
			Y = y;
		}

		public Vector2T(in T v)
		{
			Y = X = v;
		}

		public Vector2T(in Vector2T<T> vec)
		{
			X = vec.X;
			Y = vec.Y;
		}

		public Vector2T<T> Set(in Vector2T<T> vec)
		{
			X = vec.X;
			Y = vec.Y;
			return this;
		}

		public Vector2T<T> Set(in T x, in T y)
		{
			X = x;
			Y = y;
			return this;
		}

		public Vector2T<T> Set(in T v)
		{
			Y = X = v;
			return this;
		}

		public readonly Vector2T<T> Clone()
		{
			return new Vector2T<T>(X, Y);
		}

		readonly object ICloneable.Clone()
		{
			return Clone();
		}
	}
}
