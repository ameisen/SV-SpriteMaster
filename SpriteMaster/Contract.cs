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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Untraced]
		static internal bool AssertNull<T>(this T value, in string message = "Variable is not null", Type exception = null)
		{
			return Assert(value == null, message, exception ?? typeof(NotNullReferenceException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Untraced]
		static internal bool AssertNotNull<T>(this T value, in string message = "Variable is null", Type exception = null)
		{
			return Assert(value != null, message, exception ?? typeof(NullReferenceException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Untraced]
		static internal bool AssertTrue(this in bool value, in string message = "Variable is not true", Type exception = null)
		{
			return Assert(value == true, message, exception ?? typeof(BooleanException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Untraced]
		static internal bool AssertFalse(this in bool value, in string message = "Variable is not false", Type exception = null)
		{
			return Assert(value == false, message, exception ?? typeof(BooleanException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Untraced]
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
		[Untraced]
		static internal bool Assert(in ClosedPredicate predicate, in string message = "Variable failed predicated assertion", in Type exception = null)
		{
			if (predicate == null)
			{
				throw new ArgumentNullException($"Argument '{nameof(predicate)}' is null");
			}
			return Contract.Assert(predicate.Invoke(), message, exception ?? typeof(ValueException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Untraced]
		static internal bool Assert<T>(in T value, in Predicate<T> predicate, in string message = "Variable failed predicated assertion", in Type exception = null)
		{
			if (predicate == null)
			{
				throw new ArgumentNullException($"Argument '{nameof(predicate)}' is null");
			}
			return Contract.Assert(predicate.Invoke(value), message, exception ?? typeof(ValueException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Untraced]
		static internal bool AssertEqual<T, U>(this T value, in U reference, in string message = null, Type exception = null)
			where T : IComparable, IComparable<U>, IEquatable<U>
			where U : IComparable, IComparable<T>, IEquatable<T>
		{
			bool Predicate(in T value, in U reference)
			{
				if (typeof(T).IsSubclassOf(typeof(IEquatable<U>)))
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
		[Untraced]
		static internal bool AssertNotEqual<T, U>(this T value, in U reference, in string message = null, Type exception = null)
			where T : IComparable, IComparable<U>, IEquatable<U>
			where U : IComparable, IComparable<T>, IEquatable<T>
		{
			bool Predicate(in T value, in U reference)
			{
				if (typeof(T).IsSubclassOf(typeof(IEquatable<U>)))
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
		[Untraced]
		static internal bool AssertGreater<T, U>(this T value, in U reference, in string message = null, Type exception = null)
			where T : IComparable, IComparable<U>
			where U : IComparable, IComparable<T>
		{
			bool Predicate(in T value, in U reference)
			{
				return value.CompareTo(reference) > 0;
			}

			return Assert(Predicate(value, reference), message ?? $"Variable '{value}' is less than or equal to '{reference}'", exception ?? typeof(ValueException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Untraced]
		static internal bool AssertGreaterEqual<T, U>(this T value, in U reference, in string message = null, Type exception = null)
			where T : IComparable, IComparable<U>
			where U : IComparable, IComparable<T>
		{
			bool Predicate(in T value, in U reference)
			{
				return value.CompareTo(reference) >= 0;
			}

			return Assert(Predicate(value, reference), message ?? $"Variable '{value}' is less than to '{reference}'", exception ?? typeof(ValueException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Untraced]
		static internal bool AssertLess<T, U>(this T value, in U reference, in string message = null, Type exception = null)
			where T : IComparable, IComparable<U>
			where U : IComparable, IComparable<T>
		{
			bool Predicate(in T value, in U reference)
			{
				return value.CompareTo(reference) < 0;
			}

			return Assert(Predicate(value, reference), message ?? $"Variable '{value}' is greater than or equal to '{reference}'", exception ?? typeof(ValueException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Untraced]
		static internal bool AssertLessEqual<T, U>(this T value, in U reference, in string message = null, Type exception = null)
			where T : IComparable, IComparable<U>
			where U : IComparable, IComparable<T>
		{
			bool Predicate(in T value, in U reference)
			{
				return value.CompareTo(reference) <= 0;
			}

			return Assert(Predicate(value, reference), message ?? $"Variable '{value}' is greater than '{reference}'", exception ?? typeof(ValueException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Untraced]
		static internal bool AssertZero<T>(this T value, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible
		{
			return AssertEqual(
				value,
				(T)Convert.ChangeType(0, typeof(T)),
				message ?? $"Variable '{value}' is not equal to zero",
				exception ?? typeof(OutOfRangeException)
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Untraced]
		static internal bool AssertOne<T>(this T value, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible
		{
			return AssertEqual(
				value,
				(T)Convert.ChangeType(1, typeof(T)),
				message ?? $"Variable '{value}' is not equal to one",
				exception ?? typeof(OutOfRangeException)
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Untraced]
		static internal bool AssertNotZeroe<T>(this T value, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible
		{
			return AssertNotEqual(
				value,
				(T)Convert.ChangeType(0, typeof(T)),
				message ?? $"Variable '{value}' is equal to zero",
				exception ?? typeof(OutOfRangeException)
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Untraced]
		static internal bool AssertPositive<T>(this T value, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible
		{
			return AssertGreater(
				value,
				(T)Convert.ChangeType(0, typeof(T)),
				message ?? $"Variable '{value}' is not positive",
				exception ?? typeof(OutOfRangeException)
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Untraced]
		static internal bool AssertPositiveOrZero<T>(this T value, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible
		{
			return AssertGreaterEqual(
				value,
				(T)Convert.ChangeType(0, typeof(T)),
				message ?? $"Variable '{value}' is not positive or zero",
				exception ?? typeof(OutOfRangeException)
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Untraced]
		static internal bool AssertNotNegative<T>(this T value, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible
		{
			return AssertPositiveOrZero(value, message, exception);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Untraced]
		static internal bool AssertNegative<T>(this T value, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible
		{
			return AssertLess(
				value,
				(T)Convert.ChangeType(0, typeof(T)),
				message ?? $"Variable '{value}' is not negative",
				exception ?? typeof(OutOfRangeException)
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Untraced]
		static internal bool AssertNegativeOrZero<T>(this T value, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible
		{
			return AssertLessEqual(
				value,
				(T)Convert.ChangeType(0, typeof(T)),
				message ?? $"Variable '{value}' is not negative or zero",
				exception ?? typeof(OutOfRangeException)
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Untraced]
		static internal bool AssertNotPositive<T>(this T value, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible
		{
			return AssertNegativeOrZero(value, message, exception);
		}

		// TODO : Integer overloads for most asserts
		// TODO : Implement a check for ==/!= operators?
	}
}
