using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SpriteMaster {
	public static class Contract {
		private abstract class Exception : System.Exception {
			protected const string DefaultMessage = "Contract Exception";
			protected Exception () : this(DefaultMessage) { }
			protected Exception (in string message) : this(message, null) { }
			protected Exception (in string message, in Exception inner) : base(message, inner) { }
		}

		private class ValueException : Exception {
			protected new const string DefaultMessage = "Value failed assertion";
			internal ValueException () : this(DefaultMessage) { }
			internal ValueException (in string message) : this(message, null) { }
			internal ValueException (in string message, in Exception inner) : base(message, inner) { }
		}

		private class NotNullReferenceException : ValueException {
			protected new const string DefaultMessage = "Value failed assertion, was not null";
			internal NotNullReferenceException () : this(DefaultMessage) { }
			internal NotNullReferenceException (in string message) : this(message, null) { }
			internal NotNullReferenceException (in string message, in Exception inner) : base(message, inner) { }
		}

		private class NullReferenceException : ValueException {
			protected new const string DefaultMessage = "Value failed assertion, was null";
			internal NullReferenceException () : this(DefaultMessage) { }
			internal NullReferenceException (in string message) : this(message, null) { }
			internal NullReferenceException (in string message, in Exception inner) : base(message, inner) { }
		}

		private class BooleanException : ValueException {
			protected new const string DefaultMessage = "Value failed boolean assertion";
			internal BooleanException () : this(DefaultMessage) { }
			internal BooleanException (in string message) : this(message, null) { }
			internal BooleanException (in string message, in Exception inner) : base(message, inner) { }
		}

		private class OutOfRangeException : ValueException {
			protected new const string DefaultMessage = "Value failed assertion, out of range";
			internal OutOfRangeException () : this(DefaultMessage) { }
			internal OutOfRangeException (in string message) : this(message, null) { }
			internal OutOfRangeException (in string message, in Exception inner) : base(message, inner) { }
		}

		[DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static private bool IsExceptionType (this Type type) {
			return type.IsSubclassOf(typeof(Exception));
		}

		public delegate bool ClosedPredicate ();

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void AssertNull<T> (this T value, in string message = "Variable is not null", Type exception = null) {
			Assert(value == null, message, exception ?? typeof(NotNullReferenceException));
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void AssertNotNull<T> (this T value, in string message = "Variable is null", Type exception = null) {
			Assert(value != null, message, exception ?? typeof(NullReferenceException));
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void AssertTrue (this in bool value, in string message = "Variable is not true", Type exception = null) {
			Assert(value == true, message, exception ?? typeof(BooleanException));
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void AssertFalse (this in bool value, in string message = "Variable is not false", Type exception = null) {
			Assert(value == false, message, exception ?? typeof(BooleanException));
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void Assert (bool predicate, in string message = "Variable's value is invalid", Type exception = null) {
			if (!exception.IsExceptionType()) {
				throw new ArgumentOutOfRangeException("Provided assert exception type is not a subclass of Exception");
			}
			if (!predicate) {
				throw (ValueException)Activator.CreateInstance(exception ?? typeof(ValueException), new object[] { message });
			}
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void Assert (in ClosedPredicate predicate, in string message = "Variable failed predicated assertion", in Type exception = null) {
			if (predicate == null) {
				throw new ArgumentNullException($"Argument '{nameof(predicate)}' is null");
			}
			Assert(predicate.Invoke(), message, exception ?? typeof(ValueException));
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void Assert<T> (in T value, in Predicate<T> predicate, in string message = "Variable failed predicated assertion", in Type exception = null) {
			if (predicate == null) {
				throw new ArgumentNullException($"Argument '{nameof(predicate)}' is null");
			}
			Assert(predicate.Invoke(value), message, exception ?? typeof(ValueException));
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void AssertEqual<T, U> (this T value, in U reference, in string message = null, Type exception = null)
			where T : IComparable, IComparable<U>, IEquatable<U>
			where U : IComparable, IComparable<T>, IEquatable<T> {
			bool Predicate (in T value, in U reference) {
				if (typeof(T).IsSubclassOf(typeof(IEquatable<U>))) {
					return value.Equals(reference);
				}
				else {
					return value.CompareTo(reference) == 0;
				}
			}

			Assert(Predicate(value, reference), message ?? $"Variable '{value}' is not equal to '{reference}'", exception ?? typeof(ValueException));
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void AssertNotEqual<T, U> (this T value, in U reference, in string message = null, Type exception = null)
			where T : IComparable, IComparable<U>, IEquatable<U>
			where U : IComparable, IComparable<T>, IEquatable<T> {
			bool Predicate (in T value, in U reference) {
				if (typeof(T).IsSubclassOf(typeof(IEquatable<U>))) {
					return !value.Equals(reference);
				}
				else {
					return value.CompareTo(reference) != 0;
				}
			}

			Assert(Predicate(value, reference), message ?? $"Variable '{value}' is equal to '{reference}'", exception ?? typeof(ValueException));
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void AssertGreater<T, U> (this T value, in U reference, in string message = null, Type exception = null)
			where T : IComparable, IComparable<U>
			where U : IComparable, IComparable<T> {
			bool Predicate (in T value, in U reference) {
				return value.CompareTo(reference) > 0;
			}

			Assert(Predicate(value, reference), message ?? $"Variable '{value}' is less than or equal to '{reference}'", exception ?? typeof(ValueException));
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void AssertGreaterEqual<T, U> (this T value, in U reference, in string message = null, Type exception = null)
			where T : IComparable, IComparable<U>
			where U : IComparable, IComparable<T> {
			bool Predicate (in T value, in U reference) {
				return value.CompareTo(reference) >= 0;
			}

			Assert(Predicate(value, reference), message ?? $"Variable '{value}' is less than to '{reference}'", exception ?? typeof(ValueException));
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void AssertLess<T, U> (this T value, in U reference, in string message = null, Type exception = null)
			where T : IComparable, IComparable<U>
			where U : IComparable, IComparable<T> {
			bool Predicate (in T value, in U reference) {
				return value.CompareTo(reference) < 0;
			}

			Assert(Predicate(value, reference), message ?? $"Variable '{value}' is greater than or equal to '{reference}'", exception ?? typeof(ValueException));
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void AssertLessEqual<T, U> (this T value, in U reference, in string message = null, Type exception = null)
			where T : IComparable, IComparable<U>
			where U : IComparable, IComparable<T> {
			bool Predicate (in T value, in U reference) {
				return value.CompareTo(reference) <= 0;
			}

			Assert(Predicate(value, reference), message ?? $"Variable '{value}' is greater than '{reference}'", exception ?? typeof(ValueException));
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void AssertZero<T> (this T value, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible {
			AssertEqual(
				value,
				(T)Convert.ChangeType(0, typeof(T)),
				message ?? $"Variable '{value}' is not equal to zero",
				exception ?? typeof(OutOfRangeException)
			);
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void AssertOne<T> (this T value, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible {
			AssertEqual(
				value,
				(T)Convert.ChangeType(1, typeof(T)),
				message ?? $"Variable '{value}' is not equal to one",
				exception ?? typeof(OutOfRangeException)
			);
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void AssertNotZeroe<T> (this T value, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible {
			AssertNotEqual(
				value,
				(T)Convert.ChangeType(0, typeof(T)),
				message ?? $"Variable '{value}' is equal to zero",
				exception ?? typeof(OutOfRangeException)
			);
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void AssertPositive<T> (this T value, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible {
			AssertGreater(
				value,
				(T)Convert.ChangeType(0, typeof(T)),
				message ?? $"Variable '{value}' is not positive",
				exception ?? typeof(OutOfRangeException)
			);
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void AssertPositiveOrZero<T> (this T value, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible {
			AssertGreaterEqual(
				value,
				(T)Convert.ChangeType(0, typeof(T)),
				message ?? $"Variable '{value}' is not positive or zero",
				exception ?? typeof(OutOfRangeException)
			);
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void AssertNotNegative<T> (this T value, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible {
			AssertPositiveOrZero(value, message, exception);
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void AssertNegative<T> (this T value, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible {
			AssertLess(
				value,
				(T)Convert.ChangeType(0, typeof(T)),
				message ?? $"Variable '{value}' is not negative",
				exception ?? typeof(OutOfRangeException)
			);
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void AssertNegativeOrZero<T> (this T value, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible {
			AssertLessEqual(
				value,
				(T)Convert.ChangeType(0, typeof(T)),
				message ?? $"Variable '{value}' is not negative or zero",
				exception ?? typeof(OutOfRangeException)
			);
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining), Untraced]
		static internal void AssertNotPositive<T> (this T value, in string message = null, Type exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible {
			AssertNegativeOrZero(value, message, exception);
		}

		// TODO : Integer overloads for most asserts
		// TODO : Implement a check for ==/!= operators?
	}
}
