using SpriteMaster.Types;
using System;
using System.Diagnostics;

namespace SpriteMaster;

internal static class Contracts {
	[DebuggerStepThrough, DebuggerHidden]
	private static bool IsExceptionType(this Type type) => type.IsSubclassOf(typeof(Exception));

	internal delegate bool ClosedPredicate();

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertNull<T>(this T value, string message = "Variable is not null", Type? exception = null) {
		Assert(value is null, message, exception ?? typeof(ArgumentOutOfRangeException));
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertNotNull<T>(this T value, string message = "Variable is null", Type? exception = null) {
		Assert(value is not null, message, exception ?? typeof(NullReferenceException));
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertTrue(this bool value, string message = "Variable is not true", Type? exception = null) {
		Assert(value == true, message, exception ?? typeof(ArgumentOutOfRangeException));
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertFalse(this bool value, string message = "Variable is not false", Type? exception = null) {
		Assert(value == false, message, exception ?? typeof(ArgumentOutOfRangeException));
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void Assert(bool predicate, string message = "Variable's value is invalid", Type? exception = null) {
		if (exception is not null && !exception.IsExceptionType()) {
			throw new ArgumentOutOfRangeException(nameof(exception), "Provided assert exception type is not a subclass of Exception");
		}
		if (!predicate) {
			// ReSharper disable once CoVariantArrayConversion
			throw (Activator.CreateInstance(exception ?? typeof(ArgumentOutOfRangeException), Arrays.Singleton(message)) as ArgumentOutOfRangeException ?? new ArgumentOutOfRangeException());
		}
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void Assert(ClosedPredicate predicate, string message = "Variable failed predicated assertion", Type? exception = null) {
		if (predicate is null) {
			throw new ArgumentNullException(nameof(predicate), $"Argument '{nameof(predicate)}' is null");
		}
		Assert(predicate.Invoke(), message, exception ?? typeof(ArgumentOutOfRangeException));
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void Assert<T>(in T value, Predicate<T> predicate, string message = "Variable failed predicated assertion", Type? exception = null) {
		if (predicate is null) {
			throw new ArgumentNullException(nameof(predicate), $"Argument '{nameof(predicate)}' is null");
		}
		Assert(predicate.Invoke(value), message, exception ?? typeof(ArgumentOutOfRangeException));
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertEqual<T, U>(this T value, U reference, string? message = null, Type? exception = null)
		where T : IComparable, IComparable<U>, IEquatable<U>
		where U : IComparable, IComparable<T>, IEquatable<T> {
		static bool Predicate(T value, U reference) {
			if (typeof(T).IsSubclassOf(typeof(IEquatable<U>))) {
				return value.Equals(reference);
			}
			else {
				return value.CompareTo(reference) == 0;
			}
		}

		Assert(Predicate(value, reference), message ?? $"Variable '{value}' is not equal to '{reference}'", exception ?? typeof(ArgumentOutOfRangeException));
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertNotEqual<T, U>(this T value, U reference, string? message = null, Type? exception = null)
		where T : IComparable, IComparable<U>, IEquatable<U>
		where U : IComparable, IComparable<T>, IEquatable<T> {
		static bool Predicate(T value, U reference) {
			if (typeof(T).IsSubclassOf(typeof(IEquatable<U>))) {
				return !value.Equals(reference);
			}
			else {
				return value.CompareTo(reference) != 0;
			}
		}

		Assert(Predicate(value, reference), message ?? $"Variable '{value}' is equal to '{reference}'", exception ?? typeof(ArgumentOutOfRangeException));
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertGreater<T, U>(this T value, U reference, string? message = null, Type? exception = null)
		where T : IComparable, IComparable<U>
		where U : IComparable, IComparable<T> {
		static bool Predicate(T value, U reference) {
			return value.CompareTo(reference) > 0;
		}

		Assert(Predicate(value, reference), message ?? $"Variable '{value}' is less than or equal to '{reference}'", exception ?? typeof(ArgumentOutOfRangeException));
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertGreaterEqual<T, U>(this T value, U reference, string? message = null, Type? exception = null)
		where T : IComparable, IComparable<U>
		where U : IComparable, IComparable<T> {
		static bool Predicate(T value, U reference) {
			return value.CompareTo(reference) >= 0;
		}

		Assert(Predicate(value, reference), message ?? $"Variable '{value}' is less than to '{reference}'", exception ?? typeof(ArgumentOutOfRangeException));
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertLess<T, U>(this T value, U reference, string? message = null, Type? exception = null)
		where T : IComparable, IComparable<U>
		where U : IComparable, IComparable<T> {
		static bool Predicate(T value, U reference) {
			return value.CompareTo(reference) < 0;
		}

		Assert(Predicate(value, reference), message ?? $"Variable '{value}' is greater than or equal to '{reference}'", exception ?? typeof(ArgumentOutOfRangeException));
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertLessEqual<T, U>(this T value, U reference, string? message = null, Type? exception = null)
		where T : IComparable, IComparable<U>
		where U : IComparable, IComparable<T> {
		static bool Predicate(T value, U reference) {
			return value.CompareTo(reference) <= 0;
		}

		Assert(Predicate(value, reference), message ?? $"Variable '{value}' is greater than '{reference}'", exception ?? typeof(ArgumentOutOfRangeException));
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertAligned(this nuint value, nuint alignment, string? message = null, Type? exception = null) {
		AssertZero(
			value % alignment,
			message ?? $"Variable '{value}' is not aligned to '{alignment}'",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertAligned(this nint value, nint alignment, string? message = null, Type? exception = null) {
		AssertZero(
			value % alignment,
			message ?? $"Variable '{value}' is not aligned to '{alignment}'",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertAligned(this byte value, byte alignment, string? message = null, Type? exception = null) {
		AssertZero(
			value % alignment,
			message ?? $"Variable '{value}' is not aligned to '{alignment}'",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertAligned(this sbyte value, sbyte alignment, string? message = null, Type? exception = null) {
		AssertZero(
			value % alignment,
			message ?? $"Variable '{value}' is not aligned to '{alignment}'",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertAligned(this short value, short alignment, string? message = null, Type? exception = null) {
		AssertZero(
			value % alignment,
			message ?? $"Variable '{value}' is not aligned to '{alignment}'",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertAligned(this ushort value, ushort alignment, string? message = null, Type? exception = null) {
		AssertZero(
			value % alignment,
			message ?? $"Variable '{value}' is not aligned to '{alignment}'",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertAligned(this int value, int alignment, string? message = null, Type? exception = null) {
		AssertZero(
			value % alignment,
			message ?? $"Variable '{value}' is not aligned to '{alignment}'",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertAligned(this uint value, uint alignment, string? message = null, Type? exception = null) {
		AssertZero(
			value % alignment,
			message ?? $"Variable '{value}' is not aligned to '{alignment}'",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertAligned(this long value, long alignment, string? message = null, Type? exception = null) {
		AssertZero(
			value % alignment,
			message ?? $"Variable '{value}' is not aligned to '{alignment}'",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertAligned(this ulong value, ulong alignment, string? message = null, Type? exception = null) {
		AssertZero(
			value % alignment,
			message ?? $"Variable '{value}' is not aligned to '{alignment}'",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertZero(this nuint value, string? message = null, Type? exception = null) {
		AssertEqual(
			value,
			(nuint)0,
			message ?? $"Variable '{value}' is not equal to zero",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertZero(this nint value, string? message = null, Type? exception = null) {
		AssertEqual(
			value,
			(nint)0,
			message ?? $"Variable '{value}' is not equal to zero",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertZero<T>(this T value, string? message = null, Type? exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible {
		AssertEqual(
			value,
			(T)Convert.ChangeType(0, typeof(T)),
			message ?? $"Variable '{value}' is not equal to zero",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertNotZero(this nuint value, string? message = null, Type? exception = null) {
		AssertNotEqual(
			value,
			(nuint)0,
			message ?? $"Variable '{value}' is equal to zero",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertNotZero(this nint value, string? message = null, Type? exception = null) {
		AssertNotEqual(
			value,
			(nint)0,
			message ?? $"Variable '{value}' is equal to zero",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertNotZero<T>(this T value, string? message = null, Type? exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible {
		AssertNotEqual(
			value,
			(T)Convert.ChangeType(0, typeof(T)),
			message ?? $"Variable '{value}' is equal to zero",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertPositive(this nint value, string? message = null, Type? exception = null) {
		AssertGreater(
			value,
			(nint)0,
			message ?? $"Variable '{value}' is not positive",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertPositive<T>(this T value, string? message = null, Type? exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible {
		AssertGreater(
			value,
			(T)Convert.ChangeType(0, typeof(T)),
			message ?? $"Variable '{value}' is not positive",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertPositiveOrZero(this nuint value, string? message = null, Type? exception = null) {
		AssertGreaterEqual(
			value,
			(nuint)0,
			message ?? $"Variable '{value}' is not positive or zero",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertPositiveOrZero(this nint value, string? message = null, Type? exception = null) {
		AssertGreaterEqual(
			value,
			(nint)0,
			message ?? $"Variable '{value}' is not positive or zero",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertPositiveOrZero<T>(this T value, string? message = null, Type? exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible {
		AssertGreaterEqual(
			value,
			(T)Convert.ChangeType(0, typeof(T)),
			message ?? $"Variable '{value}' is not positive or zero",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertNotNegative(this nuint value, string? message = null, Type? exception = null) {
		AssertPositiveOrZero(value, message, exception);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertNotNegative(this nint value, string? message = null, Type? exception = null) {
		AssertPositiveOrZero(value, message, exception);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertNotNegative<T>(this T value, string? message = null, Type? exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible {
		AssertPositiveOrZero(value, message, exception);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertNegative(this nint value, string? message = null, Type? exception = null) {
		AssertLess(
			value,
			(nint)0,
			message ?? $"Variable '{value}' is not negative",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertNegative<T>(this T value, string? message = null, Type? exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible {
		AssertLess(
			value,
			(T)Convert.ChangeType(0, typeof(T)),
			message ?? $"Variable '{value}' is not negative",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertNegativeOrZero(this nuint value, string? message = null, Type? exception = null) {
		AssertLessEqual(
			value,
			(nuint)0,
			message ?? $"Variable '{value}' is not negative or zero",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertNegativeOrZero(this nint value, string? message = null, Type? exception = null) {
		AssertLessEqual(
			value,
			(nint)0,
			message ?? $"Variable '{value}' is not negative or zero",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertNegativeOrZero<T>(this T value, string? message = null, Type? exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible {
		AssertLessEqual(
			value,
			(T)Convert.ChangeType(0, typeof(T)),
			message ?? $"Variable '{value}' is not negative or zero",
			exception ?? typeof(ArgumentOutOfRangeException)
		);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertNotPositive(this nuint value, string? message = null, Type? exception = null) {
		AssertNegativeOrZero(value, message, exception);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertNotPositive(this nint value, string? message = null, Type? exception = null) {
		AssertNegativeOrZero(value, message, exception);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden]
	internal static void AssertNotPositive<T>(this T value, string? message = null, Type? exception = null) where T : IComparable, IComparable<T>, IEquatable<T>, IConvertible {
		AssertNegativeOrZero(value, message, exception);
	}

	// TODO : Integer overloads for most asserts
	// TODO : Implement a check for ==/!= operators?
}
