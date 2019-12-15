using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster
{
	public static class Contract
	{
		private abstract class Exception : System.Exception
		{
			protected const string DefaultMessage = "Contract Exception";

			protected Exception() : this(DefaultMessage) { }

			protected Exception(in string message) : this(message, null) { }

			protected Exception(in string message, in Exception inner) : base(message, inner) { }
		}

		private class ValueException : Exception
		{
			protected new const string DefaultMessage = "Value failed assertion";

			internal ValueException() : this(DefaultMessage) { }

			internal ValueException(in string message) : this(message, null) { }

			internal ValueException(in string message, in Exception inner) : base(message, inner) { }
		}

		private class NotNullReferenceException : ValueException
		{
			protected new const string DefaultMessage = "Value failed assertion, was not null";

			internal NotNullReferenceException() : this(DefaultMessage) { }

			internal NotNullReferenceException(in string message) : this(message, null) { }

			internal NotNullReferenceException(in string message, in Exception inner) : base(message, inner) { }
		}

		private class NullReferenceException : ValueException
		{
			protected new const string DefaultMessage = "Value failed assertion, was null";

			internal NullReferenceException() : this(DefaultMessage) { }

			internal NullReferenceException(in string message) : this(message, null) { }

			internal NullReferenceException(in string message, in Exception inner) : base(message, inner) { }
		}

		private class BooleanException : ValueException
		{
			protected new const string DefaultMessage = "Value failed boolean assertion";

			internal BooleanException() : this(DefaultMessage) { }

			internal BooleanException(in string message) : this(message, null) { }

			internal BooleanException(in string message, in Exception inner) : base(message, inner) { }
		}

		private class OutOfRangeException : ValueException
		{
			protected new const string DefaultMessage = "Value failed assertion, out of range";

			internal OutOfRangeException() : this(DefaultMessage) { }

			internal OutOfRangeException(in string message) : this(message, null) { }

			internal OutOfRangeException(in string message, in Exception inner) : base(message, inner) { }
		}

		static private bool IsExceptionType(this Type type)
		{
			return type.IsSubclassOf(typeof(Exception));
		}

		public delegate bool ClosedPredicate();

		static internal class Argument
		{
			private class ValueException : Contract.ValueException
			{
				protected new const string DefaultMessage = "Argument failed assertion";

				internal ValueException() : this(DefaultMessage) { }

				internal ValueException(in string message) : this(message, null) { }

				internal ValueException(in string message, in Exception inner) : base(message, inner) { }
			}

			private class NotNullReferenceException : Contract.NotNullReferenceException
			{
				protected new const string DefaultMessage = "Argument failed assertion, was not null";

				internal NotNullReferenceException() : this(DefaultMessage) { }

				internal NotNullReferenceException(in string message) : this(message, null) { }

				internal NotNullReferenceException(in string message, in Exception inner) : base(message, inner) { }
			}

			private class NullReferenceException : Contract.NullReferenceException
			{
				protected new const string DefaultMessage = "Argument failed assertion, was null";

				internal NullReferenceException() : this(DefaultMessage) { }

				internal NullReferenceException(in string message) : this(message, null) { }

				internal NullReferenceException(in string message, in Exception inner) : base(message, inner) { }
			}

			private class BooleanException : Contract.BooleanException
			{
				protected new const string DefaultMessage = "Argument failed boolean assertion";

				internal BooleanException() : this(DefaultMessage) { }

				internal BooleanException(in string message) : this(message, null) { }

				internal BooleanException(in string message, in Exception inner) : base(message, inner) { }
			}

			private class OutOfRangeException : Contract.OutOfRangeException
			{
				protected new const string DefaultMessage = "Argument failed assertion, out of range";

				internal OutOfRangeException() : this(DefaultMessage) { }

				internal OutOfRangeException(in string message) : this(message, null) { }

				internal OutOfRangeException(in string message, in Exception inner) : base(message, inner) { }
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static internal bool Assert(bool predicate, in string message = "Argument failed assertion", in Type exception = null)
			{
				return Contract.Assert(predicate, message, exception ?? typeof(ValueException));
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static internal bool Assert(in ClosedPredicate predicate, in string message = "Argument failed predicated assertion", in Type exception = null)
			{
				if (predicate == null)
				{
					throw new ArgumentNullException($"Argument '{nameof(predicate)}' is null");
				}
				return Contract.Assert(predicate.Invoke(), message, exception ?? typeof(ValueException));
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static internal bool Assert<T>(in T value, in Predicate<T> predicate, in string message = "Argument failed predicated assertion", in Type exception = null)
			{
				if (predicate == null)
				{
					throw new ArgumentNullException($"Argument '{nameof(predicate)}' is null");
				}
				return Contract.Assert(predicate.Invoke(value), message, exception ?? typeof(ValueException));
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static internal bool AssertNull<T>(in T value, in string message = "Argument is not null", Type exception = null)
			{
				return Assert(value == null, message, exception ?? typeof(NotNullReferenceException));
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static internal bool AssertNotNull<T>(in T value, in string message = "Argument is null", Type exception = null)
			{
				return Assert(value != null, message, exception ?? typeof(NullReferenceException));
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static internal bool AssertTrue(in bool value, in string message = "Argument is not true", Type exception = null)
			{
				return Assert(value == true, message, exception ?? typeof(BooleanException));
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static internal bool AssertFalse(in bool value, in string message = "Argument is not false", Type exception = null)
			{
				return Assert(value == false, message, exception ?? typeof(BooleanException));
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static internal bool AssertEqual<T>(in T value, in T reference, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>
			{
				bool Predicate(in T value, in T reference)
				{
					if (typeof(T).IsSubclassOf(typeof(IEquatable<T>)))
					{
						return value.Equals(reference);
					}
					else
					{
						return value.CompareTo(reference) == 0;
					}
				}

				return Assert(Predicate(value, reference), message ?? $"Argument '{value}' is not equal to '{reference}'", exception ?? typeof(ValueException));
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static internal bool AssertNotEqual<T>(in T value, in T reference, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>
			{
				bool Predicate(in T value, in T reference)
				{
					if (typeof(T).IsSubclassOf(typeof(IEquatable<T>)))
					{
						return !value.Equals(reference);
					}
					else
					{
						return value.CompareTo(reference) != 0;
					}
				}

				return Assert(Predicate(value, reference), message ?? $"Argument '{value}' is equal to '{reference}'", exception ?? typeof(ValueException));
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static internal bool AssertGreater<T>(in T value, in T reference, in string message = null, Type exception = null) where T : IComparable, IComparable<T>
			{
				bool Predicate(in T value, in T reference)
				{
					return value.CompareTo(reference) > 0;
				}

				return Assert(Predicate(value, reference), message ?? $"Argument '{value}' is less than or equal to '{reference}'", exception ?? typeof(ValueException));
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static internal bool AssertGreaterEqual<T>(in T value, in T reference, in string message = null, Type exception = null) where T : IComparable, IComparable<T>
			{
				bool Predicate(in T value, in T reference)
				{
					return value.CompareTo(reference) >= 0;
				}

				return Assert(Predicate(value, reference), message ?? $"Argument '{value}' is less than to '{reference}'", exception ?? typeof(ValueException));
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static internal bool AssertLess<T>(in T value, in T reference, in string message = null, Type exception = null) where T : IComparable, IComparable<T>
			{
				bool Predicate(in T value, in T reference)
				{
					return value.CompareTo(reference) < 0;
				}

				return Assert(Predicate(value, reference), message ?? $"Argument '{value}' is greater than or equal to '{reference}'", exception ?? typeof(ValueException));
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static internal bool AssertLessEqual<T>(in T value, in T reference, in string message = null, Type exception = null) where T : IComparable, IComparable<T>
			{
				bool Predicate(in T value, in T reference)
				{
					return value.CompareTo(reference) <= 0;
				}

				return Assert(Predicate(value, reference), message ?? $"Argument '{value}' is greater than '{reference}'", exception ?? typeof(ValueException));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertArgNull<T>(this T value, in string message = null, Type exception = null)
		{
			return Argument.AssertNull(value, message, exception);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertArgNotNull<T>(this T value, in string message = null, Type exception = null)
		{
			return Argument.AssertNotNull(value, message, exception);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertNull<T>(this T value, in string message = "Variable is not null", Type exception = null)
		{
			return Assert(value == null, message, exception ?? typeof(NotNullReferenceException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertNotNull<T>(this T value, in string message = "Variable is null", Type exception = null)
		{
			return Assert(value != null, message, exception ?? typeof(NullReferenceException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertArgTrue(this in bool value, in string message = null, Type exception = null)
		{
			return Argument.AssertTrue(value, message, exception);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertArgFalse(this in bool value, in string message = null, Type exception = null)
		{
			return Argument.AssertFalse(value, message, exception);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertTrue(this in bool value, in string message = "Variable is not true", Type exception = null)
		{
			return Assert(value == true, message, exception ?? typeof(BooleanException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertFalse(this in bool value, in string message = "Variable is not false", Type exception = null)
		{
			return Assert(value == false, message, exception ?? typeof(BooleanException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertArg(bool predicate, in string message = null, Type exception = null)
		{
			return Argument.Assert(predicate, message, exception);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool Assert(bool predicate, in string message = "Variable's value is invalid", Type exception = null)
		{
			if (!exception.IsExceptionType())
			{
				throw new ArgumentOutOfRangeException("Provided assert exception type is not a subclass of Exception");
			}
			if (!predicate)
			{
				throw (ValueException)Activator.CreateInstance(exception ?? typeof(ValueException), new object[] { message });
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertEqual<T>(this T value, in T reference, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>
		{
			bool Predicate(in T value, in T reference)
			{
				if (typeof(T).IsSubclassOf(typeof(IEquatable<T>)))
				{
					return value.Equals(reference);
				}
				else
				{
					return value.CompareTo(reference) == 0;
				}
			}

			return Assert(Predicate(value, reference), message ?? $"Variable '{value}' is not equal to '{reference}'", exception ?? typeof(ValueException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertNotEqual<T>(this T value, in T reference, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>
		{
			bool Predicate(in T value, in T reference)
			{
				if (typeof(T).IsSubclassOf(typeof(IEquatable<T>)))
				{
					return !value.Equals(reference);
				}
				else
				{
					return value.CompareTo(reference) != 0;
				}
			}

			return Assert(Predicate(value, reference), message ?? $"Variable '{value}' is equal to '{reference}'", exception ?? typeof(ValueException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertGreater<T>(this T value, in T reference, in string message = null, Type exception = null) where T : IComparable, IComparable<T>
		{
			bool Predicate(in T value, in T reference)
			{
				return value.CompareTo(reference) > 0;
			}

			return Assert(Predicate(value, reference), message ?? $"Variable '{value}' is less than or equal to '{reference}'", exception ?? typeof(ValueException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertGreaterEqual<T>(this T value, in T reference, in string message = null, Type exception = null) where T : IComparable, IComparable<T>
		{
			bool Predicate(in T value, in T reference)
			{
				return value.CompareTo(reference) >= 0;
			}

			return Assert(Predicate(value, reference), message ?? $"Variable '{value}' is less than to '{reference}'", exception ?? typeof(ValueException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertLess<T>(this T value, in T reference, in string message = null, Type exception = null) where T : IComparable, IComparable<T>
		{
			bool Predicate(in T value, in T reference)
			{
				return value.CompareTo(reference) < 0;
			}

			return Assert(Predicate(value, reference), message ?? $"Variable '{value}' is greater than or equal to '{reference}'", exception ?? typeof(ValueException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertLessEqual<T>(this T value, in T reference, in string message = null, Type exception = null) where T : IComparable, IComparable<T>
		{
			bool Predicate(in T value, in T reference)
			{
				return value.CompareTo(reference) <= 0;
			}

			return Assert(Predicate(value, reference), message ?? $"Variable '{value}' is greater than '{reference}'", exception ?? typeof(ValueException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertArgEqual<T>(this T value, in T reference, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>
		{
			return Argument.AssertEqual(value, reference, message, exception);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertArgNotEqual<T>(this T value, in T reference, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>
		{
			return Argument.AssertNotEqual(value, reference, message, exception);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertArgGreater<T>(this T value, in T reference, in string message = null, Type exception = null) where T : IComparable, IComparable<T>
		{
			return Argument.AssertGreater(value, reference, message, exception);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertArgGreaterEqual<T>(this T value, in T reference, in string message = null, Type exception = null) where T : IComparable, IComparable<T>
		{
			return Argument.AssertGreaterEqual(value, reference, message, exception);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertArgLess<T>(this T value, in T reference, in string message = null, Type exception = null) where T : IComparable, IComparable<T>
		{
			return Argument.AssertLess(value, reference, message, exception);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertArgLessEqual<T>(this T value, in T reference, in string message = null, Type exception = null) where T : IComparable, IComparable<T>
		{
			return Argument.AssertLessEqual(value, reference, message, exception);
		}
	}
}
