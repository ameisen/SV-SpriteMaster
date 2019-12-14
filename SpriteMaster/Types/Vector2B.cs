namespace SpriteMaster
{
	internal struct Vector2B
	{
		public bool X;
		public bool Y;

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
	}
}
